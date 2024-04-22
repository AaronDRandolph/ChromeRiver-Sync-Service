using System.Text.Json;
using AutoMapper;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.DTOs.Subclasses;
using ChromeRiverService.Classes.Helpers;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using IAMRepository.Models;
using IAMProgram = IAMRepository.Models.Program;

namespace ChromeRiverService.Automapper
{
    public class PersonMappingProfile : Profile
    {

        readonly static Dictionary<string, string> divisionMapper = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(".\\Automapper\\JSON\\DivisionMappings.json")) ?? throw new Exception("Division mapper json file could not be found or could not be deserialized");
        readonly static FirmWideRoleDictionary firmWideRoleDictionary = JsonSerializer.Deserialize<FirmWideRoleDictionary>(File.ReadAllText(".\\Automapper\\JSON\\FirmWideRoleDictionary.json")) ?? throw new Exception("Division mapper json file could not be found or could not be deserialized");
        readonly static Dictionary<string, string> superDelegateWhitelist = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(".\\Automapper\\JSON\\SuperDelegateWhitelist.json")) ?? throw new Exception("Super delegate json file could not be found or could not be deserialized");
        public PersonMappingProfile()
        {
            //Configure the Mappings
            //Mapping IAM Person to PersonDto
            //Source: Person and Destination: PersonDTO

            CreateMap<Person, PersonDto>()
                .AddTransform<string>((str) => str.Trim())
                .ForMember(dest => dest.PrimaryEmailAddress, opt => opt.MapFrom(src => GetPrimaryEmailAddress(src)))
                .ForMember(dest => dest.ReportsToPersonUniqueId, opt => opt.MapFrom(src => GetManagerID(src)))
                .ForMember(dest => dest.ReportsToPersonName, opt => opt.MapFrom(src => GetManagerName(src)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => GetAccountStatus(src)))
                .ForMember(dest => dest.PersonUniqueId, opt => opt.MapFrom(src => src.EmployeeId))
                .ForMember(dest => dest.SuperDelegate, opt => opt.MapFrom(src => IsIT(src) || IsOnSuperDelegateWhitelist(src)))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => GetUserName(src)))
                .ForMember(dest => dest.AdminAccess, opt => opt.MapFrom(src => IsIT(src)))
                .ForMember(dest => dest.DefaultMosaic, opt => opt.MapFrom(src => "Primary"))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => GetJobTitle(src)))
                .ForMember(dest => dest.PrimaryCurrency, opt => opt.MapFrom(src => "USD"))
                .AfterMap((src, dest) => dest.PersonEntities = GetPersonEntities(src))

                //ignores
                .ForMember(dest => dest.VendorCode1, opt => opt.Ignore())
                .ForMember(dest => dest.VendorCode2, opt => opt.Ignore());

            // Only custom mapping, nothing automatic
            CreateMap<IEnumerable<VwChromeRiverGetVendorInfo>, PersonDto>(MemberList.None)
                .AfterMap((src, dest) => dest.VendorCode1 = GetPersonVendorInfo(src, dest)?.VendorCode1 ?? "")
                .AfterMap((src, dest) => dest.VendorCode2 = GetPersonVendorInfo(src, dest)?.VendorCode2 ?? "");

        }

        private static string GetManagerID(Person person) => GetManager(person)?.EmployeeId ?? throw new ArgumentNullException(nameof(person.EmployeeId));

        private static string GetPrimaryEmailAddress(Person person) => $"{GetUserName(person)}@bakerripley.org";

        private static Person GetManager(Person person) => person.ManagerPeople?.FirstOrDefault()?.ManagerPerson ?? throw new ArgumentNullException("Manager object cannot be null");
        private static string GetUserName(Person person) => person.DomainEntityPeople?.FirstOrDefault()?.EntityName ?? throw new ArgumentNullException("Domain entity name cannot be null");

        private static bool IsIT (Person person) => GetDepartment(person).DepartmentId.Equals((int)Codes.Department.IT);

        private static bool IsOnSuperDelegateWhitelist (Person person) => superDelegateWhitelist.ContainsKey(person.EmployeeId ?? throw new ArgumentNullException($"personID {person.PersonId} does not have an EmployeeId"));

        private static string GetJobTitle (Person person) => person.JobTitles.Where(jt => jt.EndDt == null)?.FirstOrDefault()?.Title ?? throw new ArgumentNullException(nameof(person.JobTitles));

        private static string GetAccountStatus (Person person) => person.CodeIdemploymentStatus.Equals((int)Codes.EmploymentStatus.Active) ? "Pending" : "Deleted";

        private static Department GetDepartment (Person person) => person.PersonPrograms?.Where(pp => pp.Program?.Department != null)?.FirstOrDefault()?.Program?.Department ?? throw new ArgumentNullException("A person's department cannot be null");
        private static IAMProgram GetProgram (Person person) => person.PersonPrograms?.FirstOrDefault()?.Program ?? throw new ArgumentNullException("A person's program cannot be null");

        private static string GetManagerName(Person person)
        {
            Person manager = GetManager(person);
            string fullName = $"{manager.FirstName} {manager.LastName}";
            return fullName == "" ? throw new ArgumentNullException("Managers name cannot be empty") : fullName;
        }

        ICollection<PersonEntity> GetPersonEntities( Person person)
        {
            string departmentName = GetDepartment(person).DepartmentName;
            IEnumerable<string>? appRoles = GetFirmWideRoles(person);

            ICollection<PersonEntity> personEntities =   
            [ 
                new PersonEntity() {RoleName = "Part Of", EntityTypeCode = "Division", EntityCode =  divisionMapper[departmentName] is null ? throw new ArgumentNullException("A person's division cannot be null") : divisionMapper[departmentName] }, 
                new PersonEntity() {RoleName = "Part Of", EntityTypeCode = "Department", EntityCode = departmentName}
            ];
            
            if (appRoles is not null)
            {
                foreach (string appRole in appRoles)
                {
                    personEntities.Add( new PersonEntity () {RoleName = appRole, EntityTypeCode = "Firmwide",EntityCode = "Firmwide"});
                }
            }
            
            return personEntities;
        }

        private static IEnumerable<string>? GetFirmWideRoles(Person person)
        {
            string programName = GetProgram(person).ProgramName;
            string jobTitle = GetJobTitle(person);
            IEnumerable<string>? appRoles = null;

            if (firmWideRoleDictionary.ContainsKey(programName))
            {
                if (firmWideRoleDictionary[programName].ContainsKey(jobTitle)) 
                {
                    appRoles = firmWideRoleDictionary[programName][jobTitle];   
                }
            }

            return appRoles;
        }

        private static VwChromeRiverGetVendorInfo? GetPersonVendorInfo(IEnumerable<VwChromeRiverGetVendorInfo> vendorInfo, PersonDto person)
        {
            return vendorInfo?.FirstOrDefault(vInfo => vInfo.EmployeeId == person.PersonUniqueId);
        }
    }
}