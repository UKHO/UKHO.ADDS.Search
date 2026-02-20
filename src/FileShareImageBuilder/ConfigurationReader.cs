using System.Text.Json;

namespace FileShareImageBuilder;

internal static class ConfigurationReader
{
    internal static string GetRemoteServiceBaseAddress()
    {
        using var json = ReadOverrideConfiguration();
        return GetRequiredStringProperty(json.RootElement, "remoteService");
    }

    internal static string GetSourceDatabaseConnectionString()
    {
        using var json = ReadOverrideConfiguration();
        return GetRequiredStringProperty(json.RootElement, "sourceDatabase");
    }

    internal static string GetTargetDatabaseConnectionString(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException("Database name is required.", nameof(databaseName));
        }

        var targetConnectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{databaseName}");
        if (string.IsNullOrWhiteSpace(targetConnectionString))
        {
            throw new InvalidOperationException($"Missing required environment variable 'ConnectionStrings__{databaseName}'.");
        }

        return targetConnectionString;
    }

    internal static string GetRemoteServiceScope()
    {
        using var json = ReadOverrideConfiguration();
        return GetRequiredStringProperty(json.RootElement, "remoteServiceScope");
    }

    internal static string GetDataImagePath()
    {
        using var json = ReadOverrideConfiguration();
        return GetRequiredStringProperty(json.RootElement, "dataImagePath");
    }

    internal static int GetDataImageBinSizeGB()
    {
        using var json = ReadOverrideConfiguration();

        if (!json.RootElement.TryGetProperty("dataImageBinSizeGB", out var element))
        {
            throw new InvalidOperationException("Missing 'dataImageBinSizeGB' in configuration.override.json.");
        }

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value) || value <= 0)
        {
            throw new InvalidOperationException("Invalid 'dataImageBinSizeGB'. Must be a positive integer.");
        }

        return value;
    }

    private static string GetRequiredStringProperty(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            throw new InvalidOperationException($"Missing '{propertyName}' in configuration.override.json.");
        }

        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing '{propertyName}'.");
        }

        return value;
    }

    private static JsonDocument ReadOverrideConfiguration()
    {
        var overrideFilePath = Path.Combine(AppContext.BaseDirectory, "configuration.override.json");
        if (!File.Exists(overrideFilePath))
        {
            throw new FileNotFoundException("Missing required configuration override file.", overrideFilePath);
        }

        using var stream = File.OpenRead(overrideFilePath);
        return JsonDocument.Parse(stream);
    }
}
