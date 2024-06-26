using ChromeRiverService.Db.NciCommon.Interfaces;
using ChromeRiverService.Db.NciCommon.Repositories;

namespace ChromeRiverService.Db.NciCommon
{
    public class NciCommonUnitOfWork : INciCommonUnitOfWork
    {
        private readonly NciCommonContext _context;

        public NciCommonUnitOfWork(NciCommonContext context)
        {
            _context = context;
        }

        private IVwChromeRiverGetAllEntityRepository? _entities;
        private IVwChromeRiverGetVendorInfoRepository? _vendors;
        private IVwChromeRiverGetAllAllocationRepository? _allocations;

        public IVwChromeRiverGetAllEntityRepository Entities
        {
            get
            {
                _entities ??= new VwChromeRiverGetAllEntityRepository(_context);
                return _entities;
            }
        }

        public IVwChromeRiverGetVendorInfoRepository Vendors
        {
            get
            {
                _vendors ??= new VwChromeRiverGetVendorInfoRepository(_context);
                return _vendors;
            }
        }

        public IVwChromeRiverGetAllAllocationRepository Allocations
        {
            get
            {
                _allocations ??= new VwChromeRiverGetAllAllocationRepository(_context);
                return _allocations;
            }
        }
    }
}