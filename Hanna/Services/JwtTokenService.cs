using Hanna.Core;
using System.Security.Cryptography;

namespace Hanna.Services;

internal sealed class JwtTokenService
{
    private readonly AppConfig config;

    public JwtTokenService(AppConfig config)
    {
        this.config = config;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(config.JwtSecret) && config.JwtSecret.Length >= 24;

    public string CreateMobileToken(long chatId, string role, string displayName)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("HANNA_JWT_SECRET no está configurado o es demasiado corto.");

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long exp = DateTimeOffset.UtcNow.AddMinutes(Math.Max(config.JwtExpireMinutes, 5)).ToUnixTimeSeconds();

        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object>
        {
            ["iss"] = config.JwtIssuer,
            ["aud"] = config.JwtAudience,
            ["sub"] = chatId.ToString(CultureInfo.InvariantCulture),
            ["chatId"] = chatId,
            ["role"] = role,
            ["name"] = displayName,
            ["iat"] = now,
            ["exp"] = exp
        };

        string header64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
        string payload64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        string unsigned = header64 + "." + payload64;
        string signature = Sign(unsigned);

        return unsigned + "." + signature;
    }

    public JwtValidationResult Validate(string? token)
    {
        if (!IsConfigured)
            return JwtValidationResult.Fail("HANNA_JWT_SECRET no configurado.");

        if (string.IsNullOrWhiteSpace(token))
            return JwtValidationResult.Fail("Token vacío.");

        token = token.Trim();
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token[7..].Trim();

        string[] parts = token.Split('.');
        if (parts.Length != 3)
            return JwtValidationResult.Fail("Formato JWT inválido.");

        string unsigned = parts[0] + "." + parts[1];
        string expected = Sign(unsigned);
        if (!FixedTimeEquals(expected, parts[2]))
            return JwtValidationResult.Fail("Firma JWT inválida.");

        try
        {
            string payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using JsonDocument doc = JsonDocument.Parse(payloadJson);
            JsonElement root = doc.RootElement;

            string issuer = root.TryGetProperty("iss", out JsonElement issEl) ? issEl.GetString() ?? "" : "";
            string audience = root.TryGetProperty("aud", out JsonElement audEl) ? audEl.GetString() ?? "" : "";
            if (!issuer.Equals(config.JwtIssuer, StringComparison.Ordinal) || !audience.Equals(config.JwtAudience, StringComparison.Ordinal))
                return JwtValidationResult.Fail("Issuer o audience inválidos.");

            long exp = root.TryGetProperty("exp", out JsonElement expEl) ? expEl.GetInt64() : 0;
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= exp)
                return JwtValidationResult.Fail("Token expirado.");

            long chatId = 0;
            if (root.TryGetProperty("chatId", out JsonElement chatEl))
            {
                if (chatEl.ValueKind == JsonValueKind.Number) chatId = chatEl.GetInt64();
                else long.TryParse(chatEl.GetString(), out chatId);
            }
            if (chatId <= 0 && root.TryGetProperty("sub", out JsonElement subEl))
                long.TryParse(subEl.GetString(), out chatId);

            string role = root.TryGetProperty("role", out JsonElement roleEl) ? roleEl.GetString() ?? "usuario" : "usuario";
            string name = root.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() ?? "" : "";

            if (chatId <= 0)
                return JwtValidationResult.Fail("Token sin chatId válido.");

            return JwtValidationResult.Ok(chatId, role, name, DateTimeOffset.FromUnixTimeSeconds(exp));
        }
        catch (Exception ex)
        {
            return JwtValidationResult.Fail("JWT inválido: " + ex.Message);
        }
    }

    private string Sign(string value)
    {
        byte[] key = Encoding.UTF8.GetBytes(config.JwtSecret);
        byte[] data = Encoding.UTF8.GetBytes(value);
        using var hmac = new HMACSHA256(key);
        return Base64UrlEncode(hmac.ComputeHash(data));
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        byte[] ba = Encoding.UTF8.GetBytes(a);
        byte[] bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string s = value.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}

internal sealed record JwtValidationResult(bool IsValid, long ChatId, string Role, string DisplayName, DateTimeOffset? ExpiresAt, string Error)
{
    public static JwtValidationResult Ok(long chatId, string role, string displayName, DateTimeOffset expiresAt) => new(true, chatId, role, displayName, expiresAt, "");
    public static JwtValidationResult Fail(string error) => new(false, 0, "", "", null, error);
}
