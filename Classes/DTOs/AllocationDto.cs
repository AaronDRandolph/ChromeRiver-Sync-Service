
using System.Text.Json.Serialization;
using ChromeRiverService.Db.NciCommon.DbViewsModels;

namespace ChromeRiverService.Classes.DTOs
{
  public class AllocationDto(VwChromeRiverGetAllAllocation allocation, string? closeDate = "")
    {
        public string? AllocationName { get; set; } = allocation.AllocationName;
        public string? AllocationNumber { get; set; } = allocation.AllocationNumber;
        public string? ClientName { get; set; } = allocation.ClientName;
        public string? ClientNumber { get; set; } = allocation.ClientNumber;
        public string? Currency { get; set; } = "USD";
        public string? ClosedDate { get; set; } = closeDate;
        public string? Type { get; set; } = allocation.Type;
        public string? Locale { get; set; } = "en";
        public string? OnSelect1EntityTypeCode { get; set; } = allocation.OnSelect1EntityTypeCode;
        public string? OnSelect2EntityTypeCode { get; set; } = allocation.OnSelect2EntityTypeCode;
    }
}