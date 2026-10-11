using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XBLauncher.Core.Auth;

/// <summary>
/// Result of a successful Microsoft + Minecraft Services sign-in. Contains only
/// the data needed to launch; refresh tokens are persisted by the session store.
/// </summary>
public sealed class MinecraftSession
{
    public required string PlayerName { get; init; }
    public required string Uuid { get; init; }
    public required string AccessToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public bool OwnsJavaEdition { get; init; }
}

/// <summary>
/// Payload returned by the Xbox/Minecraft ownership verification step.
/// </summary>
public sealed class OwnershipResult
{
    public bool OwnsJavaEdition { get; init; }
    public string? Detail { get; init; }
}

// ---- JSON DTOs for Microsoft / Minecraft Services ----

public sealed class DeviceCodeResponse
{
    [JsonPropertyName("device_code")] public string DeviceCode { get; set; } = "";
    [JsonPropertyName("user_code")] public string UserCode { get; set; } = "";
    [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = "";
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("interval")] public int Interval { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

public sealed class TokenResponse
{
    [JsonPropertyName("token_type")] public string TokenType { get; set; } = "";
    [JsonPropertyName("scope")] public string? Scope { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("id_token")] public string? IdToken { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
}

public sealed class XboxAuthResponse
{
    [JsonPropertyName("Token")] public string Token { get; set; } = "";
    [JsonPropertyName("DisplayClaims")] public DisplayClaims? DisplayClaims { get; set; }
}

public sealed class DisplayClaims
{
    [JsonPropertyName("xui")] public List<Xui> Xui { get; set; } = new();
}

public sealed class Xui
{
    [JsonPropertyName("uhs")] public string Uhs { get; set; } = "";
}

public sealed class XstsResponse
{
    [JsonPropertyName("Token")] public string Token { get; set; } = "";
    [JsonPropertyName("DisplayClaims")] public DisplayClaims? DisplayClaims { get; set; }
}

public sealed class MinecraftLoginResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
}

public sealed class MinecraftProfile
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public sealed class GameOwnershipResponse
{
    [JsonPropertyName("items")] public List<OwnershipItem> Items { get; set; } = new();
}

public sealed class OwnershipItem
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("signature")] public string? Signature { get; set; }
}

