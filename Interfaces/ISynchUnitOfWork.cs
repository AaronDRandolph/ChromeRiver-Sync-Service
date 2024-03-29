using ChromeRiverService.Classes;

namespace ChromeRiverService.Interfaces
{
    public interface ISynchUnitOfWork
    {
        public IEntities Entities();
        public IPeople People();
        public IAllocations Allocations();
    }
}