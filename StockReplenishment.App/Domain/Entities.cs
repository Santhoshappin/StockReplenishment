namespace StockReplenishment.App.Domain;

public enum Priority { Low, Normal, Urgent }
public enum RequestStatus { Draft, Submitted, Approved, Rejected, Fulfilled }
public enum StockCheckStatus { NotChecked, Pending, Available, Shortfall }
public enum UserRole { Worker, Reviewer }

public class AppUser
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public UserRole Role { get; set; }
}

public class StockLocation
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
}

public class RequestLineItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestId { get; set; }
    public required string ArticleNumber { get; set; }
    public required string Description { get; set; }
    public int RequestedQuantity { get; set; }
    public int? FulfilledQuantity { get; set; }
    public int? AvailableQuantity { get; set; }
}

public class ReplenishmentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LocationId { get; set; }
    public StockLocation? Location { get; set; }
    public Guid CreatedById { get; set; }
    public AppUser? CreatedBy { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;
    public RequestStatus Status { get; private set; } = RequestStatus.Draft;
    public StockCheckStatus StockCheck { get; private set; } = StockCheckStatus.NotChecked;
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public List<RequestLineItem> Lines { get; set; } = [];

    public void Submit(AppUser actor)
    {
        if (actor.Id != CreatedById)
            throw new ForbiddenException("Only the person who created the request can submit it.");
        RequireStatus(RequestStatus.Draft, "submit");
        if (Lines.Count == 0)
            throw new ArgumentException("A request must have at least one line item.");
        Status = RequestStatus.Submitted;
        StockCheck = StockCheckStatus.Pending;
        Touch();
    }

    public void Approve(AppUser actor)
    {
        RequireReviewer(actor, "approve");
        RequireStatus(RequestStatus.Submitted, "approve");
        Status = RequestStatus.Approved;
        Touch();
    }

    public void Reject(AppUser actor, string? reason)
    {
        RequireReviewer(actor, "reject");
        RequireStatus(RequestStatus.Submitted, "reject");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection reason is required.");
        Status = RequestStatus.Rejected;
        RejectionReason = reason.Trim();
        Touch();
    }

    public void Fulfill(AppUser actor, IReadOnlyDictionary<Guid, int> fulfilledByLine)
    {
        RequireReviewer(actor, "fulfill");
        RequireStatus(RequestStatus.Approved, "fulfill");

        var lineIds = Lines.Select(l => l.Id).ToHashSet();
        if (fulfilledByLine.Keys.Any(id => !lineIds.Contains(id)))
            throw new ArgumentException("The fulfillment contains a line item that does not belong to this request.");

        foreach (var line in Lines)
        {
            if (!fulfilledByLine.TryGetValue(line.Id, out var quantity))
                throw new ArgumentException($"A fulfilled quantity is required for line '{line.ArticleNumber}'.");
            if (quantity < 0 || quantity > line.RequestedQuantity)
                throw new ArgumentException(
                    $"Fulfilled quantity for line '{line.ArticleNumber}' must be between 0 and the requested {line.RequestedQuantity}.");
            line.FulfilledQuantity = quantity;
        }
        Status = RequestStatus.Fulfilled;
        Touch();
    }

    public void ApplyStockCheck(StockCheckStatus result, IReadOnlyDictionary<Guid, int> availableByLine)
    {
        if (StockCheck != StockCheckStatus.Pending) return;
        StockCheck = result;
        foreach (var line in Lines)
            if (availableByLine.TryGetValue(line.Id, out var quantity))
                line.AvailableQuantity = quantity;
        Touch();
    }

    private void RequireReviewer(AppUser actor, string action)
    {
        if (actor.Role != UserRole.Reviewer)
            throw new ForbiddenException($"Only a reviewer can {action} a request.");
        if (actor.Id == CreatedById)
            throw new ForbiddenException($"You cannot {action} a request you created yourself.");
    }

    private void RequireStatus(RequestStatus expected, string action)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Cannot {action} a request in status '{Status}' (must be '{expected}').");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
