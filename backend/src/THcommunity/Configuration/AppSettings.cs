namespace THcommunity.Configuration;

public class AppSettings
{
    public string ApplicationName { get; set; } = "THcommunity";
    public string DefaultLanguage { get; set; } = "cs-CZ";
    public SupabaseSettings Supabase { get; set; } = new();
    public AuthSettings Auth { get; set; } = new();
    public StorageSettings Storage { get; set; } = new();
    public CloudflareSettings Cloudflare { get; set; } = new();
    public WebPushSettings WebPush { get; set; } = new();
    public RegistrationSettings Registration { get; set; } = new();
}

public class RegistrationSettings
{
    /// <summary>
    /// Optional invite code of a team that newly registered users are automatically
    /// added to. When empty, users register without a team and join or create one later.
    /// </summary>
    public string? DefaultTeamInviteCode { get; set; }
}

public class SupabaseSettings
{
    public string Url { get; set; } = string.Empty;
    // Optional URL reachable from the backend itself (e.g. a Docker service name).
    // When set, REST and JWKS calls use it while JWT issuer validation still uses Url.
    public string InternalUrl { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string JwtSecret { get; set; } = string.Empty;
}

/// <summary>
/// Cloud-agnostic authentication settings. When <see cref="Provider"/> is empty or
/// "supabase" the backend keeps the Supabase behaviour (issuer derived from
/// <see cref="SupabaseSettings.Url"/>, JWKS + optional HS256 secret). For "oidc",
/// "azuread"/"entra" or "cognito" the backend validates tokens from a generic OpenID
/// Connect provider using standard metadata discovery, so it can run on Azure or AWS
/// instead of Supabase.
/// </summary>
public class AuthSettings
{
    /// <summary>"" / "supabase" | "oidc" | "azuread" (alias "entra") | "cognito".</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>OIDC authority, e.g. the token issuer. The middleware discovers
    /// the signing keys from {Authority}/.well-known/openid-configuration.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Explicit OIDC metadata document URL. Overrides <see cref="Authority"/>
    /// based discovery when set (useful for Azure AD B2C / Entra External ID flows).</summary>
    public string MetadataAddress { get; set; } = string.Empty;

    /// <summary>Expected token issuer. Defaults to <see cref="Authority"/> when empty.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Expected audience (e.g. the application/client id).</summary>
    public string Audience { get; set; } = string.Empty;

    public bool ValidateAudience { get; set; } = true;

    /// <summary>Set to false for local/non-HTTPS identity providers.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Claim used as the user identifier. Defaults to "sub".</summary>
    public string NameClaimType { get; set; } = "sub";

    // --- AWS Cognito convenience: issuer/metadata are derived from these. ---
    public string Region { get; set; } = string.Empty;
    public string UserPoolId { get; set; } = string.Empty;

    // --- Azure Entra / AD convenience. ---
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// Object storage settings for media uploads. Works with any S3-compatible service:
/// AWS S3 (set <see cref="Region"/>, leave <see cref="ServiceUrl"/> empty), Cloudflare R2
/// or MinIO (set <see cref="ServiceUrl"/>). When left empty the legacy
/// <see cref="CloudflareSettings"/> values are used for backward compatibility.
/// </summary>
public class StorageSettings
{
    /// <summary>"" | "s3" (AWS native) | "r2" | "s3compatible".</summary>
    public string Provider { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; } = true;
}

public class CloudflareSettings
{
    public string R2AccountId { get; set; } = string.Empty;
    public string R2AccessKeyId { get; set; } = string.Empty;
    public string R2SecretAccessKey { get; set; } = string.Empty;
    public string R2BucketName { get; set; } = string.Empty;
    public string R2PublicUrl { get; set; } = string.Empty;
}

public class WebPushSettings
{
    public string Subject { get; set; } = string.Empty;
    public string VapidPublicKey { get; set; } = string.Empty;
    public string VapidPrivateKey { get; set; } = string.Empty;
}
