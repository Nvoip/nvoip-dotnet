using System.Text.Json;
using Nvoip;

if (args.Length == 0)
{
    throw new InvalidOperationException("Use one of: auth-token, balance, send-sms, create-call, send-otp, check-otp, wa-list, wa-send");
}

var client = new NvoipClient(
    Environment.GetEnvironmentVariable("NVOIP_BASE_URL"),
    Environment.GetEnvironmentVariable("NVOIP_OAUTH_CLIENT_ID"),
    Environment.GetEnvironmentVariable("NVOIP_OAUTH_CLIENT_SECRET"));

switch (args[0])
{
    case "auth-token":
        Console.WriteLine(await client.CreateAccessTokenAsync(Env("NVOIP_NUMBERSIP"), Env("NVOIP_USER_TOKEN")));
        break;
    case "balance":
        Console.WriteLine(await client.GetBalanceAsync(await AccessTokenOrCreateAsync(client)));
        break;
    case "send-sms":
        Console.WriteLine(await client.SendSmsAsync(
            await AccessTokenOrCreateAsync(client),
            FirstNonEmpty(Environment.GetEnvironmentVariable("NVOIP_TARGET_NUMBER"), "11999999999"),
            FirstNonEmpty(Environment.GetEnvironmentVariable("NVOIP_SMS_MESSAGE"), "Mensagem de teste Nvoip")));
        break;
    case "create-call":
        Console.WriteLine(await client.CreateCallAsync(
            await AccessTokenOrCreateAsync(client),
            Env("NVOIP_CALLER"),
            FirstNonEmpty(Environment.GetEnvironmentVariable("NVOIP_TARGET_NUMBER"), "11999999999")));
        break;
    case "send-otp":
        Console.WriteLine(await client.SendOtpAsync(
            await AccessTokenOrCreateAsync(client),
            new
            {
                sms = FirstNonEmpty(Environment.GetEnvironmentVariable("NVOIP_OTP_SMS"), Environment.GetEnvironmentVariable("NVOIP_TARGET_NUMBER")),
                voice = Environment.GetEnvironmentVariable("NVOIP_OTP_VOICE"),
                email = Environment.GetEnvironmentVariable("NVOIP_OTP_EMAIL"),
            }));
        break;
    case "check-otp":
        Console.WriteLine(await client.CheckOtpAsync(Env("NVOIP_OTP_CODE"), Env("NVOIP_OTP_KEY")));
        break;
    case "wa-list":
        Console.WriteLine(await client.ListWhatsAppTemplatesAsync(await AccessTokenOrCreateAsync(client)));
        break;
    case "wa-send":
        Console.WriteLine(await client.SendWhatsAppTemplateAsync(
            await AccessTokenOrCreateAsync(client),
            new
            {
                idTemplate = Env("NVOIP_WA_TEMPLATE_ID"),
                destination = Env("NVOIP_WA_DESTINATION"),
                instance = Env("NVOIP_WA_INSTANCE"),
                language = FirstNonEmpty(Environment.GetEnvironmentVariable("NVOIP_WA_LANGUAGE"), "pt_BR"),
                bodyVariables = ParseArray(Environment.GetEnvironmentVariable("NVOIP_WA_BODY_VARIABLES")),
                headerVariables = ParseArray(Environment.GetEnvironmentVariable("NVOIP_WA_HEADER_VARIABLES")),
                functions = new
                {
                    to_flow = ParseBool(Environment.GetEnvironmentVariable("NVOIP_WA_TO_FLOW"), false)
                }
            }));
        break;
    default:
        throw new InvalidOperationException("Unknown command: " + args[0]);
}

static string Env(string name)
{
    return Environment.GetEnvironmentVariable(name) switch
    {
        null or "" => throw new InvalidOperationException($"Missing required environment variable: {name}"),
        var value => value,
    };
}

static string FirstNonEmpty(string? value, string? fallback)
{
    return string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value;
}

static bool ParseBool(string? value, bool fallback)
{
    return string.IsNullOrWhiteSpace(value)
        ? fallback
        : value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
}

static object[] ParseArray(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return Array.Empty<object>();
    }

    return JsonSerializer.Deserialize<object[]>(raw) ?? Array.Empty<object>();
}

static async Task<string> AccessTokenOrCreateAsync(NvoipClient client)
{
    var accessToken = Environment.GetEnvironmentVariable("NVOIP_ACCESS_TOKEN");
    if (!string.IsNullOrWhiteSpace(accessToken))
    {
        return accessToken;
    }

    var response = await client.CreateAccessTokenAsync(Env("NVOIP_NUMBERSIP"), Env("NVOIP_USER_TOKEN"));
    using var document = JsonDocument.Parse(response);
    return document.RootElement.GetProperty("access_token").GetString()
        ?? throw new InvalidOperationException("access_token not found in OAuth response");
}
