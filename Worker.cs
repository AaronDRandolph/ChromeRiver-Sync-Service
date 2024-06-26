using ChromeRiverService.Classes.Helpers;
using ChromeRiverService.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace ChromeRiverService;

public class Worker(ISynchUnitOfWork synchUnitOfWork, ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly ISynchUnitOfWork _synchUnitOfWork = synchUnitOfWork;
    private readonly IConfiguration _configuration = configuration;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _synchUnitOfWork.Entities().Upsert();
            await _synchUnitOfWork.People().Upsert();
            await _synchUnitOfWork.Allocations().Upsert();
            
            if (ErrorsSummary.ContainsErrors())
            {
                await ErrorsSummary.SendEmail(_configuration);
            }

            //terminate the service with no error code
            Environment.Exit(0);
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            ErrorsSummary.IncrementNumHighPriorityErrors();
            _logger.LogCritical(ex, "Unhandled exception thrown in one of the sync methods");
            await ErrorsSummary.SendEmail(_configuration);


            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
}

