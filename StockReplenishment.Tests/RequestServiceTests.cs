using NSubstitute;

namespace StockReplenishment.Tests;

public class RequestServiceTests
{
    private AppDbContext _db = null!;
    private IStockCheckQueue _queue = null!;
    private RequestService _service = null!;
    private AppUser _worker = null!;
    private AppUser _reviewer = null!;
    private StockLocation _location = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _queue = Substitute.For<IStockCheckQueue>();
        _service = new RequestService(_db, _queue);

        _worker = new AppUser { Id = Guid.NewGuid(), Name = "Worker 1", Role = UserRole.Worker };
        _reviewer = new AppUser { Id = Guid.NewGuid(), Name = "Reviewer 1", Role = UserRole.Reviewer };
        _location = new StockLocation { Id = Guid.NewGuid(), Code = "L1", Name = "Line 1" };
        _db.Users.AddRange(_worker, _reviewer);
        _db.Locations.Add(_location);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<ReplenishmentRequest> SeedDraftAsync()
    {
        var request = new ReplenishmentRequest
        {
            LocationId = _location.Id,
            CreatedById = _worker.Id,
            Lines = [new RequestLineItem { ArticleNumber = "A-1", Description = "Bolt", RequestedQuantity = 5 }],
        };
        _db.Requests.Add(request);
        await _db.SaveChangesAsync();
        return request;
    }

    [Test]
    public async Task SubmitAsync_PersistsPendingStatus_AndEnqueuesStockCheck()
    {
        var request = await SeedDraftAsync();

        var dto = await _service.SubmitAsync(request.Id, _worker.Id);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Status, Is.EqualTo(RequestStatus.Submitted));
            Assert.That(dto.StockCheck, Is.EqualTo(StockCheckStatus.Pending));
        });
        await _queue.Received(1).EnqueueAsync(request.Id);
    }

    [Test]
    public async Task SubmitAsync_WhenNotDraft_DoesNotEnqueue()
    {
        var request = await SeedDraftAsync();
        await _service.SubmitAsync(request.Id, _worker.Id);
        _queue.ClearReceivedCalls();

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.SubmitAsync(request.Id, _worker.Id));
        await _queue.DidNotReceive().EnqueueAsync(Arg.Any<Guid>());
    }

    [Test]
    public void SubmitAsync_UnknownId_ThrowsNotFound() =>
        Assert.ThrowsAsync<KeyNotFoundException>(() => _service.SubmitAsync(Guid.NewGuid(), _worker.Id));

    [Test]
    public async Task SubmitAsync_UnknownActor_Throws()
    {
        var request = await SeedDraftAsync();

        Assert.ThrowsAsync<ArgumentException>(() => _service.SubmitAsync(request.Id, Guid.NewGuid()));
    }

    [Test]
    public async Task ApproveAsync_ByCreator_IsForbidden_AndNotPersisted()
    {
        var request = await SeedDraftAsync();
        await _service.SubmitAsync(request.Id, _worker.Id);

        Assert.ThrowsAsync<ForbiddenException>(() => _service.ApproveAsync(request.Id, _worker.Id));
        var stored = await _db.Requests.SingleAsync(r => r.Id == request.Id);
        Assert.That(stored.Status, Is.EqualTo(RequestStatus.Submitted));
    }

    [Test]
    public async Task RejectAsync_ByReviewer_PersistsRejection()
    {
        var request = await SeedDraftAsync();
        await _service.SubmitAsync(request.Id, _worker.Id);

        var dto = await _service.RejectAsync(request.Id, _reviewer.Id, "Out of budget");

        var stored = await _db.Requests.SingleAsync(r => r.Id == request.Id);
        Assert.Multiple(() =>
        {
            Assert.That(dto.Status, Is.EqualTo(RequestStatus.Rejected));
            Assert.That(stored.RejectionReason, Is.EqualTo("Out of budget"));
        });
    }

    [Test]
    public async Task CreateAsync_RecordsCreator()
    {
        var dto = await _service.CreateAsync(
            new CreateRequestDto(_location.Id, Priority.Normal,
                [new CreateLineDto("A-2", "Nut", 3)]), _worker.Id);

        Assert.Multiple(() =>
        {
            Assert.That(dto.CreatedById, Is.EqualTo(_worker.Id));
            Assert.That(dto.Status, Is.EqualTo(RequestStatus.Draft));
        });
    }

    [Test]
    public void CreateAsync_UnknownLocation_Throws() =>
        Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(
            new CreateRequestDto(Guid.NewGuid(), Priority.Low,
                [new CreateLineDto("A-2", "Nut", 1)]), _worker.Id));
}
