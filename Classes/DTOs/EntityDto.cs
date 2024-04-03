using ChromeRiverService.Classes.DTOs.Subclasses;

namespace ChromeRiverService.Classes.DTOs
{ public class EntityDto
 
    {
        public string? EntityCode { get; set; }
        public string? EntityTypeCode { get; set; }
        public string? ExtraData1 { get; set; }
        public IEnumerable<EntityName>? EntityNames { get; set; }
    }
}