using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.HelperClasses;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Interfaces;
using IamSyncService.Db.NciCommon;

namespace ChromeRiverService.Classes {
    public class Entities (INciCommonUnitOfWork nciCommonUnitOfWork, IConfiguration configuration, ILogger<Worker> logger, IHttpHelper httpHelper) : IEntities
    {
        private readonly IConfiguration _config = configuration;
        private readonly ILogger<Worker> _logger = logger;
        private readonly IHttpHelper _httpHelper = httpHelper;
        private readonly INciCommonUnitOfWork _nciCommonUnitOfWork = nciCommonUnitOfWork;

        private static int NumUpserted = 0;
        private static int NumNotUpserted = 0;

        public async Task Upsert() 
        {
            try
            {
                string upsertEntitiesEndpoint = _config.GetValue<string>("UPSERT_ENTITIES_ENDPOINT") ?? throw new Exception("Upsert entities endpoint not found");
                int batchSize = _config.GetValue<int>("UPSERT_ENTITIES_ENDPOINT_BATCH_LIMIT");

                IEnumerable<VwChromeRiverGetAllEntity> entities = await _nciCommonUnitOfWork.Entities.GetAll();
                IEnumerable<IEnumerable<VwChromeRiverGetAllEntity>> entityBatches = entities.Chunk<VwChromeRiverGetAllEntity>(batchSize.Equals(0) ? throw new Exception("Allocation batch size cannot be 0") : batchSize);

                foreach (IEnumerable<VwChromeRiverGetAllEntity> entityBatch in entityBatches)
                {
                    List<EntityDto> entityDtos = [];

                    foreach (VwChromeRiverGetAllEntity entity in entityBatch)
                    {
                        EntityDto entityDto = new(entity);

                        string nullPropertiesLog = NullChecker.GetNullPropertiesLog(entityDto,$"[ Type: '{entityDto.EntityTypeCode}', Code:'{entityDto.EntityCode}' ]");
                        if (nullPropertiesLog.Equals(string.Empty))
                        {
                            entityDtos.Add(entityDto);
                        }
                        else
                        {
                            _logger.LogError("{log}", nullPropertiesLog);
                            NumNotUpserted++;
                        }
                    }

                    HttpResponseMessage? response = await _httpHelper.ExecutePost<IEnumerable<EntityDto>>(upsertEntitiesEndpoint, entityDtos);

                    if (response is not null)
                    {
                        IEnumerable<EntityResponse>? entitiesResponses = JsonSerializer.Deserialize<IEnumerable<EntityResponse>>(response.Content.ReadAsStringAsync().Result) ?? throw new Exception("EntityResponse Json deserialize error");
                        int index = 0;

                        foreach (EntityResponse entityResponse in entitiesResponses)
                        {
                            if (entityResponse.Result.Equals("success",StringComparison.InvariantCultureIgnoreCase))
                            {
                                NumUpserted++;
                            }
                            else
                            {
                                _logger.LogError("{log}", GetLog(Codes.ResultType.InvalidEntity.ToString(), entityResponse, entityDtos[index]));
                                NumNotUpserted++;
                            }
                            index++;
                        }
                    }
                    else
                    {
                        NumNotUpserted += entityDtos.Count;
                    }
                }

                _logger.LogInformation("{log}", GetLog(Codes.ResultType.AllUpsertsComplete.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError("Entity exception thrown after {NumUpserted} were upserted and {NumNotUpserted} were not sent or returned unsuccessful | Message: {messsage}", NumUpserted, NumNotUpserted, ex.Message);
            }
        }


        private class EntityResponse : Response   // These return null on success
        {
            public string? EntityCode { get; set; }
            public string? EntityTypeCode { get; set; }
        }



        private static string GetLog(string resultType, EntityResponse? entityResponse = null, EntityDto? mappedDto = null)
        {
            string pipe = " | ";

            StringBuilder log = new StringBuilder("Upsert Type: Allocations")
                             .Append(pipe).Append($"Result Type: {RegexHelper.PlaceSpacesBeforeUppercase(resultType)}");

            if (resultType.Equals(Codes.ResultType.AllUpsertsComplete.ToString()))
            {
                return log
                    .Append(pipe).Append($"Total Entities Upserted: {NumUpserted}")
                    .Append(pipe).Append($"Total Entities Not Upserted: {NumNotUpserted}")
                    .ToString();
            }
            else if (entityResponse is not null && mappedDto is not null)
            {
                return log
                    .Append(pipe).Append($"Error: {entityResponse.ErrorMessage}")
                    .Append(pipe).Append($"AllocationDto: ${JsonSerializer.Serialize(mappedDto)}")
                    .ToString();
            } 
            else
            {
                throw new Exception("entityResponse and mappedDto required to create entity error log");
            }
        }
    }
}