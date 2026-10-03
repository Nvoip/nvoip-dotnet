using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nvoip;
using Xunit;

public sealed class NvoipClientTests
{
    [Fact]
    public async Task UsesOAuthFormAndBearerTransport()
    {
        var handler = new CaptureHandler();
        var client = new NvoipClient("https://local/v3", "id +", "secret:/", new HttpClient(handler), "https://local/auth/oauth2/token");
        await client.CreateAccessTokenAsync();
        await client.GetBalanceAsync("token");
        await client.CheckOtpAsync("token", "a b", "key/1");
        Assert.Equal("https://local/auth/oauth2/token", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("grant_type=client_credentials", handler.Bodies[0]);
        Assert.Equal("Basic", handler.Requests[0].Headers.Authorization!.Scheme);
        Assert.Equal("Bearer", handler.Requests[1].Headers.Authorization!.Scheme);
        Assert.Equal("Bearer", handler.Requests[2].Headers.Authorization!.Scheme);
    }
    [Fact]
    public async Task RaisesForUnauthorized()
    {
        var client = new NvoipClient("https://local/v3", "id", "secret", new HttpClient(new CaptureHandler(HttpStatusCode.Unauthorized)), "https://local/auth/oauth2/token");
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetBalanceAsync("bad"));
    }
    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode status; public List<HttpRequestMessage> Requests { get; } = []; public List<string> Bodies { get; } = [];
        public CaptureHandler(HttpStatusCode status = HttpStatusCode.OK) => this.status = status;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { Requests.Add(request); Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(token)); return new HttpResponseMessage(status) { Content = new StringContent("{}") }; }
    }
}
