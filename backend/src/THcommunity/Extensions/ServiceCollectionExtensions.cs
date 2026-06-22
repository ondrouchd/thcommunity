using Amazon.S3;
using Amazon.Runtime;
using THcommunity.Configuration;
using THcommunity.Services;
using Microsoft.Extensions.Configuration;

namespace THcommunity.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSupabaseClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ISupabaseClient, SupabaseHttpClient>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<ISurveyService, SurveyService>();
        services.AddScoped<IPushNotificationService, PushNotificationService>();

        // Register storage implementation conditionally based on configured object storage
        // (generic S3 settings) or the legacy Cloudflare R2 keys.
        var storageAccessKey = configuration["AppSettings:Storage:AccessKeyId"];
        var legacyAccessKey = configuration["Cloudflare:R2AccessKeyId"];

        if (!string.IsNullOrEmpty(storageAccessKey) || !string.IsNullOrEmpty(legacyAccessKey))
        {
            services.AddScoped<IStorageService, StorageService>();
        }
        else
        {
            services.AddScoped<IStorageService, NoopStorageService>();
        }

        services.AddScoped<IStatisticsService, StatisticsService>();
        
        return services;
    }

    public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var storage = new StorageSettings();
        configuration.GetSection("AppSettings:Storage").Bind(storage);

        // Backward compatibility: fall back to the legacy Cloudflare R2 flat keys when the
        // generic Storage section is not configured.
        if (string.IsNullOrWhiteSpace(storage.AccessKeyId))
        {
            var legacyAccountId = configuration["Cloudflare:R2AccountId"];
            var legacyAccessKey = configuration["Cloudflare:R2AccessKeyId"];
            if (!string.IsNullOrWhiteSpace(legacyAccessKey))
            {
                storage.Provider = string.IsNullOrWhiteSpace(storage.Provider) ? "r2" : storage.Provider;
                storage.AccessKeyId = legacyAccessKey!;
                storage.SecretAccessKey = configuration["Cloudflare:R2SecretAccessKey"] ?? string.Empty;
                storage.BucketName = string.IsNullOrWhiteSpace(storage.BucketName)
                    ? (configuration["Cloudflare:R2BucketName"] ?? string.Empty) : storage.BucketName;
                storage.PublicUrl = string.IsNullOrWhiteSpace(storage.PublicUrl)
                    ? (configuration["Cloudflare:R2PublicUrl"] ?? string.Empty) : storage.PublicUrl;
                if (string.IsNullOrWhiteSpace(storage.ServiceUrl) && !string.IsNullOrWhiteSpace(legacyAccountId))
                {
                    storage.ServiceUrl = $"https://{legacyAccountId}.r2.cloudflarestorage.com";
                }
                storage.ForcePathStyle = true;
            }
        }

        // No credentials -> storage stays disabled (NoopStorageService is registered).
        if (string.IsNullOrWhiteSpace(storage.AccessKeyId))
        {
            return services;
        }

        // Expose the resolved settings (incl. legacy fallback) to StorageService.
        services.PostConfigure<AppSettings>(s => s.Storage = storage);

        var credentials = new BasicAWSCredentials(storage.AccessKeyId, storage.SecretAccessKey);
        var config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(storage.ServiceUrl))
        {
            // Cloudflare R2, MinIO or any other S3-compatible endpoint.
            config.ServiceURL = storage.ServiceUrl;
            config.ForcePathStyle = storage.ForcePathStyle;
        }
        else if (!string.IsNullOrWhiteSpace(storage.Region))
        {
            // AWS S3 native.
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(storage.Region);
        }

        services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, config));

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = new AuthSettings();
        configuration.GetSection("AppSettings:Auth").Bind(auth);
        var provider = (auth.Provider ?? string.Empty).Trim().ToLowerInvariant();
        var useOidcDiscovery = provider is "oidc" or "azuread" or "entra" or "cognito";

        var nameClaimType = string.IsNullOrWhiteSpace(auth.NameClaimType) ? "sub" : auth.NameClaimType;
        var signingKeys = new List<Microsoft.IdentityModel.Tokens.SecurityKey>();

        string issuer;
        string? audience;
        bool validateAudience;
        string? authority = null;
        string? metadataAddress = null;

        if (useOidcDiscovery)
        {
            // Cloud-native identity provider (Azure Entra / AD, AWS Cognito or any OIDC).
            // The JwtBearer middleware discovers signing keys (with rotation) from the
            // provider's OpenID Connect metadata, so no key is fetched manually here.
            if (provider == "cognito" && !string.IsNullOrWhiteSpace(auth.Region) && !string.IsNullOrWhiteSpace(auth.UserPoolId))
            {
                var derived = $"https://cognito-idp.{auth.Region}.amazonaws.com/{auth.UserPoolId}";
                authority = string.IsNullOrWhiteSpace(auth.Authority) ? derived : auth.Authority;
                issuer = string.IsNullOrWhiteSpace(auth.Issuer) ? derived : auth.Issuer;
            }
            else if ((provider == "azuread" || provider == "entra")
                     && !string.IsNullOrWhiteSpace(auth.TenantId)
                     && string.IsNullOrWhiteSpace(auth.Authority)
                     && string.IsNullOrWhiteSpace(auth.MetadataAddress))
            {
                authority = $"https://login.microsoftonline.com/{auth.TenantId}/v2.0";
                issuer = string.IsNullOrWhiteSpace(auth.Issuer) ? authority : auth.Issuer;
            }
            else
            {
                authority = string.IsNullOrWhiteSpace(auth.Authority) ? null : auth.Authority;
                issuer = string.IsNullOrWhiteSpace(auth.Issuer) ? (authority ?? string.Empty) : auth.Issuer;
            }

            metadataAddress = string.IsNullOrWhiteSpace(auth.MetadataAddress) ? null : auth.MetadataAddress;
            audience = string.IsNullOrWhiteSpace(auth.Audience) ? null : auth.Audience;
            validateAudience = auth.ValidateAudience && audience != null;

            Console.WriteLine($"JWT Auth: provider={provider}, authority={authority}, issuer={issuer}, audience={audience}");
        }
        else
        {
            // Supabase (default) – issuer derived from the Supabase URL, signing keys from
            // JWKS plus an optional symmetric HS256 secret (self-hosted GoTrue / local dev).
            var supabaseUrl = configuration["AppSettings:Supabase:Url"]
                ?? throw new InvalidOperationException("AppSettings:Supabase:Url is required for the Supabase auth provider");
            var internalUrl = configuration["AppSettings:Supabase:InternalUrl"];
            var jwtSecret = configuration["AppSettings:Supabase:JwtSecret"];

            // Use the internal URL (e.g. a Docker service name) for reaching the auth server
            // when configured, while issuer validation below still uses the public URL.
            var authBaseUrl = !string.IsNullOrWhiteSpace(internalUrl) ? internalUrl : supabaseUrl;

            issuer = $"{supabaseUrl}/auth/v1";
            audience = "authenticated";
            validateAudience = true;

            Console.WriteLine($"JWT Auth: Supabase URL = {supabaseUrl}");
            Console.WriteLine($"JWT Auth: Using JWKS endpoint for key validation");

            var jwksUrl = $"{authBaseUrl}/auth/v1/.well-known/jwks.json";
            Microsoft.IdentityModel.Tokens.JsonWebKeySet? jwks = null;

            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                var jwksJson = httpClient.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
                jwks = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(jwksJson);
                Console.WriteLine($"JWT Auth: Loaded {jwks.Keys.Count} signing key(s) from JWKS");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JWT Auth: Failed to load JWKS: {ex.Message}");
            }

            if (jwks?.Keys != null)
            {
                signingKeys.AddRange(jwks.Keys);
            }
            if (!string.IsNullOrWhiteSpace(jwtSecret))
            {
                signingKeys.Add(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtSecret)));
                Console.WriteLine("JWT Auth: Added symmetric signing key from configured JWT secret");
            }
        }

        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                // Disable default claim mapping to preserve original claim names from the token
                options.MapInboundClaims = false;

                if (useOidcDiscovery)
                {
                    if (!string.IsNullOrWhiteSpace(authority))
                        options.Authority = authority;
                    if (!string.IsNullOrWhiteSpace(metadataAddress))
                        options.MetadataAddress = metadataAddress;
                    options.RequireHttpsMetadata = auth.RequireHttpsMetadata;
                    if (!string.IsNullOrWhiteSpace(audience))
                        options.Audience = audience;
                }

                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = validateAudience,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = nameClaimType
                };

                if (!useOidcDiscovery)
                {
                    // Supabase path supplies its keys directly; OIDC discovery fills them itself.
                    options.TokenValidationParameters.IssuerSigningKeys = signingKeys;
                }

                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Auth failed: {context.Exception.Message}");
                        return System.Threading.Tasks.Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var sub = context.Principal?.FindFirst("sub")?.Value;
                        Console.WriteLine($"Token validated for: {sub}");
                        Console.WriteLine("All claims:");
                        foreach (var claim in context.Principal?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
                        {
                            Console.WriteLine($"  {claim.Type}: {claim.Value}");
                        }
                        return System.Threading.Tasks.Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"Auth challenge: {context.Error}, {context.ErrorDescription}");
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        
        return services;
    }

    public static IServiceCollection AddPushNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        // VAPID credentials live under the "Vapid" configuration section; project them onto
        // AppSettings.WebPush so PushNotificationService is constructed with valid keys.
        services.PostConfigure<AppSettings>(settings =>
        {
            var vapid = configuration.GetSection("Vapid");
            var subject = vapid["Subject"];
            var publicKey = vapid["PublicKey"];
            var privateKey = vapid["PrivateKey"];

            if (!string.IsNullOrWhiteSpace(subject))
                settings.WebPush.Subject = subject;
            if (!string.IsNullOrWhiteSpace(publicKey))
                settings.WebPush.VapidPublicKey = publicKey;
            if (!string.IsNullOrWhiteSpace(privateKey))
                settings.WebPush.VapidPrivateKey = privateKey;
        });

        return services;
    }
}
