using ChromeRiverService.Db.Iam.Interfaces;
using IAMRepository;
using IAMRepository.Models;
using IAMRepository.Repository;

namespace ChromeRiverService.Db.Iam.Repositories
{
    public class IamPersonRepository(IamDatabaseContext context) : GenericRepository<Person>(context), IIamPersonRepository
    {
    }
}