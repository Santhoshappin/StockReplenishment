using StockReplenishment.App;
using StockReplenishment.App.Database;
using StockReplenishment.App.Frontend;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataAccess()
    .AddApplicationServices()
    .AddBackendApi()
    .AddFrontend();

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
app.UseRateLimiter();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
    SeedData.Initialize(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.Run();
