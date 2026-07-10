namespace StockReplenishment.App.BackendApi.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController(ILocationService service) : ControllerBase
{
    [HttpGet]
    public Task<List<StockLocation>> List() => service.ListAsync();

    [HttpPost]
    public async Task<ActionResult<StockLocation>> Create(
        [FromHeader(Name = "X-User-Id")] Guid? userId, UpsertLocationDto dto)
    {
        var location = await service.CreateAsync(dto, RequireUser(userId));
        return Created($"/api/locations/{location.Id}", location);
    }

    [HttpPut("{id:guid}")]
    public Task<StockLocation> Update(
        Guid id, [FromHeader(Name = "X-User-Id")] Guid? userId, UpsertLocationDto dto) =>
        service.UpdateAsync(id, dto, RequireUser(userId));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromHeader(Name = "X-User-Id")] Guid? userId)
    {
        await service.DeleteAsync(id, RequireUser(userId));
        return NoContent();
    }

    private static Guid RequireUser(Guid? userId) =>
        userId ?? throw new ArgumentException("The X-User-Id header (acting user) is required for this action.");
}
