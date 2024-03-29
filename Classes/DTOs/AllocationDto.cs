namespace ChromeRiverService.Classes.DTOs
{
    public class AllocationDto()
    {
        public string? AllocationName {get; set;}
        public string? AllocationNumber {get; set;}
        public string? ClientName  {get; set;}
        public string? ClientNumber  {get; set;}
        public string? Currency  {get; set;}
        public string ClosedDate {get; set;} = "";
        public string? Type  {get; set;}
        public string? Locale  {get; set;}
        public string? OnSelect1EntityTypeCode  {get; set;}
        public string? OnSelect2EntityTypeCode  {get; set;}
    }
}