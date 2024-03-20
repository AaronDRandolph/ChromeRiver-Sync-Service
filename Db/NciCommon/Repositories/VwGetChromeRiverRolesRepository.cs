using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Db.NciCommon.Interfaces;
using IAMRepository.Repository;

namespace ChromeRiverService.Db.NciCommon.Repositories
{
    public class VwGetChromeRiverRolesRepository (NciCommonContext context ) : GenericRepository<VwGetChromeRiverRoles>(context), IVwGetChromeRiverRolesRepository
    {
    }
}