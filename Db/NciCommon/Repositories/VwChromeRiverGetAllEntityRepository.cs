using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Db.NciCommon.Interfaces;
using IAMRepository.Repository;

namespace ChromeRiverService.Db.NciCommon.Repositories
{
    public class VwChromeRiverGetAllEntityRepository(NciCommonContext context) : GenericRepository<VwChromeRiverGetAllEntity> (context), IVwChromeRiverGetAllEntityRepository
    {
    }
}