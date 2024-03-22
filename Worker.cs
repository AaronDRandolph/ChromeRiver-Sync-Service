using ChromeRiverService.Interfaces;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace ChromeRiverService;

public class Worker : BackgroundService
{
    private readonly IEntities _entities;
    private readonly IPeople _people;
    private readonly IAllocations _allocations;

    public Worker(IEntities entities, IPeople people, IAllocations allocations)
    {
        _entities = entities;
        _people = people;
        _allocations = allocations;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool upsertEntities = true;
        bool upsertPeople = true;
        bool upsertAllocations = true;

        if (upsertEntities)
        {
            await _entities.Upsert();
        }

        if (upsertPeople)
        {
            await _people.Upsert();
        }

        if (upsertAllocations)
        {
            await _allocations.Upsert();
        }

        Environment.Exit(0);
    }
}

