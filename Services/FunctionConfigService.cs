using BKBToolClient.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text; // <-- Diese Zeile stellt Encoding bereit

namespace BKBToolClient.Services;

public class FunctionConfigService
{
    private readonly HttpClient _http;
    private List<FunctionConfig>? _cachedConfigs;
    // kept for compatibility with previous versions (Edit-and-Continue)
    private List<string> _allowedSchoolNumbers = new();

    public FunctionConfigService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<FunctionConfig>> LoadFunctionsAsync()
    {
        if (_cachedConfigs != null)
            return _cachedConfigs;

        try
        {
            var resp = await _http.GetAsync("config/functions.json");
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"FunctionConfigService: failed to fetch config/functions.json - {resp.StatusCode}");
                return new List<FunctionConfig>();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            try
            {
                // Read raw bytes and decode as UTF-8 to avoid charset problems that cause replacement chars
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("FunctionConfigService: functions.json is empty");
                    return new List<FunctionConfig>();
                }

                // preview logging removed to avoid verbose output in browser console

                var wrapper = JsonSerializer.Deserialize<FunctionConfigWrapper>(json, options);
                if (wrapper == null)
                {
                    Console.WriteLine("FunctionConfigService: functions.json deserialized to null wrapper");
                    _cachedConfigs = new List<FunctionConfig>();
                    return _cachedConfigs;
                }

                _cachedConfigs = wrapper.Functions ?? new List<FunctionConfig>();
                return _cachedConfigs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FunctionConfigService: error deserializing functions.json: {ex}");
                return new List<FunctionConfig>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FunctionConfigService: exception while loading functions.json: {ex}");
            return new List<FunctionConfig>();
        }
    }

    public (bool IsValid, string ErrorMessage) ValidateField(InputField field, string value)
    {
        if (field.IsRequired && string.IsNullOrWhiteSpace(value))
            return (false, $"{field.Label} ist erforderlich");

        if (!string.IsNullOrEmpty(field.ValidationRegex))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, field.ValidationRegex))
                return (false, field.ValidationErrorMessage);
        }

        return (true, string.Empty);
    }

    private class FunctionConfigWrapper
    {
        public List<FunctionConfig> Functions { get; set; } = new();
    }
}
