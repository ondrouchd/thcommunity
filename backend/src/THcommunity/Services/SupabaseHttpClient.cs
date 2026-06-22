using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using THcommunity.Configuration;
using Microsoft.Extensions.Options;

namespace THcommunity.Services;

public interface ISupabaseClient
{
    Task<T?> GetSingleAsync<T>(string table, string query) where T : class;
    Task<List<T>> GetListAsync<T>(string table, string query = "") where T : class;
    Task<T> InsertAsync<T>(string table, T data) where T : class;
    Task<T> UpdateAsync<T>(string table, string query, object data) where T : class;
    Task DeleteAsync(string table, string query);
    Task<T?> RpcAsync<T>(string function, object? parameters = null);
}

public class SupabaseHttpClient : ISupabaseClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<SupabaseHttpClient> _logger;

    public SupabaseHttpClient(
        HttpClient httpClient,
        IOptions<AppSettings> settings,
        ILogger<SupabaseHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        var supabaseSettings = settings.Value.Supabase;
        _httpClient.BaseAddress = new Uri($"{supabaseSettings.Url}/rest/v1/");
        _httpClient.DefaultRequestHeaders.Add("apikey", supabaseSettings.SecretKey);
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", supabaseSettings.SecretKey);
        _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<T?> GetSingleAsync<T>(string table, string query) where T : class
    {
        var url = string.IsNullOrEmpty(query) ? table : $"{table}?{query}";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<T>>(content, _jsonOptions);
        
        return items?.FirstOrDefault();
    }

    public async Task<List<T>> GetListAsync<T>(string table, string query = "") where T : class
    {
        var url = string.IsNullOrEmpty(query) ? table : $"{table}?{query}";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<T>>(content, _jsonOptions) ?? new List<T>();
    }

    public async Task<T> InsertAsync<T>(string table, T data) where T : class
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        _logger.LogInformation("Inserting into {Table}: {Data}", table, json);
        
        var response = await _httpClient.PostAsync(table, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Insert failed with {StatusCode}: {Error}", response.StatusCode, error);
        }
        
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<T>>(responseContent, _jsonOptions);
        
        return items?.First() ?? throw new InvalidOperationException("Insert failed");
    }

    public async Task<T> UpdateAsync<T>(string table, string query, object data) where T : class
    {
        var url = $"{table}?{query}";
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<T>>(responseContent, _jsonOptions);
        
        return items?.First() ?? throw new InvalidOperationException("Update failed");
    }

    public async Task DeleteAsync(string table, string query)
    {
        var url = $"{table}?{query}";
        var response = await _httpClient.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
    }

    public async Task<T?> RpcAsync<T>(string function, object? parameters = null)
    {
        var json = parameters != null 
            ? JsonSerializer.Serialize(parameters, _jsonOptions) 
            : "{}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"rpc/{function}", content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
    }
}
