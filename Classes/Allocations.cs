using System.Text;
using System.Text.Json;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.HelperClasses;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Interfaces;
using IamSyncService.Db.NciCommon;

namespace ChromeRiverService.Classes {
    public class Allocations (INciCommonUnitOfWork nciCommonUnitOfWork, IConfiguration configuration, ILogger<Worker> logger, IHttpHelper httpHelper) : IAllocations
    {
        private readonly IConfiguration _config = configuration;
        private readonly ILogger<Worker> _logger = logger;
        private readonly INciCommonUnitOfWork _nciCommonUnitOfWork = nciCommonUnitOfWork; 
        private readonly IHttpHelper _httpHelper= httpHelper;

        private static int NumUpserted = 0; 
        private static int NumNotUpserted = 0; 

        public async Task Upsert()
        {
            try
            {
                string upsertAllocationsEndpoint = _config.GetValue<string>("UPSERT_ALLOCATIONS_ENDPOINT") ?? throw new Exception("Upsert allocations endpoint not found");
                int batchSize = _config.GetValue<int>("UPSERT_ALLOCATIONS_ENDPOINT_BATCH_LIMIT");

                IEnumerable<VwChromeRiverGetAllAllocation> allocations = await _nciCommonUnitOfWork.Allocations.GetAll();
                IEnumerable<IEnumerable<VwChromeRiverGetAllAllocation>> allocationBatches = allocations.Chunk<VwChromeRiverGetAllAllocation>(batchSize);

                foreach (IEnumerable<VwChromeRiverGetAllAllocation> allocationBatch in allocationBatches)
                {
                    List<AllocationDto> allocationDtos = [];

                    foreach (VwChromeRiverGetAllAllocation allocation in allocationBatch)
                    {
                        AllocationDto allocationDto = new(allocation);
                        
                        string nullPropertiesLog = NullChecker.GetNullPropertiesLog(allocationDto, $"AllocationNumber:{allocationDto.AllocationNumber}");
                        if (nullPropertiesLog.Equals(string.Empty))
                        {
                            allocationDtos.Add(allocationDto);          
                        }
                        else
                        {
                            _logger.LogError("{log}", nullPropertiesLog);
                            NumNotUpserted++;
                        }
                    }

                    HttpResponseMessage? responseMessage = await _httpHelper.ExecutePost<IEnumerable<AllocationDto>>(upsertAllocationsEndpoint, allocationDtos);

                    if (responseMessage is not null)
                    {
                        IEnumerable<AllocationResponse>? allocationResponses = JsonSerializer.Deserialize<IEnumerable<AllocationResponse>>(responseMessage.Content.ReadAsStringAsync().Result) ?? throw new Exception("AllocationResponse Json deserialize error");
                        int index = 0;

                        foreach (AllocationResponse allocationResponse in allocationResponses)
                        {
                            if (allocationResponse.Result.Equals("success", StringComparison.InvariantCultureIgnoreCase))
                            {
                                NumUpserted++;
                            }
                            else
                            {
                                _logger.LogError("{log}", GetLog(Codes.ResultType.InvalidAllocation, allocationResponse, allocationDtos[index]));
                                NumNotUpserted++;
                            }

                            index++;
                        }
                    }
                    else
                    {
                        NumNotUpserted += allocationDtos.Count;
                    }
                }

                _logger.LogInformation("{log}", GetLog(Codes.ResultType.AllUpsertsComplete));
            }
            catch (Exception ex)
            {
                _logger.LogError("Allocations exception thrown after {NumUpserted} were upserted and {NumNotUpserted} were not sent or returned unsuccessful | Message : {messsage}", NumUpserted, NumNotUpserted, ex.Message);
            }
        }
        

        private class AllocationResponse : Response
        {
            public string? AllocationId { get; set; } // This returns null on success
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
            else if (allocationResponse is not null && mappedDto is not null) 
            {
                return   log.Append(pipe).Append($"Error: ${allocationResponse.ErrorMessage}")
                            .Append(pipe).Append($"AllocationDto: ${JsonSerializer.Serialize(mappedDto)}")
                            .ToString();                
            } 
            else
            {
                throw new Exception("allocationResponse and mapped dto objects required to create allocation error log");
            }
        }
    }
}