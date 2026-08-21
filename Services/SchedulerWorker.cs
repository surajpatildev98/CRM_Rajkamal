using CRMWebServices.Interfaces;

namespace CRMWebServices.Services
{
    public class SchedulerWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SchedulerWorker> _logger;

        private static readonly TimeSpan PollInterval =TimeSpan.FromSeconds(10);

        public SchedulerWorker(IServiceScopeFactory scopeFactory,ILogger<SchedulerWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CRM Scheduler Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope =_scopeFactory.CreateScope();
                    var executionService =scope.ServiceProvider.GetRequiredService<ISchedulerExecutionService>();
                    await executionService.ProcessDueItemsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"Unexpected error in CRM Scheduler Worker.");
                }

                try
                {
                    await Task.Delay(PollInterval,stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            _logger.LogInformation("CRM Scheduler Worker stopped.");
        }
    }
}