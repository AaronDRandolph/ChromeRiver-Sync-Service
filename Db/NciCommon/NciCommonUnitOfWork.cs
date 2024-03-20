
using System.Runtime.CompilerServices;
using ChromeRiverService.Db.NciCommon.DbViewsModels;
using ChromeRiverService.Db.NciCommon.Interfaces;
using ChromeRiverService.Db.NciCommon.Repositories;
using IamSyncService.Db.NciCommon;
using Microsoft.EntityFrameworkCore;

namespace ChromeRiverService 
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
        private IVwGetChromeRiverRolesRepository? _roles;

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

        public IVwGetChromeRiverRolesRepository Roles
        {
            get
            {
                _roles ??= new VwGetChromeRiverRolesRepository(_context);
                return _roles;
            }
        } 
    }


}