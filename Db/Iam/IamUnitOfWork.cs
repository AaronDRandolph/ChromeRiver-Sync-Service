
using ChromeRiverService.Db.Iam.Interfaces;
using ChromeRiverService.Db.Iam.Repositories;
using IAMRepository;

namespace ChromeRiverService.Db.Iam 
{
    public class IamUnitOfWork(IamDatabaseContext context) : IIamUnitOfWork
    {
        private readonly IamDatabaseContext _context = context;

        private IIamPersonRepository? _people;

        public IIamPersonRepository People
        {
            get
            {
                _people ??= new IamPersonRepository(_context);
                return _people;
            }
        }
    }
}

