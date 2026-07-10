namespace StockReplenishment.App.Services;

public interface IRequestService
{
    Task<PagedResult<RequestDto>> ListAsync(
        RequestStatus? status, Priority? priority, Guid? locationId, int page, int pageSize);
    Task<RequestDto?> GetAsync(Guid id);
    Task<RequestDto> CreateAsync(CreateRequestDto dto, Guid actorId);
    Task<RequestDto> SubmitAsync(Guid id, Guid actorId);
    Task<RequestDto> ApproveAsync(Guid id, Guid actorId);
    Task<RequestDto> RejectAsync(Guid id, Guid actorId, string? reason);
    Task<RequestDto> FulfillAsync(Guid id, Guid actorId, IReadOnlyDictionary<Guid, int> fulfilledByLine);
}
