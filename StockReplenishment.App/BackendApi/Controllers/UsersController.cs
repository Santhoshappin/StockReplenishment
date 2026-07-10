namespace StockReplenishment.App.BackendApi.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService service) : ControllerBase
{
    [HttpGet]
    public Task<List<AppUser>> List() => service.ListAsync();
}
