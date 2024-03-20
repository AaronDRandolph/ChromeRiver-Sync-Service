using ChromeRiverService.Classes.HelperClasses;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using IAMRepository.Models;

namespace ChromeRiverService.Classes.DTOs
{
    public class PersonDto (Person person, Person? manager, Department? department, IEnumerable<VwChromeRiverGetVendorInfo> vendorInfo, IEnumerable<VwGetChromeRiverRoles> companyWideRoles)
    {
        public bool? AdminAccess { get; set; } = department?.DepartmentId.Equals((int)Codes.People.ITDepartment);
        public string? FirstName { get; set; } = person.FirstName;
        public string? LastName { get; set; } = person.LastName;
        public string? Username { get; set; } = person.DomainEntityPeople?.Where(de => de.Active).LastOrDefault()?.EntityName;

        public string? PersonUniqueId { get; set; } = person.EmployeeId;
        public string? Status { get; set; } = person.CodeIdemploymentStatus.Equals((int)Codes.People.ActiveEmployee) ? "Pending" : "Deleted"; //Our configuration uses "pending" for "active" and "disabled" for "deleted" :)

        public string? ReportsToPersonUniqueId { get; set; } = manager?.EmployeeId;
        public string? ReportsToPersonName { get; set; } = manager is not null ? $"{manager?.FirstName} {manager?.LastName}" : null;
        public string? PrimaryEmailAddress { get; set; } = person.ContactPeople.FirstOrDefault()?.ContactValue; 
        public string? Title { get; set; }  = person.JobTitles.Where(jt => jt.EndDt == null).LastOrDefault()?.Title;

        public string? PrimaryCurrency { get; set; } = "USD";
        public string? DefaultMosaic { get; set; } =  "Primary";
        public string VendorCode1 { get; set; } = vendorInfo?.FirstOrDefault(w => w.EmployeeId == person.EmployeeId)?.VendorCode1 ?? "";
        public string VendorCode2 { get; set; } = vendorInfo?.FirstOrDefault(w => w.EmployeeId == person.EmployeeId)?.VendorCode2 ?? "";
        public bool? SuperDelegate { get; set; } = department?.DepartmentId.Equals((int)Codes.People.ITDepartment);
        public class Entities
        {
            public string? RoleName { get; set; }
            public string? EntityTypeCode { get; set; }
            public string? EntityCode { get; set; }
        }
        public List<Entities>? PersonEntities { get; set; } =  companyWideRoles.FirstOrDefault(role => role.EmployeeId == person.EmployeeId) is not null ?
        [ 
            new() {RoleName = "Part Of", EntityTypeCode = "Division",EntityCode = GetDivisionCode(department?.DepartmentName)}, 
            new() {RoleName = "Part Of", EntityTypeCode = "Department",EntityCode = department?.DepartmentName}, 
            new() {RoleName = companyWideRoles.FirstOrDefault(role => role.EmployeeId == person.EmployeeId)?.ApRole ?? throw new Exception($"Person {person.FirstName} {person.LastName} with Employee Id {person.EmployeeId} is expected to have role"), EntityTypeCode = "Firmwide",EntityCode = "Firmwide"}
        ]
        :  
        [ 
            new() {RoleName = "Part Of", EntityTypeCode = "Division",EntityCode = GetDivisionCode(department?.DepartmentName)}, 
            new() {RoleName = "Part Of", EntityTypeCode = "Department",EntityCode = department?.DepartmentName}
        ];


        static string? GetDivisionCode(string? departmentName)
        {
            // all of this needs to be checked and updated with the new org
            string[] EconmicInitiativeDepartments = ["Economic Initiatives", "Financial Initiatives", "Adult Education"];
            string[] ProgramsDepartments = ["Prog Strategy Plan Eval", "Strategy", "Program Planning & Eval"];
            string[] CommunitySchoolsDepartments = ["Charter School", "Choices in Education","Community School"];
            string[] HeadStartDepartments = ["CACFP", "Early Childhood Education", "Early Head Start", "Head Start", "ECDC", "Child Partnership", "Ft Bend Start Up"];
            string[] CommunityInitiativesDepartments = ["Community Initiatives", "Comm Based Initiatives", "Community Centers", "Immigration", "Youth Programs", "Financial Initiatives", "Leadership", "BNTW", "Summer Youth - OtJ Train"];
            string[] WorkforceInitiativesDepartments = ["Workforce Initiatives", "Public Sector Solutions", "Career Centers", "FAPO", "WFS Coastal Bend", "WFS East Texas", "WFS Rural Capital", "PSS Shared Cost", "Support Srvc for Vet Fams", "ASPIRE", "WFS", "Health Career Pathway Par", "Work Base Learning"];
            string[] RegionalInititivesDepartments = ["Regional Initiatives", "CEAP Energy Assistance", "Disaster Relief", "VITA Program", "Energy Efficiency", "Harvey", "Weatherization", "Energy Aid"];
            string[] HealthAndWellnessInitiativesDepartments = ["Hlth & Wllns Initiatives", "Home Care", "Senior Meals", "Volunteer Services", "Senior Services", "Senior Health Promotion", "Case Management", "Houston Dementia Alliance"];
            string[] DevelopmentDepartments = ["Development", "Marketing", "Turkey Trot", "Marketing & Communication", "Good 2 Go", "Hearty of Gold", "Major Gifts", "Events"];
            string[] AdministrativeDepartments = ["Procurement & Contracts", "Peope & Culture", "Supporting Services", "Loc Specific Shared Cost", "Compliance & QA", "Administration", "Accounting & Finance", "Executive Department", "BPDI", "Compliance Contract Admin", "Executive Admin", "Facilities", "People & Culture", "Finance & Accounting", "Human Resources", "Information Technology", "Procurement", "Credit Union", "Family Dev Credential Pgm", "Adult Day Center", "Agency Sponsored Init"];

            if (EconmicInitiativeDepartments.Contains(departmentName))
            {
                return "EI";
            }
            if (ProgramsDepartments.Contains(departmentName))
            {
                return "PSPE";
            }
            if (CommunitySchoolsDepartments.Contains(departmentName))
            {
                return "CS";
            }
            if (HeadStartDepartments.Contains(departmentName))
            {
                return "HS";
            }
            if (CommunityInitiativesDepartments.Contains(departmentName))
            {
                return "CI";
            }
            if (WorkforceInitiativesDepartments.Contains(departmentName))
            {
                return "WFI";
            }
            if (RegionalInititivesDepartments.Contains(departmentName))
            {
                return "RI";
            }
            if (HealthAndWellnessInitiativesDepartments.Contains(departmentName))
            {
                return "HWI";
            }
            if (DevelopmentDepartments.Contains(departmentName))
            {
                return "DEV";
            }
            if (AdministrativeDepartments.Contains(departmentName))
            {
                return "AD";
            }

            return null;
        }
    }
}


