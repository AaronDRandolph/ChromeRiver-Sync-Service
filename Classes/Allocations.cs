using System.Text;
using System.Text.Json;
using AutoMapper;
using Azure;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.HelperClasses;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Interfaces;
using IamSyncService.Db.NciCommon;
using Task = System.Threading.Tasks.Task;


namespace ChromeRiverService.Classes {
    public class Allocations (INciCommonUnitOfWork nciCommonUnitOfWork, IConfiguration configuration, ILogger<Worker> logger, IHttpHelper httpHelper, IMapper mapper) : IAllocations
    {
        private readonly IConfiguration _config = configuration;
        private readonly ILogger<Worker> _logger = logger;
        private readonly INciCommonUnitOfWork _nciCommonUnitOfWork = nciCommonUnitOfWork; 
        private readonly IHttpHelper _httpHelper= httpHelper;
        private readonly IMapper _mapper= mapper;

        private static int NumUpserted = 0; 
        private static int NumNotUpserted = 0; 


        public async Task Upsert()
        {
            try
            {
                string upsertAllocationsEndpoint = _config.GetValue<string>("UPSERT_ALLOCATIONS_ENDPOINT") ?? throw new Exception("Upsert allocations endpoint not found");
                int batchSize = _config.GetValue<int>("UPSERT_ALLOCATIONS_ENDPOINT_BATCH_LIMIT");
                int batchNum = 0;

                IEnumerable<VwChromeRiverGetAllAllocation> allocations = await _nciCommonUnitOfWork.Allocations.GetAll();
                IEnumerable<IEnumerable<VwChromeRiverGetAllAllocation>> allocationBatches = allocations.Chunk<VwChromeRiverGetAllAllocation>(batchSize);

                foreach (IEnumerable<VwChromeRiverGetAllAllocation> allocationBatch in allocationBatches)
                {
                    batchNum++;

                    try
                    {
                        IList<AllocationDto> allocationDtos = [];

                        foreach (VwChromeRiverGetAllAllocation allocation in allocationBatch)
                        {
                            try
                            {
                                AllocationDto allocationDto = _mapper.Map<VwChromeRiverGetAllAllocation, AllocationDto>(allocation);
                                allocationDtos.Add(allocationDto);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Exception thrown while mapping Allocation Number '{allocationNumber}': {ex}", allocation.AllocationNumber, ex);
                                NumNotUpserted++;
                            }
                        }
                        HttpResponseMessage? response = await _httpHelper.ExecutePost<IEnumerable<AllocationDto>>(upsertAllocationsEndpoint, allocationDtos);

                        if (response is not null)
                        {
                            if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.AllUpsertedSuccessfully))
                            {
                                NumUpserted += allocationDtos.Count;
                            }
                            else if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.SomeUpsertedSuccessfully))
                            {
                                JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
                                IEnumerable<AllocationResponse>? allocationResponses = JsonSerializer.Deserialize<IEnumerable<AllocationResponse>>(response.Content.ReadAsStringAsync().Result, options) ?? throw new Exception("AllocationResponse Json deserialize error");

                                foreach (AllocationResponse allocationResponse in allocationResponses)
                                {
                                    if (allocationResponse.Result.Equals("success", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        NumUpserted++;
                                    }
                                    else
                                    {
                                        _logger.LogError("{log}", GetLog(Codes.ResultType.InvalidAllocation, allocationResponse));
                                        NumNotUpserted++;
                                    }
                                }                                
                            }
                            else
                            {
                                throw new Exception("Success message type not handled");
                            }
                        }
                        else
                        {
                            _logger.LogError("The response for allocation batch #{batchNum} returned a null *************************************************************************************************", batchNum);
                            NumNotUpserted += allocationDtos.Count;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Exception thrown while processing allocation batch #{batchNum}: {ex}", batchNum, ex);
                    }
                    break;
                }

                _logger.LogInformation("{log}", GetLog(Codes.ResultType.AllUpsertsComplete));
            }
            catch (Exception ex)
            {
                _logger.LogError("Allocations exception thrown after {NumUpserted} were upserted and {NumNotUpserted} were not sent or returned unsuccessful | Message : {messsage}", NumUpserted, NumNotUpserted, ex.Message);
            }
        }
        

        private static string GetLog(Codes.ResultType resultType, AllocationResponse? allocationResponse = null, AllocationDto? mappedDto = null) 
        {
            string pipe = " | ";

            StringBuilder log = new StringBuilder("Upsert Type: Allocations")
                             .Append(pipe).Append($"Result Type: {RegexHelper.PlaceSpacesBeforeUppercase(resultType.ToString())}");

            if (resultType.Equals(Codes.ResultType.AllUpsertsComplete))
            {
                return   log.Append(pipe).Append($"Total Allocations Upserted: {NumUpserted}")
                            .Append(pipe).Append($"Total Allocations Not Upserted: {NumNotUpserted}")
                            .ToString();
            }
            else if (allocationResponse is not null) 
            {
                return   log.Append(pipe).Append($"Error: ${allocationResponse.ErrorMessage}")
                            .Append(pipe).Append($"AllocationID (AllocationNumber_ClientNumber): {allocationResponse.AllocationId}")
                            .ToString();                
            } 
            else
            {
                throw new Exception("allocationResponse and mapped dto objects required to create allocation error log");
            }
        }
    }
}