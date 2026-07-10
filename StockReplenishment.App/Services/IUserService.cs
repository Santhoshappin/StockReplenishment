namespace StockReplenishment.App.Services;

public interface IUserService
{
    Task<List<AppUser>> ListAsync();
}
