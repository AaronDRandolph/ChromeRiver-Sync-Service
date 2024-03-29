using AutoMapper;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.HelperClasses;
using ChromeRiverService.Db.Iam;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Interfaces;
using IAMRepository.Models;
using IamSyncService.Db.NciCommon;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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


        public async Task Upsert()
        {
            try
            {   
                int deactivationWidowLength = 7;
                string upsertPeopleEndPoint = _config.GetValue<string>("UPSERT_PEOPLE_ENDPOINT") ?? throw new Exception("UPSERT_PEOPLE_ENDPOINT is null");
                int batchSize = _config.GetValue<int>("UPSERT_PEOPLE_ENDPOINT_BATCH_LIMIT");
                int batchNum = 0;

                IEnumerable<Person> people = await _iamUnitOfWork.People.GetAll(
                filter: p => p.IsEmployee.HasValue && p.IsEmployee.Value &&
                                (
                                    p.CodeIdemploymentStatus == (int)Codes.People.ActiveEmployee ||
                                    p.CodeIdemploymentStatus == (int)Codes.People.OnLeaveEmployee ||
                                    p.CodeIdemploymentStatus == (int)Codes.People.SupspendedEmployee ||
                                    p.CodeIdemploymentStatus == (int)Codes.People.RevokedEmployee ||
                                    (p.CodeIdemploymentStatus == (int)Codes.People.TerminatedEmployee && p.EndDate > DateTime.Now.AddDays(-deactivationWidowLength))
                                ),
                include: source => source
                                .Include(p => p.PersonPrograms.Where(pp => pp.EndDt == null)).ThenInclude(pp => pp.Program).ThenInclude(p => p.Department)
                                .Include(p => p.ManagerPeople.Where(mp => mp.EndDt == null)).ThenInclude(mp => mp.ManagerPerson)
                                .Include(p => p.JobTitles.Where(jt => jt.EndDt == null))
                                .Include(p => p.DomainEntityPeople.Where(dp => dp.Active))
                                .Include(p => p.ContactPeople.Where(cp => cp.ContactTypeId == (int)Codes.People.WorkEmail))
                );

                IEnumerable<IEnumerable<Person>> peopleBatches = people.Chunk(batchSize);
                IEnumerable<VwChromeRiverGetVendorInfo>? vendorInfo = await _nciCommonUnitOfWork.Vendors.GetAll() ?? throw new Exception("Call to get vendor returned null");
                IEnumerable<VwGetChromeRiverRoles>? companyWideRoles = await _nciCommonUnitOfWork.Roles.GetAll() ?? throw new Exception("Call to get role returned null");

                foreach (IEnumerable<Person> peopleBatch in peopleBatches)
                {
                    batchNum++;
                    IList<PersonDto> personDtos = [];

                    try
                    { 
                        foreach (Person person in peopleBatch)
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
                                _logger.LogError("Exception thrown while mapping {firstName} {lastName} with employee id '{EmployeeID}': {ex}", person.FirstName, person.LastName, person.EmployeeId, ex);
                                NumNotUpserted++;
                            }
                        }

                        HttpResponseMessage? response = await _httpHelper.ExecutePost<IEnumerable<PersonDto>>(upsertPeopleEndPoint, personDtos);

                        if (response is not null)
                        {
                            if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.AllUpsertedSuccessfully))
                            {
                                NumUpserted += personDtos.Count;

                                foreach (PersonDto personDto in personDtos)
                                {
                                    _logger.LogInformation("{log}", GetLog(Codes.ResultType.OneUpserted, personDto: personDto));
                                }
                            }
                            else if (((int)response.StatusCode).Equals((int)Codes.HttpResponses.SomeUpsertedSuccessfully))
                            {
                                JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
                                IEnumerable<PersonResponse>? personResponses = JsonSerializer.Deserialize<IEnumerable<PersonResponse>>(response.Content.ReadAsStringAsync().Result, options) ?? throw new Exception("PersonResponse Json deserialize error");
                                int index = 0;

                                foreach (PersonResponse personResponse in personResponses)
                                {

                                    if (personResponse.Result.Equals("success", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        _logger.LogInformation("{log}", GetLog(Codes.ResultType.OneUpserted, personResponse: personResponse, personDto: personDtos[index]));
                                        NumUpserted++;
                                    }
                                    else
                                    {
                                        if (personResponse.ErrorMessage.Contains("Person with username") && personResponse.ErrorMessage.Contains("already exists"))
                                        {
                                            _logger.LogError("{log}", GetLog(Codes.ResultType.PotentiallyConflictsWithManuallyCreatedPerson, personResponse: personResponse, personDto: personDtos[index]));
                                        }
                                        else
                                        {
                                            _logger.LogError("{log}", GetLog(Codes.ResultType.UncategorizedError, personResponse: personResponse, personDto: personDtos[index]));
                                        }

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
                            _logger.LogError("The response for person batch #{batchNum} returned a null", batchNum);
                            NumNotUpserted += personDtos.Count;
                        }
                    }
                    catch (Exception ex)    
                    {
                        _logger.LogError("Error Thrown while processing people batch #{batchNum}: {ex}", batchNum, ex);
                    }
                    break;
                }

                _logger.LogInformation("{log}",GetLog(Codes.ResultType.AllUpsertsComplete));
            }
            catch (Exception ex)
            {
                _logger.LogError("People exception thrown after {NumUpserted} were upserted and {NumNotUpserted} were not sent or returned unsuccessful | Message: {messsage}", NumUpserted, NumNotUpserted, ex.Message);
            }
        }


        private static string GetLog(Codes.ResultType resultType, PersonDto? personDto = null, Person? person = null, PersonResponse? personResponse = null)
        {
            string pipe = " | ";

            StringBuilder log = new StringBuilder("Upsert Type: People")
                             .Append(pipe).Append($"Result Type: {RegexHelper.PlaceSpacesBeforeUppercase(resultType.ToString())}");

            if (resultType.Equals(Codes.ResultType.AllUpsertsComplete))
            {
                return   log.Append(pipe).Append($"Total People Upserted: {NumUpserted}")
                            .Append(pipe).Append($"Total People Not Upserted: {NumNotUpserted}")
                            .ToString();
            }
            else if (personDto is not null)
            {
                 log.Append(pipe).Append($"Name: {personDto.FirstName} {personDto.LastName}")
                    .Append(pipe).Append($"Employee ID: {personDto.PersonUniqueId}");

                if (personResponse is not null && !personResponse.ErrorMessage.Equals(string.Empty))
                {
                         log.Append(pipe).Append($"Error Message: {personResponse.ErrorMessage}")
                            .Append(pipe).Append($"Dto: {JsonSerializer.Serialize(personDto)}");
                }

                return log.ToString();
            }
            else
            {
                throw new Exception("no person type found to create an error log");
            }
        }
    }
}





