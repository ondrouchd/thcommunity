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

        // Register storage implementation conditionally based on Cloudflare R2 configuration
        var accountId = configuration["Cloudflare:R2AccountId"];
        var accessKeyId = configuration["Cloudflare:R2AccessKeyId"];

        if (!string.IsNullOrEmpty(accountId) && !string.IsNullOrEmpty(accessKeyId))
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

    public static IServiceCollection AddCloudflareR2(this IServiceCollection services, IConfiguration configuration)
    {
        var accountId = configuration["Cloudflare:R2AccountId"];
        var accessKeyId = configuration["Cloudflare:R2AccessKeyId"];
        var secretAccessKey = configuration["Cloudflare:R2SecretAccessKey"];

        if (!string.IsNullOrEmpty(accountId) && !string.IsNullOrEmpty(accessKeyId))
        {
            var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            };
            
            services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, config));
        }
        
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var supabaseUrl = configuration["AppSettings:Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is required");
        
        Console.WriteLine($"JWT Auth: Supabase URL = {supabaseUrl}");
        Console.WriteLine($"JWT Auth: Using JWKS endpoint for key validation");
        
        // Fetch JWKS synchronously at startup
        var jwksUrl = $"{supabaseUrl}/auth/v1/.well-known/jwks.json";
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
        
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                // Disable default claim mapping to preserve original claim names from Supabase JWT
                options.MapInboundClaims = false;
                
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{supabaseUrl}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = jwks?.Keys,
                    NameClaimType = "sub"
                };
                
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
        return services;
    }
}
