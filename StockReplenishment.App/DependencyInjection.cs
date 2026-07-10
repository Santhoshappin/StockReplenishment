using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using StockReplenishment.App.Frontend;

namespace StockReplenishment.App;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services) =>
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("StockReplenishment"));

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<IStockCheckQueue, StockCheckQueue>();
        services.AddSingleton<IStockAvailabilityService, FakeStockAvailabilityService>();
        services.AddHostedService<StockCheckWorker>();
        return services;
    }

    public static IServiceCollection AddBackendApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    public static IServiceCollection AddFrontend(this IServiceCollection services)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddMudServices();
        services.AddScoped<UserState>();
        services.AddScoped(sp => new HttpClient(new UserHeaderHandler(sp.GetRequiredService<UserState>()))
        {
            BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri),
        });
        return services;
    }
}
