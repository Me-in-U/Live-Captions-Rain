using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveCaptionsRain.Core.Settings;

public sealed class SettingsStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Path { get; } = path;

    public async Task<LiveCaptionsRainSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(Path))
        {
            return LiveCaptionsRainSettings.CreateDefault();
        }

        try
        {
            await using var stream = File.OpenRead(Path);
            var settings = await JsonSerializer.DeserializeAsync<LiveCaptionsRainSettings>(stream, JsonOptions, cancellationToken);
            return (settings ?? LiveCaptionsRainSettings.CreateDefault()).Sanitize();
        }
        catch (JsonException)
        {
            return LiveCaptionsRainSettings.CreateDefault();
        }
        catch (IOException)
        {
            return LiveCaptionsRainSettings.CreateDefault();
        }
    }

    public async Task SaveAsync(LiveCaptionsRainSettings settings, CancellationToken cancellationToken = default)
    {
        var folder = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await using var stream = File.Create(Path);
        await JsonSerializer.SerializeAsync(stream, settings.Sanitize(), JsonOptions, cancellationToken);
    }
}
