namespace StockReplenishment.App.Services;

public class LocationService(AppDbContext db) : ILocationService
{
    public Task<List<StockLocation>> ListAsync() =>
        db.Locations.OrderBy(l => l.Code).ToListAsync();

    public async Task<StockLocation> CreateAsync(UpsertLocationDto dto, Guid actorId)
    {
        await RequireReviewerAsync(actorId);
        await RequireUniqueCodeAsync(dto.Code, excludeId: null);

        var location = new StockLocation { Id = Guid.NewGuid(), Code = dto.Code.Trim(), Name = dto.Name.Trim() };
        db.Locations.Add(location);
        await db.SaveChangesAsync();
        return location;
    }

    public async Task<StockLocation> UpdateAsync(Guid id, UpsertLocationDto dto, Guid actorId)
    {
        await RequireReviewerAsync(actorId);
        var location = await LoadAsync(id);
        await RequireUniqueCodeAsync(dto.Code, excludeId: id);

        location.Code = dto.Code.Trim();
        location.Name = dto.Name.Trim();
        await db.SaveChangesAsync();
        return location;
    }

    public async Task DeleteAsync(Guid id, Guid actorId)
    {
        await RequireReviewerAsync(actorId);
        var location = await LoadAsync(id);
        if (await db.Requests.AnyAsync(r => r.LocationId == id))
            throw new InvalidOperationException(
                $"Location '{location.Code}' cannot be deleted because replenishment requests refer to it.");

        db.Locations.Remove(location);
        await db.SaveChangesAsync();
    }

    private async Task<StockLocation> LoadAsync(Guid id) =>
        await db.Locations.FindAsync(id)
        ?? throw new KeyNotFoundException($"Location '{id}' was not found.");

    private async Task RequireReviewerAsync(Guid actorId)
    {
        var actor = await db.Users.FindAsync(actorId)
            ?? throw new ArgumentException($"Unknown user '{actorId}'.");
        if (actor.Role != UserRole.Reviewer)
            throw new ForbiddenException("Only a reviewer can manage stock locations.");
    }

    private async Task RequireUniqueCodeAsync(string code, Guid? excludeId)
    {
        var normalized = code.Trim().ToUpperInvariant();
        if (await db.Locations.AnyAsync(l => l.Id != excludeId && l.Code.ToUpper() == normalized))
            throw new InvalidOperationException($"A location with code '{code.Trim()}' already exists.");
    }
}
