using System.Globalization;
using Microsoft.AspNetCore.Localization;
using THcommunity;
using THcommunity.Configuration;
using THcommunity.Extensions;
using THcommunity.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add local settings (secrets not in git)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add services
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<TeamSettings>(builder.Configuration.GetSection("TeamSettings"));
builder.Services.AddSupabaseClient(builder.Configuration);
builder.Services.AddCloudflareR2(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddPushNotifications(builder.Configuration);

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(builder.Configuration["Frontend:Url"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure localization (Czech default, English available)
var supportedCultures = new[] { new CultureInfo("cs-CZ"), new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("cs-CZ"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // Inject a small script in development that reads the Supabase session from localStorage
    // (key = 'thcommunity-auth') and auto-authorizes the Swagger UI with the Bearer token.
    if (app.Environment.IsDevelopment())
    {
        options.HeadContent = @"<script>
(function(){
  function getTokenFromStorage(){
    try{
      var raw = localStorage.getItem('thcommunity-auth');
      if (!raw) return null;
      var parsed = JSON.parse(raw);
      var access = parsed && (parsed.currentSession?.access_token || parsed.session?.access_token || parsed.access_token || parsed.token);
      if (access) return access;
      // Fallback: scan object for anything that looks like an access_token
      for (var k in parsed){
        try{
          var v = parsed[k];
          if (v && typeof v === 'object'){
            if (v.access_token) return v.access_token;
            if (v.session?.access_token) return v.session.access_token;
            if (v.currentSession?.access_token) return v.currentSession.access_token;
          }
        }catch(e){}
      }
    }catch(e){console.warn('Swagger auto-auth: failed to parse storage', e)}
    return null;
  }

  function setSwaggerAuth(token){
    try{
      if(!token) return;
      var tryOnce = function(){
        if(window.ui && window.ui.authActions){
          window.ui.authActions.authorize({
            Bearer: {
              name: 'Authorization',
              schema: { type: 'http', scheme: 'bearer', bearerFormat: 'JWT' },
              value: 'Bearer ' + token
            }
          });
          console.log('Swagger: auto-authorized from thcommunity-auth');
        } else {
          setTimeout(tryOnce, 300);
        }
      };
      tryOnce();
    }catch(e){console.warn('Swagger auto-auth failed', e)}
  }

  var t = getTokenFromStorage();
  setSwaggerAuth(t);
})();
</script>";
    }
});

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapAuthEndpoints();
app.MapTeamEndpoints();
app.MapEventEndpoints();
app.MapMessageEndpoints();
app.MapSurveyEndpoints();
app.MapUserEndpoints();
app.MapPushEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
