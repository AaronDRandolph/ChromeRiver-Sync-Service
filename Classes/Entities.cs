using System.Text.Json;
using AutoMapper;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.DTOs.Responses;
using ChromeRiverService.Classes.Helpers;
using ChromeRiverService.Db.NciCommon;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Interfaces;

namespace ChromeRiverService.Classes
{
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
                IEnumerable<IEnumerable<VwChromeRiverGetAllEntity>> entityBatches = entities.Chunk<VwChromeRiverGetAllEntity>(batchSize.Equals(0) ? throw new Exception("Entities batch size cannot be 0") : batchSize);

                foreach (IEnumerable<VwChromeRiverGetAllEntity> entityBatch in entityBatches)
                {
                    batchNum++;

                    try
                    {
                        ICollection<EntityDto> entityDtos = [];

                        foreach (VwChromeRiverGetAllEntity entity in entityBatch)
                        {
                            try
                            {
                                EntityDto entityDto = _mapper.Map<VwChromeRiverGetAllEntity, EntityDto>(entity);
                                entityDtos.Add(entityDto);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex,"Expection thrown while mapping entity '{entityName}'",entity.EntityName);
                                NumNotUpserted++;
                            }
                        }

                        if (entityDtos.Count == 0) 
                        {
                            throw new Exception($"Entity batch #{batchNum} mapping completely failed");
                        }

                        HttpResponseMessage? response = await _httpHelper.ExecutePostOrPatch<IEnumerable<EntityDto>>(upsertEntitiesEndpoint, entityDtos, isPatch: false);

                        if (response is not null)
                        {
                            if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.AllUpsertedSuccessfully))
                            {
                                NumUpserted += entityDtos.Count;
                            }
                            else if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.SomeUpsertedSuccessfully))
                            {
                                JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
                                string responseContent = await response.Content.ReadAsStringAsync();
                                IEnumerable<EntityResponse> entitiesResponses = JsonSerializer.Deserialize<IEnumerable<EntityResponse>>(responseContent, options) ?? throw new Exception("EntityResponse Json deserialize error");

                                foreach (EntityResponse entityResponse in entitiesResponses)
                                {
                                    try
                                    {
                                        if (entityResponse.Result.Equals("success", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            NumUpserted++;
                                        }
                                        else
                                        {
                                            EntityDto currentEntity = entityDtos.FirstOrDefault(dtos => dtos.EntityCode == entityResponse.EntityCode) ?? throw new Exception($"Entity with EntityCode {entityResponse.EntityCode} could not be mapped to a dto for error messaging");
                                            _logger.LogError("Error attempting to upsert an entity | Error: {errorMessage} | EntityDto: {dto}", entityResponse.ErrorMessage, JsonSerializer.Serialize(currentEntity));
                                            NumNotUpserted++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Expection processing entity upsert responses");
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("Entity success message type not handled");
                            }
                        }
                        else
                        {
                            _logger.LogError("The response for entity batch #{batchNum} returned a null", batchNum);
                            NumNotUpserted += entityDtos.Count;
                        }
                    }
                    catch (Exception ex) 
                    {
                        _logger.LogError(ex, "Exception thrown while processing entities batch #{batchNum}",batchNum);
                    }
                }

                _logger.LogInformation("Entities Upsert Complete | Total Entities Upserted: {NumUpserted} | Total Entities Not Upserted: {NumNotUpserted}", NumUpserted, NumNotUpserted);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,"Entity exception thrown after {NumUpserted} were upserted and {NumNotUpserted} were not sent or returned unsuccessful",NumUpserted,NumNotUpserted);
            }
        }
    }
}