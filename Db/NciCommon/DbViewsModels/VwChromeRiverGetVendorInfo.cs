using System;
using System.Collections.Generic;

namespace ChromeRiverService.Db.NciCommon.DbViewsModels;

public partial class VwChromeRiverGetVendorInfo
{
    public string? EmployeeId { get; set; }

    public string EmployeeName { get; set; } = null!;

    public string? VendorCode1 { get; set; }

    public string VendorCode2 { get; set; } = null!;
}
