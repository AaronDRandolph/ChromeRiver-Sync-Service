using ChromeRiverService.Db.NciCommon.Interfaces;

namespace IamSyncService.Db.NciCommon
{
    public interface INciCommonUnitOfWork
    {
        IVwChromeRiverGetAllEntityRepository Entities { get; }
        IVwChromeRiverGetVendorInfoRepository Vendors { get; }
        IVwChromeRiverGetAllAllocationRepository Allocations { get; }
        IVwGetChromeRiverRolesRepository Roles { get; }
    }
}