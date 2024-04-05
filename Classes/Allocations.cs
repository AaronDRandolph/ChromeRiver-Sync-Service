using System.Text.Json;
using AutoMapper;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.DTOs.Responses;
using ChromeRiverService.Classes.Helpers;
using ChromeRiverService.Db.NciCommon;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Interfaces;
using Task = System.Threading.Tasks.Task;


namespace ChromeRiverService.Classes
{
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
                        ICollection<AllocationDto> allocationDtos = [];

                        foreach (VwChromeRiverGetAllAllocation allocation in allocationBatch)
                        {
                            try
                            {
                                AllocationDto allocationDto = _mapper.Map<VwChromeRiverGetAllAllocation, AllocationDto>(allocation);
                                allocationDtos.Add(allocationDto);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex,"Exception thrown while mapping Allocation Number '{allocationNumber}'",allocation.AllocationNumber);
                                NumNotUpserted++;
                            }
                        }

                        if (allocationDtos.Count == 0) throw new Exception($"Allocation batch #{batchNum} mapping completely failed");

                        HttpResponseMessage? response = await _httpHelper.ExecutePostOrPatch<IEnumerable<AllocationDto>>(upsertAllocationsEndpoint, allocationDtos, isPatch: false);

                        if (response is not null)
                        {
                            if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.AllUpsertedSuccessfully))
                            {
                                NumUpserted += allocationDtos.Count;
                            }
                            else if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.SomeUpsertedSuccessfully))
                            {
                                JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
                                string responseContent = await response.Content.ReadAsStringAsync();
                                IEnumerable<AllocationResponse> allocationResponses = JsonSerializer.Deserialize<IEnumerable<AllocationResponse>>(responseContent, options) ?? throw new Exception("AllocationResponse Json deserialize error");

                                foreach (AllocationResponse allocationResponse in allocationResponses)
                                {
                                    if (allocationResponse.Result.Equals("success", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        NumUpserted++;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            AllocationDto currentAllocation = allocationDtos.FirstOrDefault(dto => allocationResponse.AllocationId.Equals($"{dto.AllocationNumber}_{dto.Type}", StringComparison.InvariantCultureIgnoreCase)) ?? throw new Exception($"Allocation response with ID (Allocation.AllocationNumber_Allocation.Type) {allocationResponse.AllocationId} could not be mapped to a dto for error messaging");
                                            _logger.LogError("Upsert Type: Allocations | Result Type: All Allocations Upserted | Error: {ErrorMessage} | AllocationDto: {dto}", allocationResponse.ErrorMessage, JsonSerializer.Serialize(currentAllocation));
                                            NumNotUpserted++;
                                        }
                                        catch (Exception ex) 
                                        {
                                            _logger.LogError(ex, "Expection processing allocation upsert responses");
                                        }
                                    }
                                }                                
                            }
                            else
                            {
                                throw new Exception("Allocations success message type not handled");
                            }
                        }
                        else
                        {
                            _logger.LogError("The response for allocation batch #{batchNum} returned a null", batchNum);
                            NumNotUpserted += allocationDtos.Count;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception thrown while processing allocation batch #{batchNum}",batchNum);
                    }
                }

                _logger.LogInformation("Allocations Upsert Complete | Total Allocations Upserted: {NumUpserted} | Total Allocations Not Upserted: {NumNotUpserted}", NumUpserted, NumNotUpserted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Allocations exception thrown after {NumUpserted} were upserted and {NumNotUpserted} were not sent or returned unsuccessful",NumUpserted,NumNotUpserted);
            }
        }
    }
}