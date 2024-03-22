
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ChromeRiverService.Db.NciCommon.DbViewsModels;

namespace ChromeRiverService.Classes.DTOs
{ public class EntityDto ( VwChromeRiverGetAllEntity entity)
 
 {
        public string? EntityCode { get; set; } = entity?.EntityCode?.Trim();
        public string? EntityTypeCode { get; set; } = entity?.EntitytypeCode;
        public string? ExtraData1 { get; set; } = entity?.Extradata1;
    public class EntityName
    {
        public string? Name { get; set; } 
        public string? Locale { get; set; } 
    }
    public List<EntityName>? EntityNames { get; set; } = [new() { Name = entity?.EntityName, Locale = "en" }];   
    }
}