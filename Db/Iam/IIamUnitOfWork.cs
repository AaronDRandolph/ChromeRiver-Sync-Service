
using ChromeRiverService.Db.Iam.Interfaces;

namespace ChromeRiverService.Db.Iam 
{
    public interface IIamUnitOfWork
    {
        IIamPersonRepository People {get;}
    }
}