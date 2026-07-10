namespace StockReplenishment.App.Frontend;

public class UserHeaderHandler(UserState state) : DelegatingHandler(new HttpClientHandler())
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (state.CurrentUser is { } user)
            request.Headers.Add("X-User-Id", user.Id.ToString());
        return base.SendAsync(request, cancellationToken);
    }
}
