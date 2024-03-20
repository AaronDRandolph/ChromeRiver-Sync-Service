using System.Runtime.InteropServices;
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

        private const int BatchSize = 25; // entity limit
        private static int NumUpserted = 0; 
        private static int NumNotUpserted = 0; 

        public async Task Upsert()
        {
            try
            {
                string upsertAllocationsEndpoint = new(_config.GetValue<string>("UPSERT_ALLOCATIONS_ENDPOINT") ?? throw new Exception("Upsert entities endpoint not found"));
                IEnumerable<VwChromeRiverGetAllAllocation> allocations = await _nciCommonUnitOfWork.Allocations.GetAll();
                IEnumerable<VwChromeRiverGetAllAllocation[]> allocationBatches = allocations.Chunk<VwChromeRiverGetAllAllocation>(BatchSize);

                foreach (VwChromeRiverGetAllAllocation[] allocationBatch in allocationBatches)
                {
                    List<AllocationDto> allocationDtos = [];

                    foreach (VwChromeRiverGetAllAllocation allocation in allocationBatch)
                    {
                        AllocationDto allocationDto = new(allocation);
                        
                        string nullPropertiesLog = NullChecker.GetNullPropertiesLog(allocationDto, $"AllocationNumber:{allocationDto.AllocationNumber}");
                        if (nullPropertiesLog.Equals(string.Empty))
                        {
                            _logger.LogError(nullPropertiesLog);
                            NumNotUpserted++;           
                        }
                        else
                        {
                            allocationDtos.Add(allocationDto);
                        }
                    }

                    HttpResponseMessage responseMessage = await _httpHelper.ExecutePost(upsertAllocationsEndpoint, allocationDtos);

                    if (responseMessage is not null)
                    {
                        IEnumerable<AllocationResponse>? allocationResponses = JsonSerializer.Deserialize<IEnumerable<AllocationResponse>>(responseMessage.Content.ReadAsStringAsync().Result) ?? throw new Exception("No response recieved for allocation upsert");

                        foreach (AllocationResponse allocationResponse in allocationResponses)
                        {
                            if (allocationResponse.Result.ToLower().Equals("success"))
                            {
                                NumUpserted++;
                            }
                            else
                            {
                                _logger.LogError(GetLog(Codes.ResultType.InvalidAllocation.ToString(), allocationResponse));
                                NumNotUpserted++;
                            }
                        }
                    }
                    else
                    {
                        NumNotUpserted += allocationDtos.Count;
                    }

                }

                _logger.LogInformation(Codes.ResultType.AllUpsertsComplete.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error after {NumUpserted} allocations upserted: {ex.Message}");
            }
        }
        
        
        private class AllocationResponse : Response
        {
            public string? AllocationId { get; set; }
        }



        private static string GetLog(string resultType, AllocationResponse? allocationResponse = null)
        {
            if (resultType.Equals(Codes.ResultType.AllUpsertsComplete.ToString()))
            {
                return $"Total Allocations Upserted: {NumUpserted} \n Total Allocations Not Upserted: {NumNotUpserted}";
            }
            else
            {
                StringBuilder log = new($"Upsert Type: Allocations | Result Type: {RegexHelper.PlaceSpacesBeforeUppercase(resultType)}");
                string pipe = " | ";

                if (allocationResponse is not null) 
                {
                    log.Append(pipe).Append($"Allocation ID: ${allocationResponse.AllocationId}");
                    log.Append(pipe).Append($"Error: ${allocationResponse.ErrorMessage}");
                    return log.ToString();
                } 
                else
                {
                    throw new Exception("Result Type required to create allocation error log");
                };
            }
        }
    }
}