using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CrystalLauncher.Models;

namespace CrystalLauncher.Services;

public sealed class LauncherConfigService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _configPath;
    private readonly string _templatePath;

    public LauncherConfigService(string configPath, string templatePath)
    {
        _configPath = configPath;
        _templatePath = templatePath;
    }

    public string ConfigPath => _configPath;

    public async Task<LauncherConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConfigFileAsync(cancellationToken).ConfigureAwait(false);

        var config = await LoadFromFileAsync(_configPath, cancellationToken).ConfigureAwait(false) ?? new LauncherConfig();

        if (!string.IsNullOrWhiteSpace(config.RawConfigUrl))
        {
            var remoteConfig = await TryLoadFromRemoteAsync(config.RawConfigUrl, cancellationToken).ConfigureAwait(false);
            if (remoteConfig != null)
            {
                if (string.IsNullOrWhiteSpace(remoteConfig.RawConfigUrl))
                {
                    remoteConfig.RawConfigUrl = config.RawConfigUrl;
                }

                return remoteConfig;
            }
        }

        return config;
    }

    private async Task EnsureConfigFileAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(_configPath))
        {
            return;
        }

        if (File.Exists(_templatePath))
        {
            using var templateStream = File.OpenRead(_templatePath);
            await using var output = File.Create(_configPath);
            await templateStream.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            return;
        }

        await using var stream = File.Create(_configPath);
        await JsonSerializer.SerializeAsync(stream, new LauncherConfig(), SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<LauncherConfig?> LoadFromFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<LauncherConfig>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<LauncherConfig?> TryLoadFromRemoteAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<LauncherConfig>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}
