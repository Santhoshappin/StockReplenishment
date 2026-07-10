namespace StockReplenishment.App.Services;

public class StockCheckWorker(
    IStockCheckQueue queue,
    IStockAvailabilityService stockService,
    IServiceScopeFactory scopeFactory,
    ILogger<StockCheckWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var requestId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var request = await db.Requests.Include(r => r.Lines)
                    .FirstOrDefaultAsync(r => r.Id == requestId, stoppingToken);
                if (request is null || request.StockCheck != StockCheckStatus.Pending) continue;

                var result = await stockService.CheckAsync(request, stoppingToken);
                request.ApplyStockCheck(result.Status, result.AvailableByLine);
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Stock check failed for request {RequestId}", requestId);
            }
        }
    }
}
