using System.Net.Http.Json;

namespace StockReplenishment.App.BackendApi;

public static class ApiError
{
    public static async Task<string> ReadAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            if (problem?.Errors is { Count: > 0 } errors)
                return string.Join(" ", errors.SelectMany(e => e.Value));
            return problem?.Detail ?? problem?.Title ?? $"Request failed ({(int)response.StatusCode}).";
        }
        catch
        {
            return $"Request failed ({(int)response.StatusCode}).";
        }
    }
}
