using AutoMapper;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.DTOs.Responses;
using ChromeRiverService.Classes.Helpers;
using ChromeRiverService.Db.Iam;
using ChromeRiverService.Db.NciCommon;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Interfaces;
using IAMRepository.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace ChromeRiverService.Classes
{
    public class People (IIamUnitOfWork iamUnitOfWork, INciCommonUnitOfWork nciCommonUnitOfWork, IConfiguration configuration, ILogger<Worker> logger, IHttpHelper httpHelper, IMapper mapper) : IPeople
    {
        private readonly IConfiguration _config = configuration;
        private readonly ILogger<Worker> _logger = logger;
        private readonly IHttpHelper _httpHelper= httpHelper;
        private readonly IIamUnitOfWork _iamUnitOfWork = iamUnitOfWork;
        private readonly INciCommonUnitOfWork _nciCommonUnitOfWork = nciCommonUnitOfWork;
        private readonly IMapper _mapper = mapper;

        private static int NumUpserted = 0;
        private static int NumNotUpserted = 0;
        private static int NumSetToDisabled = 0;
        private static int DeactivationWindowLength = 10;


        public async Task Upsert()
        {
            try
            {   
                string upsertPeopleEndPoint = _config.GetValue<string>("UPSERT_PEOPLE_ENDPOINT") ?? throw new Exception("UPSERT_PEOPLE_ENDPOINT is null");
                string patchPeopleEndPoint = _config.GetValue<string>("PATCH_PERSON_ENDPOINT") ?? throw new Exception("PATCH_PEOPLE_ENDPOINT is null");
                int batchSize = _config.GetValue<int>("UPSERT_PEOPLE_ENDPOINT_BATCH_LIMIT");
                int batchNum = 0;

                IEnumerable<Person> people = await _iamUnitOfWork.People.GetAll(
                filter: p => p.IsEmployee.HasValue && p.IsEmployee.Value &&
                                (
                                    p.CodeIdemploymentStatus == (int)Codes.People.ActiveEmployee ||
                                    p.CodeIdemploymentStatus == (int)Codes.People.OnLeaveEmployee ||
                                    p.CodeIdemploymentStatus == (int)Codes.People.SupspendedEmployee ||
                                    p.CodeIdemploymentStatus == (int)Codes.People.RevokedEmployee ||
                                    (p.CodeIdemploymentStatus == (int)Codes.People.TerminatedEmployee && p.EndDate > DateTime.Now.AddDays(-DeactivationWindowLength))
                                ),
                include: source => source
                                .Include(p => p.PersonPrograms.Where(pp => pp.EndDt == null)).ThenInclude(pp => pp.Program).ThenInclude(p => p.Department)
                                .Include(p => p.ManagerPeople.Where(mp => mp.EndDt == null)).ThenInclude(mp => mp.ManagerPerson)
                                .Include(p => p.JobTitles.Where(jt => jt.EndDt == null))
                                .Include(p => p.DomainEntityPeople.Where(dp => dp.Active))
                );

                IEnumerable<IEnumerable<Person>> peopleBatches = people.Chunk(batchSize);
                IEnumerable<VwChromeRiverGetVendorInfo>? vendorInfo = await _nciCommonUnitOfWork.Vendors.GetAll() ?? throw new Exception("Call to get vendor returned null");
                IEnumerable<VwGetChromeRiverRoles>? companyWideRoles = await _nciCommonUnitOfWork.Roles.GetAll() ?? throw new Exception("Call to get role returned null");

                foreach (IEnumerable<Person> peopleBatch in peopleBatches)
                {
                    batchNum++;
                    ICollection<PersonDto> personDtos = [];

                    try
                    { 
                        foreach (Person person in peopleBatch)
                        {
                            if (person.EndDate is not null)
                            {
                                await _httpHelper.ExecutePostOrPatch($"{patchPeopleEndPoint}/{person.EmployeeId}", new { Status = "DELETED" } , isPatch: true);
                                _logger.LogInformation("{FirstName} {LastName} with employeeID {EmployeeId} status was set to disabled", person.FirstName, person.LastName, person.EmployeeId);
                                NumSetToDisabled++;
                            }
                            else
                            {
                                try
                                {
                                    PersonDto personDto = _mapper.Map<Person, PersonDto>(person);
                                    _mapper.Map(vendorInfo, personDto);
                                    _mapper.Map(companyWideRoles, personDto);

                                    personDtos.Add(personDto);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Exception thrown while mapping {FirstName} {LastName} with employeeID {EmployeeId}",person.FirstName,person.LastName,person.EmployeeId);
                                    NumNotUpserted++;
                                }
                            }

                        }

                        if (personDtos.Count == 0) 
                        {
                            throw new Exception($"Person batch #{batchNum} mapping completely failed");
                        }
                        
                        HttpResponseMessage? response = await _httpHelper.ExecutePostOrPatch<IEnumerable<PersonDto>>(upsertPeopleEndPoint, personDtos, isPatch: false);

                        if (response is not null)
                        {
                            if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.AllUpsertedSuccessfully))
                            {
                                NumUpserted += personDtos.Count;
                                _logger.LogInformation("All people in the batch where successfully upserted: {successBatch}", JsonSerializer.Serialize(personDtos));
                            }
                            else if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.SomeUpsertedSuccessfully))
                            {
                                JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
                                string responseContent = await response.Content.ReadAsStringAsync();
                                IEnumerable<PersonResponse> personResponses = JsonSerializer.Deserialize<IEnumerable<PersonResponse>>(responseContent, options) ?? throw new Exception("PersonResponse Json deserialize error");
                                ICollection<PersonDto> successfulPeople = [];

                                foreach (PersonResponse personResponse in personResponses)
                                {
                                    try
                                    {
                                        PersonDto currentPersonDto = personDtos.FirstOrDefault(p => p.PersonUniqueId == personResponse.PersonUniqueId) ?? throw new Exception($"Person response with PersonUniqueId {personResponse.PersonUniqueId} could not be mapped to a dto for either success or error messaging");

                                        if (personResponse.Result.Equals("success", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            successfulPeople.Add(currentPersonDto);
                                            NumUpserted++;
                                        }
                                        else
                                        {
                                            if (personResponse.ErrorMessage.Contains("Person with username") && personResponse.ErrorMessage.Contains("already exists"))
                                            {
                                                _logger.LogError("Person not updated because the username is already in use, this can happen if the person was manually created and there is a disconnect with the persons service object, or if there is duplicate entity names in the database | Name: {firstName} {lastName} | Employee ID: {employeeID}", currentPersonDto.FirstName, currentPersonDto.LastName, currentPersonDto.PersonUniqueId);
                                            }
                                            else
                                            {
                                                _logger.LogError("Uncategorized person error | Error: {errorMessage}, PersonDto: {dto}", personResponse.ErrorMessage, JsonSerializer.Serialize(currentPersonDto));
                                            }

                                            NumNotUpserted++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex , "Expection processing person upsert responses");
                                    }

                                }

                                _logger.LogInformation("People upserted in a partially successful batch: {successBatch}", JsonSerializer.Serialize(successfulPeople));
                            }
                            else
                            {
                                throw new Exception("Person success message type not handled");
                            }
                        }
                        else
                        {
                            _logger.LogError("The response for person batch #{batchNum} returned a null", batchNum);
                            NumNotUpserted += personDtos.Count;
                        }
                    }
                    catch (Exception ex)    
                    {
                        _logger.LogError(ex,"Exception thrown while processing people batch #{batchNum}", batchNum);
                    }
                }

                _logger.LogInformation("People Upsert Complete | Total People Upserted: {NumUpserted} | Total People Not Upserted: {NumNotUpserted} | {NumSetToDisabled} were set to disabled due to termination in the last {deactivationWidowLength} days", NumUpserted, NumNotUpserted, NumSetToDisabled, DeactivationWindowLength);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,"People exception thrown after {NumUpserted} were upserted | {NumNotUpserted} were not sent or returned unsuccessful | {NumSetToDisabled} were set to disabled due to termination in the last {deactivationWidowLength} days", NumUpserted, NumNotUpserted, NumSetToDisabled, DeactivationWindowLength);
            }
        }
    }
}





