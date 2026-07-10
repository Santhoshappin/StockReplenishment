using System.ComponentModel.DataAnnotations;

namespace StockReplenishment.App.BackendApi.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController(IRequestService service) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<RequestDto>> List(
        [FromQuery] RequestStatus? status,
        [FromQuery] Priority? priority,
        [FromQuery] Guid? locationId,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 10) =>
        await service.ListAsync(status, priority, locationId, page, pageSize);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestDto>> Get(Guid id) =>
        await service.GetAsync(id) is { } dto ? dto : NotFound();

    [HttpPost]
    public async Task<ActionResult<RequestDto>> Create(
        [FromHeader(Name = "X-User-Id")] Guid? userId, CreateRequestDto dto)
    {
        var created = await service.CreateAsync(dto, RequireUser(userId));
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<RequestDto>> Submit(
        Guid id, [FromHeader(Name = "X-User-Id")] Guid? userId) =>
        Accepted($"/api/requests/{id}", await service.SubmitAsync(id, RequireUser(userId)));

    [HttpPost("{id:guid}/approve")]
    public async Task<RequestDto> Approve(Guid id, [FromHeader(Name = "X-User-Id")] Guid? userId) =>
        await service.ApproveAsync(id, RequireUser(userId));

    [HttpPost("{id:guid}/reject")]
    public async Task<RequestDto> Reject(
        Guid id, [FromHeader(Name = "X-User-Id")] Guid? userId, RejectDto dto) =>
        await service.RejectAsync(id, RequireUser(userId), dto.Reason);

    [HttpPost("{id:guid}/fulfill")]
    public async Task<RequestDto> Fulfill(
        Guid id, [FromHeader(Name = "X-User-Id")] Guid? userId, FulfillDto dto) =>
        await service.FulfillAsync(id, RequireUser(userId),
            dto.Lines.ToDictionary(l => l.LineItemId, l => l.FulfilledQuantity));

    private static Guid RequireUser(Guid? userId) =>
        userId ?? throw new ArgumentException("The X-User-Id header (acting user) is required for this action.");
}
