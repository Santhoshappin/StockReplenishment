namespace StockReplenishment.App.Services;

public record StockCheckResult(StockCheckStatus Status, IReadOnlyDictionary<Guid, int> AvailableByLine);

public interface IStockAvailabilityService
{
    Task<StockCheckResult> CheckAsync(ReplenishmentRequest request, CancellationToken cancellationToken);
}

public class FakeStockAvailabilityService : IStockAvailabilityService
{
    public async Task<StockCheckResult> CheckAsync(ReplenishmentRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(3, 9)), cancellationToken);

        var shortfall = Random.Shared.Next(2) == 0;
        var available = request.Lines.ToDictionary(
            l => l.Id,
            l => shortfall ? Random.Shared.Next(0, l.RequestedQuantity) : l.RequestedQuantity);

        return new StockCheckResult(
            shortfall ? StockCheckStatus.Shortfall : StockCheckStatus.Available, available);
    }
}
