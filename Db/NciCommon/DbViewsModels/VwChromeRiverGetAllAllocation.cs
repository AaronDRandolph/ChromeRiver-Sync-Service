namespace ChromeRiverService.Db.NciCommon.DbViewsModels;

public partial class VwChromeRiverGetAllAllocation
{
    public string? AllocationName { get; set; }

    public string? AllocationNumber { get; set; }

    public string ClientName { get; set; } = null!;

    public string ClientNumber { get; set; } = null!;

    public string Currency { get; set; } = null!;

    public string OnSelect1EntityTypeCode { get; set; } = null!;

    public string OnSelect2EntityTypeCode { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? CloseDate { get; set; }
}
