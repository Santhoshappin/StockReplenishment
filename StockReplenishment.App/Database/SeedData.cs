namespace StockReplenishment.App.Database;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (db.Locations.Any()) return;

        var worker1 = new AppUser { Id = Guid.NewGuid(), Name = "Worker 1", Role = UserRole.Worker };
        var worker2 = new AppUser { Id = Guid.NewGuid(), Name = "Worker 2", Role = UserRole.Worker };
        var reviewer1 = new AppUser { Id = Guid.NewGuid(), Name = "Reviewer 1", Role = UserRole.Reviewer };
        var reviewer2 = new AppUser { Id = Guid.NewGuid(), Name = "Reviewer 2", Role = UserRole.Reviewer };
        db.Users.AddRange(worker1, worker2, reviewer1, reviewer2);

        var lineA = new StockLocation { Id = Guid.NewGuid(), Code = "LINE-A", Name = "Assembly Line A" };
        var lineB = new StockLocation { Id = Guid.NewGuid(), Code = "LINE-B", Name = "Assembly Line B" };
        var paint = new StockLocation { Id = Guid.NewGuid(), Code = "PAINT-1", Name = "Paint Shop 1" };
        db.Locations.AddRange(lineA, lineB, paint);

        var draft = NewRequest(worker1, lineA, Priority.Normal, -1,
            ("ART-1001", "M6 Hex Bolt", 500), ("ART-1002", "M6 Washer", 500));

        var submitted = NewRequest(worker2, lineB, Priority.Urgent, -3,
            ("ART-2001", "Bearing 6204", 40));
        submitted.Submit(worker2);
        submitted.ApplyStockCheck(StockCheckStatus.Available,
            submitted.Lines.ToDictionary(l => l.Id, l => l.RequestedQuantity));

        var shortfall = NewRequest(worker1, lineA, Priority.Low, -5,
            ("ART-3001", "Paint Primer 5L", 20), ("ART-3002", "Clear Coat 5L", 10));
        shortfall.Submit(worker1);
        shortfall.ApplyStockCheck(StockCheckStatus.Shortfall,
            shortfall.Lines.ToDictionary(l => l.Id, l => l.RequestedQuantity / 2));

        var approved = NewRequest(worker2, paint, Priority.Normal, -8,
            ("ART-4001", "Masking Tape 50m", 100));
        approved.Submit(worker2);
        approved.ApplyStockCheck(StockCheckStatus.Available,
            approved.Lines.ToDictionary(l => l.Id, l => l.RequestedQuantity));
        approved.Approve(reviewer1);

        var rejected = NewRequest(worker1, lineB, Priority.Low, -10,
            ("ART-5001", "Cable Tie 300mm", 1000));
        rejected.Submit(worker1);
        rejected.ApplyStockCheck(StockCheckStatus.Available,
            rejected.Lines.ToDictionary(l => l.Id, l => l.RequestedQuantity));
        rejected.Reject(reviewer2, "Duplicate of an existing request for LINE-B.");

        var fulfilled = NewRequest(worker2, lineA, Priority.Urgent, -14,
            ("ART-6001", "Weld Wire 1.2mm", 60));
        fulfilled.Submit(worker2);
        fulfilled.ApplyStockCheck(StockCheckStatus.Available,
            fulfilled.Lines.ToDictionary(l => l.Id, l => l.RequestedQuantity));
        fulfilled.Approve(reviewer1);
        fulfilled.Fulfill(reviewer1, fulfilled.Lines.ToDictionary(l => l.Id, l => l.RequestedQuantity));

        db.Requests.AddRange(draft, submitted, shortfall, approved, rejected, fulfilled);
        db.SaveChanges();
    }

    private static ReplenishmentRequest NewRequest(
        AppUser creator, StockLocation location, Priority priority, int ageDays,
        params (string Article, string Description, int Qty)[] lines) => new()
    {
        CreatedById = creator.Id,
        LocationId = location.Id,
        Priority = priority,
        CreatedAt = DateTime.UtcNow.AddDays(ageDays),
        Lines = lines.Select(l => new RequestLineItem
        {
            ArticleNumber = l.Article,
            Description = l.Description,
            RequestedQuantity = l.Qty,
        }).ToList(),
    };
}
