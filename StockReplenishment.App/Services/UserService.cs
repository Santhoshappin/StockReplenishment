namespace StockReplenishment.App.Services;

public class UserService(AppDbContext db) : IUserService
{
    public Task<List<AppUser>> ListAsync() =>
        db.Users.OrderBy(u => u.Role).ThenBy(u => u.Name).ToListAsync();
}
