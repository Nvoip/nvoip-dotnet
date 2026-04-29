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

    public NvoipClient(
        string? baseUrl = null,
        string? oauthClientId = null,
        string? oauthClientSecret = null,
        HttpClient? httpClient = null)
    {
        _baseUrl = (baseUrl ?? "https://api.nvoip.com.br/v2").TrimEnd('/');
        _oauthClientId = oauthClientId;
        _oauthClientSecret = oauthClientSecret;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public static string EncodeBasicAuth(string clientId, string clientSecret)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
    }

    public Task<string> CreateAccessTokenAsync(string numbersip, string userToken, CancellationToken cancellationToken = default)
    {
        var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = numbersip,
            ["password"] = userToken,
            ["grant_type"] = "password",
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/oauth/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", ResolveBasicAuth());
        request.Content = payload;
        return SendAsync(request, cancellationToken);
    }

    public Task<string> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/oauth/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", ResolveBasicAuth());
        request.Content = payload;
        return SendAsync(request, cancellationToken);
    }

    public Task<string> GetBalanceAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/balance");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return SendAsync(request, cancellationToken);
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

    public Task<string> CheckOtpAsync(string code, string key, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/check/otp?code={Uri.EscapeDataString(code)}&key={Uri.EscapeDataString(key)}");
        return SendAsync(request, cancellationToken);
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

    private Task<string> PostJsonAsync(string url, string accessToken, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return SendAsync(request, cancellationToken);
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
