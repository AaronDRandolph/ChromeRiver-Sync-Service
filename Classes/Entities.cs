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

        private const int BatchSize = 50;
        private static int NumUpserted = 0;
        private static int NumNotUpserted = 0;

        public async Task Upsert() 
        {
            try
            {
                string upsertEntitiesEndpoint = new(_config.GetValue<string>("UPSERT_ENTITIES_ENDPOINT") ?? throw new Exception("Upsert entities endpoint not found"));
                IEnumerable<VwChromeRiverGetAllEntity> entities = await _nciCommonUnitOfWork.Entities.GetAll();
                IEnumerable<VwChromeRiverGetAllEntity[]> entityBatches = entities.Chunk<VwChromeRiverGetAllEntity>(BatchSize);

                foreach (VwChromeRiverGetAllEntity[] entityBatch in entityBatches)
                {
                    List<EntityDto> entityDtos = [];

                    foreach (VwChromeRiverGetAllEntity entity in entityBatch)
                    {
                        EntityDto entityDto = new EntityDto(entity);

                        string nullPropertiesLog = NullChecker.GetNullPropertiesLog(entityDto,$"[ type '{entityDto.EntityTypeCode}' and code'{entityDto.EntityCode}' ]");
                        if (nullPropertiesLog.Equals(string.Empty))
                        {
                            entityDtos.Add(entityDto);
                        }
                        else
                        {
                            _logger.LogError(nullPropertiesLog);
                            NumNotUpserted++;
                        }
                    }

                    HttpResponseMessage? response = await _httpHelper.ExecutePost(upsertEntitiesEndpoint, entityDtos);

                    if (response is not null)
                    {

                        IEnumerable<EntityResponse>? entitiesResponses = JsonSerializer.Deserialize<IEnumerable<EntityResponse>>(response.Content.ReadAsStringAsync().Result) ?? throw new Exception("EntityResponse Json Deserialize error");

                        foreach (EntityResponse entityResponse in entitiesResponses)
                        {
                            if (entityResponse.Result.ToLower().Equals("success"))
                            {
                                NumUpserted++;
                            }
                            else
                            {
                                _logger.LogError(GetLog(Codes.ResultType.InvalidEntity.ToString(), entityResponse));
                                NumNotUpserted++;
                            }
                        }
                    }
                    else
                    {
                        NumNotUpserted += entityDtos.Count;
                    }

                }

                _logger.LogInformation(GetLog(Codes.ResultType.AllUpsertsComplete.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error after {NumUpserted} entitied upserted: {ex.Message}");
            }

        }
        private class EntityResponse : Response
        {
            public string? EntityCode { get; set; }
            public string? EntityTypeCode { get; set; }
        }

         private static string GetLog(string resultType, EntityResponse? entityResponse = null)
        {
            if (resultType.Equals(Codes.ResultType.AllUpsertsComplete.ToString()))
            {
                return $"Total Entites Upserted: {NumUpserted} \n Total Entites Not Upserted: {NumNotUpserted}";
            }
            else
            {
                StringBuilder log = new($"Upsert Type: Allocations | Result Type: {RegexHelper.PlaceSpacesBeforeUppercase(resultType)}");
                string pipe = " | ";

                if (entityResponse is not null) 
                {
                    log.Append(pipe).Append($"Entity Type Code: ${entityResponse.EntityTypeCode}");
                    log.Append(pipe).Append($"Entity Code: ${entityResponse.EntityCode}");
                    log.Append(pipe).Append($"Error: ${entityResponse.ErrorMessage}");
                    return log.ToString();
                } 
                else
                {
                    throw new Exception("Result Type required to create entity log");
                };
            }
        }
    }
}