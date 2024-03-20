using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.HelperClasses;
using ChromeRiverService.Db.Iam;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Interfaces;
using IAMRepository.Models;
using IamSyncService.Db.NciCommon;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Task = System.Threading.Tasks.Task;

namespace ChromeRiverService.Classes {
    public class People (IIamUnitOfWork iamUnitOfWork, INciCommonUnitOfWork nciCommonUnitOfWork, IConfiguration configuration, ILogger<Worker> logger, IHttpHelper httpHelper) : IPeople
    {
        private readonly IConfiguration _config = configuration;
        private readonly ILogger<Worker> _logger = logger;
        private readonly IHttpHelper _httpHelper= httpHelper;
        private readonly IIamUnitOfWork _iamUnitOfWork = iamUnitOfWork;
        private readonly INciCommonUnitOfWork _nciCommonUnitOfWork = nciCommonUnitOfWork;

        private const int BatchSize = 100;
        private static int NumUpserted = 0;
        private static int NumNotUpserted = 0;


        public async Task Upsert()
        {
            try
            {   
                int deactivationWidowLength = 7;
                string upsertPeopleEndPoint = _config.GetValue<string>("UPSERT_PEOPLE_ENDPOINT") ?? throw new Exception("UPSERT_PEOPLE_ENDPOINT is null");

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
                                .Include(p => p.JobTitles)
                                .Include(p => p.DomainEntityPeople)
                                .Include(p => p.ContactPeople.Where(cp => cp.ContactTypeId == (int)Codes.People.WorkEmail))
                );

                IEnumerable<Person[]> peopleBatches = people.Chunk(BatchSize);
                IEnumerable<VwChromeRiverGetVendorInfo>? vendorInfo = await _nciCommonUnitOfWork.Vendors.GetAll() ?? throw new Exception("Call to get vendor returned null");
                IEnumerable<VwGetChromeRiverRoles>? companyWideRoles = await _nciCommonUnitOfWork.Roles.GetAll() ?? throw new Exception("Call to get role returned null");

                foreach (Person[] peopleBatch in peopleBatches)
                {
                    List<PersonDto> personDtos = [];

                    foreach (Person person in peopleBatch)
                    {
                        Person? manager = person.ManagerPeople?.FirstOrDefault()?.ManagerPerson;
                        Department? department = person.PersonPrograms.Where(pp => pp?.Program?.Department != null).FirstOrDefault()?.Program?.Department;
                        PersonDto personDto = new (person,manager,department,vendorInfo,companyWideRoles);

                        string nullPropertiesLog = NullChecker.GetNullPropertiesLog(personDto, $"PersonUniqueId:{personDto.PersonUniqueId ?? "WARNING => NONE"}");
                        if (nullPropertiesLog.Equals(string.Empty))
                        {
                            _logger.LogError(nullPropertiesLog);
                            NumNotUpserted++;
                        }
                        else
                        {
                            personDtos.Add(personDto);
                        }
                    }

                    HttpResponseMessage response = await _httpHelper.ExecutePost<IEnumerable<PersonDto>>(upsertPeopleEndPoint, personDtos);
                    if (response is not null)
                    {
                        IEnumerable<PersonResponse>? personResponses = JsonSerializer.Deserialize<IEnumerable<PersonResponse>>(response.Content.ReadAsStringAsync().Result) ?? throw new Exception("No response recieved for person upsert");

                        foreach (PersonResponse personResponse in personResponses)
                        {
                            if (personResponse.Result.ToLower().Equals("success"))
                            {
                                _logger.LogInformation(GetLog(Codes.ResultType.OneUpserted.ToString(), personResponse: personResponse, people: people));
                                NumUpserted++;
                            }
                            else
                            {
                                if (personResponse.ErrorMessage.Contains("Person with username") && personResponse.ErrorMessage.Contains("already exists"))
                                {
                                    _logger.LogError(GetLog(Codes.ResultType.ManuallyCreatedPeopleAreNotUpdated.ToString(), personResponse: personResponse, people: people));
                                }
                                else if (personResponse.ErrorMessage.Contains("") && personResponse.ErrorMessage.Contains("")) // GET THIS__________________________________________________________________________
                                {
                                    _logger.LogError(GetLog(Codes.ResultType.PersonManagerMissing.ToString(), personResponse: personResponse, people: people));
                                }
                                else
                                {
                                    _logger.LogError(GetLog(Codes.ResultType.UncategorizedError.ToString(), personResponse: personResponse, people: people));
                                }

                                NumNotUpserted++;
                            }

                        }
                    }
                    else
                    {
                        NumNotUpserted += personDtos.Count;

                    }
                }
                _logger.LogInformation(GetLog(Codes.ResultType.AllUpsertsComplete.ToString()));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error after {NumUpserted} people upserted: {ex.Message}");
            }
        }


        private class PersonResponse : Response
        {
            [JsonPropertyName("personUniqueId")]
            public string PersonUniqueId { get; set; } = "";

        }

        private static string GetLog(string resultType, PersonDto? personDto = null, Person? person = null, PersonResponse? personResponse = null, IEnumerable<Person>? people = null)
        {
            if (resultType.Equals(Codes.ResultType.AllUpsertsComplete.ToString()))
            {
                return $"Total Users Upserted: {NumUpserted} \n Total Users Not Upserted: {NumNotUpserted}";
            }
            else
            {
                StringBuilder log = new($"Upsert Type: People | Result Type: {RegexHelper.PlaceSpacesBeforeUppercase(resultType)}");
                string pipe = " | ";

                if (person is not null)
                {
                    log.Append(pipe).Append($"Name: {person.FirstName} {person.LastName}");
                    log.Append(pipe).Append($"Employee ID: {person.EmployeeId}");
                    return log.ToString();
                }
                else if (personDto is not null)
                {
                    log.Append(pipe).Append($"Name: {personDto.FirstName} {personDto.LastName}");
                    log.Append(pipe).Append($"Employee ID: {personDto.PersonUniqueId}");
                    return log.ToString();
                }
                else if (personResponse is not null && people is not null)
                {
                    Person? mappedPerson = people.ToList().Find(p => p.EmployeeId == personResponse.PersonUniqueId);
                    if (mappedPerson is not null)
                    {
                        log.Append(pipe).Append($"Name: {mappedPerson.FirstName} {mappedPerson.LastName}");
                        log.Append(pipe).Append($"Employee ID: {mappedPerson.EmployeeId}");
                        log.Append(pipe).Append($"ErrorMessage: {personResponse.ErrorMessage}");
                        return log.ToString();
                    }
                    
                    throw new Exception("Person object required to create an error log");
                }

                throw new Exception("Person object required to create an error log");
            }
    
        }

    }
}





