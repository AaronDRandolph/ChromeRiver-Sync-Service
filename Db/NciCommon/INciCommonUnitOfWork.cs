using ChromeRiverService.Db.NciCommon.Interfaces;

namespace ChromeRiverService.Db.NciCommon
{
    public interface INciCommonUnitOfWork
    {
        IVwChromeRiverGetAllEntityRepository Entities { get; }
        IVwChromeRiverGetVendorInfoRepository Vendors { get; }
        IVwChromeRiverGetAllAllocationRepository Allocations { get; }
    }
}