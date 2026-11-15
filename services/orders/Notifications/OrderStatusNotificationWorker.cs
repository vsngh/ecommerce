namespace Orders.Api.Notifications;

public sealed class OrderStatusNotificationWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private readonly OrderStatusNotificationDispatcher dispatcher;
    private readonly ILogger<OrderStatusNotificationWorker> logger;

    public OrderStatusNotificationWorker(
        OrderStatusNotificationDispatcher dispatcher,
        ILogger<OrderStatusNotificationWorker> logger)
    {
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await dispatcher.DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to process pending order status notifications.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

}