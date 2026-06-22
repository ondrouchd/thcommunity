namespace THcommunity.Configuration;

public class AppSettings
{
    public string ApplicationName { get; set; } = "THcommunity";
    public string DefaultLanguage { get; set; } = "cs-CZ";
    public SupabaseSettings Supabase { get; set; } = new();
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
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string JwtSecret { get; set; } = string.Empty;
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
