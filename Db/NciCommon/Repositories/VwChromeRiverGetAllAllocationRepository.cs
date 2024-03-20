using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Db.NciCommon.Interfaces;
using IAMRepository.Repository;

namespace ChromeRiverService.Db.NciCommon.Repositories
{
    public class VwChromeRiverGetAllAllocationRepository(NciCommonContext context) : GenericRepository<VwChromeRiverGetAllAllocation>(context), IVwChromeRiverGetAllAllocationRepository
    {
    }
}