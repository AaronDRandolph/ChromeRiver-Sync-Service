using System.Text;
using System.Text.Json;
using AutoMapper;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.HelperClasses;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Interfaces;
using IamSyncService.Db.NciCommon;

namespace ChromeRiverService.Classes {
    public class Entities (INciCommonUnitOfWork nciCommonUnitOfWork, IConfiguration configuration, ILogger<Worker> logger, IHttpHelper httpHelper, IMapper mapper) : IEntities
    {
        private readonly IConfiguration _config = configuration;
        private readonly ILogger<Worker> _logger = logger;
        private readonly IHttpHelper _httpHelper = httpHelper;
        private readonly INciCommonUnitOfWork _nciCommonUnitOfWork = nciCommonUnitOfWork;
        private readonly IMapper _mapper = mapper;

        private static int NumUpserted = 0;
        private static int NumNotUpserted = 0;

        public async Task Upsert() 
        {
            try
            {
                string upsertEntitiesEndpoint = _config.GetValue<string>("UPSERT_ENTITIES_ENDPOINT") ?? throw new Exception("Upsert entities endpoint not found");
                int batchSize = _config.GetValue<int>("UPSERT_ENTITIES_ENDPOINT_BATCH_LIMIT");
                int batchNum = 0;

                IEnumerable<VwChromeRiverGetAllEntity> entities = await _nciCommonUnitOfWork.Entities.GetAll();
                IEnumerable<IEnumerable<VwChromeRiverGetAllEntity>> entityBatches = entities.Chunk<VwChromeRiverGetAllEntity>(batchSize.Equals(0) ? throw new Exception("Allocation batch size cannot be 0") : batchSize);

                foreach (IEnumerable<VwChromeRiverGetAllEntity> entityBatch in entityBatches)
                {
                    batchNum++;

                    try
                    {
                        IList<EntityDto> entityDtos = [];

                        foreach (VwChromeRiverGetAllEntity entity in entityBatch)
                        {
                            try
                            {
                                EntityDto entityDto = _mapper.Map<VwChromeRiverGetAllEntity, EntityDto>(entity);
                                entityDtos.Add(entityDto);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Error thrown while mapping entity '{entitiyName}': {log}", entity.EntityName, ex);
                                NumNotUpserted++;
                            }
                        }

                        HttpResponseMessage? response = await _httpHelper.ExecutePost<IEnumerable<EntityDto>>(upsertEntitiesEndpoint, entityDtos);

                        if (response is not null)
                        {
                            if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.AllUpsertedSuccessfully))
                            {
                                NumUpserted += entityDtos.Count;
                            }
                            else if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.SomeUpsertedSuccessfully))
                            {
                                JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
                                IEnumerable<EntityResponse>? entitiesResponses = JsonSerializer.Deserialize<IEnumerable<EntityResponse>>(response.Content.ReadAsStringAsync().Result, options) ?? throw new Exception("EntityResponse Json deserialize error");
                                int index = 0;

                                foreach (EntityResponse entityResponse in entitiesResponses)
                                {
                                    if (entityResponse.Result.Equals("success", StringComparison.InvariantCultureIgnoreCase))
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
                                throw new Exception("Success message type not handled");
                            }
                        }
                        else
                        {
                            _logger.LogError("The response for entity batch #{batchNum} returned a null", batchNum);
                            NumNotUpserted += entityDtos.Count;
                        }
                        break;

                    }
                    catch (Exception ex) 
                    {
                        _logger.LogError("Exception thrown while processing entity batch #{batchNum}: {ex}", batchNum, ex);
                    }
                    break;

                }

                _logger.LogInformation("{log}", GetLog(Codes.ResultType.AllUpsertsComplete.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError("Entity exception thrown after {NumUpserted} were upserted and {NumNotUpserted} were not sent or returned unsuccessful | Message: {messsage}", NumUpserted, NumNotUpserted, ex.Message);
            }
        }



        private static string GetLog(string resultType, EntityResponse? entityResponse = null, EntityDto? mappedDto = null)
        {
            string pipe = " | ";

            StringBuilder log = new StringBuilder("Upsert Type: Entities")
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