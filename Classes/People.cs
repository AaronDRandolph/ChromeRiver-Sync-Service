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

namespace ChromeRiverService.Classes {
    public class People (IIamUnitOfWork iamUnitOfWork, INciCommonUnitOfWork nciCommonUnitOfWork, IConfiguration configuration, ILogger<Worker> logger, IHttpHelper httpHelper) : IPeople
    {
        private readonly IConfiguration _config = configuration;
        private readonly ILogger<Worker> _logger = logger;
        private readonly IHttpHelper _httpHelper= httpHelper;
        private readonly IIamUnitOfWork _iamUnitOfWork = iamUnitOfWork;
        private readonly INciCommonUnitOfWork _nciCommonUnitOfWork = nciCommonUnitOfWork;

        private static int NumUpserted = 0;
        private static int NumNotUpserted = 0;


        public async Task Upsert()
        {
            try
            {   
                int deactivationWidowLength = 7;
                string upsertPeopleEndPoint = _config.GetValue<string>("UPSERT_PEOPLE_ENDPOINT") ?? throw new Exception("UPSERT_PEOPLE_ENDPOINT is null");
                int batchSize = _config.GetValue<int>("UPSERT_PEOPLE_ENDPOINT_BATCH_LIMIT");

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
                    List<PersonDto> personDtos = [];

                    foreach (Person person in peopleBatch)
                    {
                        Person? manager = person.ManagerPeople?.FirstOrDefault()?.ManagerPerson;
                        Department? department = person.PersonPrograms.Where(pp => pp?.Program?.Department != null).FirstOrDefault()?.Program?.Department;
                        PersonDto personDto = new (person,manager,department,vendorInfo,companyWideRoles);

                        string nullPropertiesLog = NullChecker.GetNullPropertiesLog(personDto, $"[ Name: {personDto.FirstName} {personDto.LastName}, PersonUniqueId:{personDto.PersonUniqueId ?? "WARNING => NONE"} ]");
                        if (nullPropertiesLog.Equals(string.Empty))
                        {
                            personDtos.Add(personDto);
                        }
                        else
                        {
                            _logger.LogError("{log}", nullPropertiesLog);
                            NumNotUpserted++;
                        }
                    }

                    HttpResponseMessage? response = await _httpHelper.ExecutePost<IEnumerable<PersonDto>>(upsertPeopleEndPoint, personDtos);
                    
                    if (response is not null)
                    {
                        IEnumerable<PersonResponse>? personResponses = JsonSerializer.Deserialize<IEnumerable<PersonResponse>>(response.Content.ReadAsStringAsync().Result) ?? throw new Exception("PersonResponse Json deserialize error");
                        int index = 0;

                        foreach (PersonResponse personResponse in personResponses)
                        {

                            if (personResponse.Result.Equals("success", StringComparison.InvariantCultureIgnoreCase))
                            {
                                _logger.LogInformation("{log}",GetLog(Codes.ResultType.OneUpserted, personResponse: personResponse, personDto: personDtos[index]));
                                NumUpserted++;
                            }
                            else
                            {
                                if (personResponse.ErrorMessage.Contains("Person with username") && personResponse.ErrorMessage.Contains("already exists"))
                                {
                                    _logger.LogError("{log}",GetLog(Codes.ResultType.ManuallyCreatedPeopleAreNotUpdated, personResponse: personResponse, personDto: personDtos[index]));
                                }
                                else
                                {
                                    _logger.LogError("{log}",GetLog(Codes.ResultType.UncategorizedError, personResponse: personResponse, personDto: personDtos[index]));
                                }

                                NumNotUpserted++;
                            }

                            index++;
                        }
                    }
                    else
                    {
                        NumNotUpserted += personDtos.Count;
                    }
                }

                _logger.LogInformation("{log}",GetLog(Codes.ResultType.AllUpsertsComplete));
            }
            catch (Exception ex)
            {
                _logger.LogError("People exception thrown after {NumUpserted} were upserted and {NumNotUpserted} were not sent or returned unsuccessful | Message: {messsage}", NumUpserted, NumNotUpserted, ex.Message);
            }
        }


        private class PersonResponse : Response 
        {
            public string PersonUniqueId { get; set; } = "";   // return empty on Success
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
            else if ( personDto is not null)
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





