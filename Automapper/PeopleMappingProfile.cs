using System.Text.Json;
using AutoMapper;
using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.DTOs.Subclasses;
using ChromeRiverService.Classes.Helpers;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using IAMRepository.Models;

namespace ChromeRiverService.Automapper
{
    public class PersonMappingProfile : Profile
    {

        readonly Dictionary<string, string> divisionMapper = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(".\\Automapper\\JSON\\DivisionMappings.json")) ?? throw new Exception("Division mapper json file could not be found or could not be deserialized");
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
                .ForMember(dest => dest.SuperDelegate, opt => opt.MapFrom(src => GetIsIT(src)))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => GetUserName(src)))
                .ForMember(dest => dest.AdminAccess, opt => opt.MapFrom(src => GetIsIT(src)))
                .ForMember(dest => dest.DefaultMosaic, opt => opt.MapFrom(src => "Primary"))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => GetJobTitle(src)))
                .ForMember(dest => dest.PrimaryCurrency, opt => opt.MapFrom(src => "USD"))
                .AfterMap((src, dest) => dest.PersonEntities = GetPersonEntities(src))

                //ignores
                .ForMember(dest => dest.PersonEntities, opt => opt.Ignore())
                .ForMember(dest => dest.VendorCode1, opt => opt.Ignore())
                .ForMember(dest => dest.VendorCode2, opt => opt.Ignore());

            // Only custom mapping, nothing automatic
            CreateMap<IEnumerable<VwChromeRiverGetVendorInfo>, PersonDto>(MemberList.None)
                .AfterMap((src, dest) => dest.VendorCode1 = GetPersonVendorInfo(src, dest)?.VendorCode1 ?? "")
                .AfterMap((src, dest) => dest.VendorCode2 = GetPersonVendorInfo(src, dest)?.VendorCode2 ?? "");

            // Only custom mapping, nothing automatic
            CreateMap<IEnumerable<VwGetChromeRiverRoles>, PersonDto>(MemberList.None)
                .AfterMap((src, dest) => {
                    PersonEntity? personRoleEntity = GetFirmWideRoleEntity(src, dest);
                    if (personRoleEntity is not null)
                    {
                        dest.PersonEntities?.Add(personRoleEntity);
                    }
                });
        }


        private static string GetManagerID(Person person) => GetManager(person)?.EmployeeId ?? throw new ArgumentNullException(nameof(person.EmployeeId));

        private static string GetPrimaryEmailAddress(Person person) => $"{GetUserName(person)}@bakerripley.org";

        private static Person GetManager(Person person) => person.ManagerPeople?.FirstOrDefault()?.ManagerPerson ?? throw new ArgumentNullException("Manager object cannot be null");
        private static string GetUserName (Person person) => person.DomainEntityPeople?.FirstOrDefault()?.EntityName ?? throw new ArgumentNullException("Domain entity name cannot be null");

        private static bool GetIsIT (Person person) => GetDepartment(person).DepartmentId.Equals(Codes.People.ITDepartment);

        private static string GetJobTitle (Person person) => person.JobTitles.Where(jt => jt.EndDt == null)?.FirstOrDefault()?.Title ?? throw new ArgumentNullException(nameof(person.JobTitles));

        private static string GetAccountStatus (Person person) => person.CodeIdemploymentStatus.Equals((int)Codes.People.ActiveEmployee) ? "Pending" : "Deleted";

        private static Department GetDepartment (Person person) => person.PersonPrograms?.Where(pp => pp.Program?.Department != null)?.FirstOrDefault()?.Program?.Department ?? throw new ArgumentNullException("A Person's department cannot be null");

        private static string GetManagerName(Person person)
        {
            Person manager = GetManager(person);
            string fullName = $"{manager.FirstName} {manager.LastName}";
            return fullName == "" ? throw new ArgumentNullException("Managers name cannot be empty") : fullName;
        }

        ICollection<PersonEntity> GetPersonEntities( Person person)
        {
            string? departmentName = GetDepartment(person)?.DepartmentName;
            return  
            [ 
                new PersonEntity() {RoleName = "Part Of", EntityTypeCode = "Division", EntityCode =  departmentName is not null ? divisionMapper[departmentName] : null}, 
                new PersonEntity() {RoleName = "Part Of", EntityTypeCode = "Department", EntityCode = departmentName}
            ];
        }

        private static PersonEntity? GetFirmWideRoleEntity( IEnumerable<VwGetChromeRiverRoles> firmWideRoles, PersonDto personDto)
        {
            return  firmWideRoles.FirstOrDefault(role => role.EmployeeId == personDto.PersonUniqueId) is not null ?
                new PersonEntity () {RoleName = firmWideRoles.FirstOrDefault(role => role.EmployeeId == personDto.PersonUniqueId)?.ApRole, EntityTypeCode = "Firmwide",EntityCode = "Firmwide"} : null;
        }

        private static VwChromeRiverGetVendorInfo? GetPersonVendorInfo(IEnumerable<VwChromeRiverGetVendorInfo> vendorInfo, PersonDto person)
        {
            return vendorInfo?.FirstOrDefault(vInfo => vInfo.EmployeeId == person.PersonUniqueId);
        }
    }
}