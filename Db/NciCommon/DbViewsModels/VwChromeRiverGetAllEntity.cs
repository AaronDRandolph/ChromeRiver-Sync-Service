namespace ChromeRiverService.Db.NciCommon.DbViewsModels;

public partial class VwChromeRiverGetAllEntity
{
    public string? EntityCode { get; set; }

    public string EntitytypeCode { get; set; } = null!;

    public string SortOrder { get; set; } = null!;

    public string? EntityName { get; set; }

    public string? Extradata1 { get; set; }

    public int Active { get; set; }
}
