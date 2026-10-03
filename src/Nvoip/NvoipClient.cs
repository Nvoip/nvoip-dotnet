using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nvoip;

public sealed class NvoipClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string? _oauthClientId;
    private readonly string? _oauthClientSecret;
    private readonly string _tokenUrl;

    public NvoipClient(
        string? baseUrl = null,
        string? oauthClientId = null,
        string? oauthClientSecret = null,
        HttpClient? httpClient = null,
        string? tokenUrl = null)
    {
        _baseUrl = (baseUrl ?? "https://api.nvoip.com.br/v3").TrimEnd('/');
        _oauthClientId = oauthClientId;
        _oauthClientSecret = oauthClientSecret;
        _tokenUrl = tokenUrl ?? "https://api.nvoip.com.br/auth/oauth2/token";
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public static string EncodeBasicAuth(string clientId, string clientSecret)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(clientId)}:{Uri.EscapeDataString(clientSecret)}"));
    }

    public async Task<string> CreateAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", ResolveBasicAuth());
        request.Content = payload;
        return await SendAsync(request, cancellationToken);
    }

    public async Task<string> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", ResolveBasicAuth());
        request.Content = payload;
        return await SendAsync(request, cancellationToken);
    }

    public async Task<string> GetBalanceAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/balance");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await SendAsync(request, cancellationToken);
    }

    public Task<string> SendSmsAsync(string accessToken, string numberPhone, string message, CancellationToken cancellationToken = default)
    {
        return PostJsonAsync(
            $"{_baseUrl}/sms",
            accessToken,
            new
            {
                numberPhone,
                message,
                flashSms = false,
            },
            cancellationToken);
    }

    public Task<string> CreateCallAsync(string accessToken, string caller, string called, CancellationToken cancellationToken = default)
    {
        return PostJsonAsync(
            $"{_baseUrl}/calls/",
            accessToken,
            new
            {
                caller,
                called,
            },
            cancellationToken);
    }

    public Task<string> SendOtpAsync(string accessToken, object payload, CancellationToken cancellationToken = default)
    {
        return PostJsonAsync($"{_baseUrl}/otp", accessToken, payload, cancellationToken);
    }

    public async Task<string> CheckOtpAsync(string accessToken, string code, string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/check/otp?code={Uri.EscapeDataString(code)}&key={Uri.EscapeDataString(key)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await SendAsync(request, cancellationToken);
    }

    public Task<string> ListWhatsAppTemplatesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/wa/listTemplates");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return SendAsync(request, cancellationToken);
    }

    public Task<string> SendWhatsAppTemplateAsync(string accessToken, object payload, CancellationToken cancellationToken = default)
    {
        return PostJsonAsync($"{_baseUrl}/wa/sendTemplates", accessToken, payload, cancellationToken);
    }

    private string ResolveBasicAuth()
    {
        if (!string.IsNullOrWhiteSpace(_oauthClientId) && !string.IsNullOrWhiteSpace(_oauthClientSecret))
        {
            return EncodeBasicAuth(_oauthClientId, _oauthClientSecret);
        }

        throw new InvalidOperationException("Missing OAuth client credentials. Configure oauthClientId + oauthClientSecret.");
    }

    private async Task<string> PostJsonAsync(string url, string accessToken, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return await SendAsync(request, cancellationToken);
    }

    private async Task<string> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if ((int)response.StatusCode >= 400)
        {
            throw new HttpRequestException($"Nvoip request failed with status {(int)response.StatusCode}: {payload}");
        }
        return payload;
    }
}
