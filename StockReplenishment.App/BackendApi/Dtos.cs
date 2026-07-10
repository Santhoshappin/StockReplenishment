using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockReplenishment.App.BackendApi;

public record PagedResult<T>(List<T> Items, int TotalCount);

public record CreateRequestDto(
    [Required] Guid LocationId,
    Priority Priority,
    [MinLength(1, ErrorMessage = "At least one line item is required."), MaxLength(100)] List<CreateLineDto> Lines);

public record CreateLineDto(
    [Required, StringLength(50)] string ArticleNumber,
    [Required, StringLength(200)] string Description,
    [Range(1, 1_000_000)] int RequestedQuantity);

public record RejectDto([Required, StringLength(500)] string Reason);

public record FulfillDto([Required] List<FulfillLineDto> Lines);
public record FulfillLineDto(Guid LineItemId, [Range(0, int.MaxValue)] int FulfilledQuantity);

public record UpsertLocationDto(
    [Required, StringLength(20, MinimumLength = 2)] string Code,
    [Required, StringLength(100, MinimumLength = 2)] string Name);

public record LineDto(
    Guid Id, string ArticleNumber, string Description,
    int RequestedQuantity, int? FulfilledQuantity, int? AvailableQuantity);

public record RequestDto(
    Guid Id, Guid LocationId, string LocationCode, string LocationName,
    Guid CreatedById, string CreatedByName,
    Priority Priority, RequestStatus Status, StockCheckStatus StockCheck,
    string? RejectionReason, DateTime CreatedAt, DateTime UpdatedAt, List<LineDto> Lines)
{
    public static RequestDto From(ReplenishmentRequest r) => new(
        r.Id, r.LocationId, r.Location?.Code ?? "", r.Location?.Name ?? "",
        r.CreatedById, r.CreatedBy?.Name ?? "",
        r.Priority, r.Status, r.StockCheck, r.RejectionReason, r.CreatedAt, r.UpdatedAt,
        r.Lines.Select(l => new LineDto(l.Id, l.ArticleNumber, l.Description,
            l.RequestedQuantity, l.FulfilledQuantity, l.AvailableQuantity)).ToList());
}

public static class AppJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
