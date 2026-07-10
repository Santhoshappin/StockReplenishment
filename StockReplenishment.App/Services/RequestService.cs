namespace StockReplenishment.App.Services;

public class RequestService(AppDbContext db, IStockCheckQueue queue) : IRequestService
{
    public async Task<PagedResult<RequestDto>> ListAsync(
        RequestStatus? status, Priority? priority, Guid? locationId, int page, int pageSize)
    {
        var query = db.Requests
            .Include(r => r.Location).Include(r => r.CreatedBy).Include(r => r.Lines)
            .AsQueryable();
        if (status is not null) query = query.Where(r => r.Status == status);
        if (priority is not null) query = query.Where(r => r.Priority == priority);
        if (locationId is not null) query = query.Where(r => r.LocationId == locationId);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => RequestDto.From(r))
            .ToListAsync();

        return new PagedResult<RequestDto>(items, total);
    }

    public async Task<RequestDto?> GetAsync(Guid id)
    {
        var request = await FindAsync(id);
        return request is null ? null : RequestDto.From(request);
    }

    public async Task<RequestDto> CreateAsync(CreateRequestDto dto, Guid actorId)
    {
        var actor = await LoadUserAsync(actorId);
        if (!await db.Locations.AnyAsync(l => l.Id == dto.LocationId))
            throw new ArgumentException($"Unknown location '{dto.LocationId}'.");

        var request = new ReplenishmentRequest
        {
            LocationId = dto.LocationId,
            CreatedById = actor.Id,
            Priority = dto.Priority,
            Lines = dto.Lines.Select(l => new RequestLineItem
            {
                ArticleNumber = l.ArticleNumber,
                Description = l.Description,
                RequestedQuantity = l.RequestedQuantity,
            }).ToList(),
        };
        db.Requests.Add(request);
        await db.SaveChangesAsync();
        return (await GetAsync(request.Id))!;
    }

    public async Task<RequestDto> SubmitAsync(Guid id, Guid actorId)
    {
        var (request, actor) = await LoadAsync(id, actorId);
        request.Submit(actor);
        await db.SaveChangesAsync();
        await queue.EnqueueAsync(id);
        return RequestDto.From(request);
    }

    public async Task<RequestDto> ApproveAsync(Guid id, Guid actorId)
    {
        var (request, actor) = await LoadAsync(id, actorId);
        request.Approve(actor);
        await db.SaveChangesAsync();
        return RequestDto.From(request);
    }

    public async Task<RequestDto> RejectAsync(Guid id, Guid actorId, string? reason)
    {
        var (request, actor) = await LoadAsync(id, actorId);
        request.Reject(actor, reason);
        await db.SaveChangesAsync();
        return RequestDto.From(request);
    }

    public async Task<RequestDto> FulfillAsync(Guid id, Guid actorId, IReadOnlyDictionary<Guid, int> fulfilledByLine)
    {
        var (request, actor) = await LoadAsync(id, actorId);
        request.Fulfill(actor, fulfilledByLine);
        await db.SaveChangesAsync();
        return RequestDto.From(request);
    }

    private async Task<(ReplenishmentRequest Request, AppUser Actor)> LoadAsync(Guid id, Guid actorId)
    {
        var request = await FindAsync(id)
            ?? throw new KeyNotFoundException($"Request '{id}' was not found.");
        return (request, await LoadUserAsync(actorId));
    }

    private Task<ReplenishmentRequest?> FindAsync(Guid id) =>
        db.Requests.Include(r => r.Location).Include(r => r.CreatedBy).Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id);

    private async Task<AppUser> LoadUserAsync(Guid actorId) =>
        await db.Users.FindAsync(actorId)
        ?? throw new ArgumentException($"Unknown user '{actorId}'.");
}
