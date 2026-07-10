namespace StockReplenishment.App.Services;

public interface ILocationService
{
    Task<List<StockLocation>> ListAsync();
    Task<StockLocation> CreateAsync(UpsertLocationDto dto, Guid actorId);
    Task<StockLocation> UpdateAsync(Guid id, UpsertLocationDto dto, Guid actorId);
    Task DeleteAsync(Guid id, Guid actorId);
}
