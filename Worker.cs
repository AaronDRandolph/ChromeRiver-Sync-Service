using ChromeRiverService.Interfaces;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace ChromeRiverService;

public class Worker(ISynchUnitOfWork synchUnitOfWork) : BackgroundService
{
    private readonly ISynchUnitOfWork _synchUnitOfWork = synchUnitOfWork;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        await _synchUnitOfWork.Entities().Upsert();
        await _synchUnitOfWork.People().Upsert();
        await _synchUnitOfWork.Allocations().Upsert();

        Environment.Exit(0);
    }
}

