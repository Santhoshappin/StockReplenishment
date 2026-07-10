namespace StockReplenishment.Tests;

public class LocationServiceTests
{
    private AppDbContext _db = null!;
    private LocationService _service = null!;
    private AppUser _worker = null!;
    private AppUser _reviewer = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _service = new LocationService(_db);

        _worker = new AppUser { Id = Guid.NewGuid(), Name = "Worker 1", Role = UserRole.Worker };
        _reviewer = new AppUser { Id = Guid.NewGuid(), Name = "Reviewer 1", Role = UserRole.Reviewer };
        _db.Users.AddRange(_worker, _reviewer);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task CreateAsync_ByReviewer_AddsLocation()
    {
        var location = await _service.CreateAsync(new UpsertLocationDto("LINE-C", "Assembly Line C"), _reviewer.Id);

        Assert.That(await _db.Locations.AnyAsync(l => l.Id == location.Id), Is.True);
    }

    [Test]
    public void CreateAsync_ByWorker_IsForbidden() =>
        Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.CreateAsync(new UpsertLocationDto("LINE-C", "Assembly Line C"), _worker.Id));

    [Test]
    public async Task CreateAsync_DuplicateCode_Conflicts()
    {
        await _service.CreateAsync(new UpsertLocationDto("LINE-C", "Assembly Line C"), _reviewer.Id);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(new UpsertLocationDto("line-c", "Another"), _reviewer.Id));
    }

    [Test]
    public async Task UpdateAsync_ByReviewer_ChangesCodeAndName()
    {
        var location = await _service.CreateAsync(new UpsertLocationDto("LINE-C", "Assembly Line C"), _reviewer.Id);

        var updated = await _service.UpdateAsync(location.Id, new UpsertLocationDto("LINE-D", "Line D"), _reviewer.Id);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Code, Is.EqualTo("LINE-D"));
            Assert.That(updated.Name, Is.EqualTo("Line D"));
        });
    }

    [Test]
    public void UpdateAsync_UnknownId_ThrowsNotFound() =>
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateAsync(Guid.NewGuid(), new UpsertLocationDto("X", "Y"), _reviewer.Id));

    [Test]
    public async Task DeleteAsync_LocationWithRequests_Conflicts()
    {
        var location = await _service.CreateAsync(new UpsertLocationDto("LINE-C", "Assembly Line C"), _reviewer.Id);
        _db.Requests.Add(new ReplenishmentRequest { LocationId = location.Id, CreatedById = _worker.Id });
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(location.Id, _reviewer.Id));
    }

    [Test]
    public async Task DeleteAsync_UnusedLocation_Deletes()
    {
        var location = await _service.CreateAsync(new UpsertLocationDto("LINE-C", "Assembly Line C"), _reviewer.Id);

        await _service.DeleteAsync(location.Id, _reviewer.Id);

        Assert.That(await _db.Locations.AnyAsync(l => l.Id == location.Id), Is.False);
    }
}
