namespace StockReplenishment.Tests;

public class LifecycleTests
{
    private static readonly AppUser Creator = new() { Id = Guid.NewGuid(), Name = "Worker 1", Role = UserRole.Worker };
    private static readonly AppUser OtherWorker = new() { Id = Guid.NewGuid(), Name = "Worker 2", Role = UserRole.Worker };
    private static readonly AppUser Reviewer = new() { Id = Guid.NewGuid(), Name = "Reviewer 1", Role = UserRole.Reviewer };

    private static ReplenishmentRequest NewDraft(AppUser? creator = null) => new()
    {
        LocationId = Guid.NewGuid(),
        CreatedById = (creator ?? Creator).Id,
        Lines = [new RequestLineItem { ArticleNumber = "A-1", Description = "Bolt", RequestedQuantity = 10 }],
    };

    private static ReplenishmentRequest NewSubmitted()
    {
        var request = NewDraft();
        request.Submit(Creator);
        return request;
    }

    [Test]
    public void Submit_ByCreator_SetsSubmittedAndPendingStockCheck()
    {
        var request = NewDraft();

        request.Submit(Creator);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(RequestStatus.Submitted));
            Assert.That(request.StockCheck, Is.EqualTo(StockCheckStatus.Pending));
        });
    }

    [Test]
    public void Submit_ByAnotherUser_IsForbidden()
    {
        var request = NewDraft();

        Assert.Throws<ForbiddenException>(() => request.Submit(OtherWorker));
        Assert.That(request.Status, Is.EqualTo(RequestStatus.Draft));
    }

    [Test]
    public void Submit_WithoutLines_Fails()
    {
        var request = new ReplenishmentRequest { LocationId = Guid.NewGuid(), CreatedById = Creator.Id };

        Assert.Throws<ArgumentException>(() => request.Submit(Creator));
    }

    [Test]
    public void Submit_WhenAlreadySubmitted_Fails() =>
        Assert.Throws<InvalidOperationException>(() => NewSubmitted().Submit(Creator));

    [Test]
    public void Approve_ByReviewer_FromSubmitted_Approves()
    {
        var request = NewSubmitted();

        request.Approve(Reviewer);

        Assert.That(request.Status, Is.EqualTo(RequestStatus.Approved));
    }

    [Test]
    public void Approve_ByWorker_IsForbidden() =>
        Assert.Throws<ForbiddenException>(() => NewSubmitted().Approve(OtherWorker));

    [Test]
    public void Approve_ByCreator_IsForbidden_EvenIfReviewer()
    {
        var reviewerCreator = new AppUser { Id = Guid.NewGuid(), Name = "Self", Role = UserRole.Reviewer };
        var request = NewDraft(reviewerCreator);
        request.Submit(reviewerCreator);

        Assert.Throws<ForbiddenException>(() => request.Approve(reviewerCreator));
    }

    [Test]
    public void Approve_FromDraft_Fails() =>
        Assert.Throws<InvalidOperationException>(() => NewDraft().Approve(Reviewer));

    [Test]
    public void Reject_WithoutReason_Fails()
    {
        var request = NewSubmitted();

        Assert.Throws<ArgumentException>(() => request.Reject(Reviewer, "  "));
        Assert.That(request.Status, Is.EqualTo(RequestStatus.Submitted));
    }

    [Test]
    public void Reject_ByReviewer_SetsRejectedWithReason()
    {
        var request = NewSubmitted();

        request.Reject(Reviewer, "Not needed");

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(RequestStatus.Rejected));
            Assert.That(request.RejectionReason, Is.EqualTo("Not needed"));
        });
    }

    [Test]
    public void Reject_ByWorker_IsForbidden() =>
        Assert.Throws<ForbiddenException>(() => NewSubmitted().Reject(OtherWorker, "reason"));

    [Test]
    public void Fulfill_ByReviewer_FromApproved_SetsQuantitiesAndFulfilled()
    {
        var request = NewSubmitted();
        request.Approve(Reviewer);

        request.Fulfill(Reviewer, request.Lines.ToDictionary(l => l.Id, l => 7));

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(RequestStatus.Fulfilled));
            Assert.That(request.Lines[0].FulfilledQuantity, Is.EqualTo(7));
        });
    }

    [Test]
    public void Fulfill_FromSubmitted_Fails()
    {
        var request = NewSubmitted();

        Assert.Throws<InvalidOperationException>(() =>
            request.Fulfill(Reviewer, request.Lines.ToDictionary(l => l.Id, l => 1)));
    }

    [Test]
    public void Fulfill_ByWorker_IsForbidden()
    {
        var request = NewSubmitted();
        request.Approve(Reviewer);

        Assert.Throws<ForbiddenException>(() =>
            request.Fulfill(OtherWorker, request.Lines.ToDictionary(l => l.Id, l => 1)));
    }

    [Test]
    public void Fulfill_MissingLineQuantity_Fails()
    {
        var request = NewSubmitted();
        request.Approve(Reviewer);

        Assert.Throws<ArgumentException>(() => request.Fulfill(Reviewer, new Dictionary<Guid, int>()));
    }

    [Test]
    public void Fulfill_QuantityAboveRequested_Fails()
    {
        var request = NewSubmitted();
        request.Approve(Reviewer);

        Assert.Throws<ArgumentException>(() =>
            request.Fulfill(Reviewer, request.Lines.ToDictionary(l => l.Id, l => l.RequestedQuantity + 1)));
    }

    [Test]
    public void Fulfill_UnknownLineItem_Fails()
    {
        var request = NewSubmitted();
        request.Approve(Reviewer);
        var payload = request.Lines.ToDictionary(l => l.Id, l => 1);
        payload[Guid.NewGuid()] = 1;

        Assert.Throws<ArgumentException>(() => request.Fulfill(Reviewer, payload));
    }

    [Test]
    public void ApplyStockCheck_WhenPending_SetsResultAndAvailability()
    {
        var request = NewSubmitted();
        var lineId = request.Lines[0].Id;

        request.ApplyStockCheck(StockCheckStatus.Shortfall, new Dictionary<Guid, int> { [lineId] = 4 });

        Assert.Multiple(() =>
        {
            Assert.That(request.StockCheck, Is.EqualTo(StockCheckStatus.Shortfall));
            Assert.That(request.Lines[0].AvailableQuantity, Is.EqualTo(4));
        });
    }
}
