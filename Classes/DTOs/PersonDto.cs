using ChromeRiverService.Classes.DTOs.Subclasses;

namespace ChromeRiverService.Classes.DTOs
{
    public class PersonDto
    {
        public bool? AdminAccess { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }

        public string? PersonUniqueId { get; set; }
        public string? Status { get; set; }

        public string? ReportsToPersonUniqueId { get; set; }
        public string? ReportsToPersonName { get; set; }
        public string? PrimaryEmailAddress { get; set; }
        public string? Title { get; set; }

        public string? PrimaryCurrency { get; set; }
        public string? DefaultMosaic { get; set; }
        public string? VendorCode1 { get; set; }
        public string? VendorCode2 { get; set; }
        public bool? SuperDelegate { get; set; }
        public ICollection<PersonEntity>? PersonEntities { get; set; }

    }
}


