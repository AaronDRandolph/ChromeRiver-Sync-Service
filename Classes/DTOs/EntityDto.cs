using ChromeRiverService.Classes.DTOs.Subclasses;
using ChromeRiverService.Db.NciCommon.DbViewsModels;

namespace ChromeRiverService.Classes.DTOs
{ public class EntityDto
 
    {
        public string? EntityCode { get; set; }
        public string? EntityTypeCode { get; set; }
        public string? ExtraData1 { get; set; }
        public ICollection<EntityName>? EntityNames { get; set; }
    }
}