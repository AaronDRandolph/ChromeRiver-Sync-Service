using System.Text.Json;

namespace ChromeRiverService.Interfaces
{
    public interface IBase
    {
        Task Upsert();
    }
}