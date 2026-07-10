namespace StockReplenishment.App.Frontend;

public class UserState
{
    public AppUser? CurrentUser { get; private set; }

    public void SetUser(AppUser? user) => CurrentUser = user;
}
