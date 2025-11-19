using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using CrystalLauncher.Infrastructure;
using CrystalLauncher.Models;
using CrystalLauncher.Services;

namespace CrystalLauncher.ViewModels;

public sealed class LauncherViewModel : INotifyPropertyChanged
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LauncherConfigService _configService;
    private readonly LauncherConfig _config;

    private readonly string? _clientDirectory;
    private readonly string? _gameExecutablePath;
    private readonly string? _websiteUrl;
    private readonly string? _assetsManifestPath;

    private double _downloadProgress;
    private string _downloadStatus = "Pronto para verificar atualizações";
    private bool _isBusy;
    private ServerState _serverState;

    public LauncherViewModel(LauncherConfig config, LauncherConfigService configService)
    {
        _config = config ?? new LauncherConfig();
        _configService = configService;

        _clientDirectory = ResolvePath(_config.ClientDirectory);
        _assetsManifestPath = ResolveAssetsManifestPath(_clientDirectory, _config.AssetsManifest);
        _gameExecutablePath = ResolveGameExecutablePath(_config.GameExecutable, _clientDirectory);
        _websiteUrl = _config.WebsiteUrl;

        GameTitle = string.IsNullOrWhiteSpace(_config.GameTitle) ? "Crystal Shards" : _config.GameTitle!;
        Tagline = string.IsNullOrWhiteSpace(_config.Tagline) ? "Desperte o poder antigo e proteja o seu reino" : _config.Tagline!;
        BuildVersion = string.IsNullOrWhiteSpace(_config.BuildVersion) ? "Build 1.0.0" : _config.BuildVersion!;

        ClientDirectoryDisplay = string.IsNullOrWhiteSpace(_clientDirectory)
            ? "Client não configurado"
            : _clientDirectory;

        ServerState = ParseServerState(_config.DefaultServerState) ?? ServerState.Online;

        News = new ObservableCollection<NewsEntry>();
        PatchNotes = new ObservableCollection<PatchNoteEntry>();

        if (_config.News is { Count: > 0 })
        {
            ReplaceCollection(News, _config.News);
        }
        else
        {
            SeedDefaultNews();
        }

        if (_config.PatchNotes is { Count: > 0 })
        {
            ReplaceCollection(PatchNotes, _config.PatchNotes);
        }
        else
        {
            SeedDefaultPatchNotes();
        }

        PlayCommand = new RelayCommand(_ => TriggerPlay(), _ => !IsBusy && IsGameExecutableAvailable);
        VerifyCommand = new RelayCommand(_ => VerifyClientAsync(), _ => !IsBusy);
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings(), _ => !IsBusy);
        OpenSiteCommand = new RelayCommand(_ => OpenSite(), _ => !string.IsNullOrWhiteSpace(_websiteUrl));

        _ = InitializeAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string GameTitle { get; }

    public string Tagline { get; }

    public string BuildVersion { get; }

    public string ClientDirectoryDisplay { get; }

    public ObservableCollection<NewsEntry> News { get; }

    public ObservableCollection<PatchNoteEntry> PatchNotes { get; }

    public ICommand PlayCommand { get; }

    public ICommand VerifyCommand { get; }

    public ICommand OpenSiteCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public double DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            if (Math.Abs(_downloadProgress - value) < 0.001)
            {
                return;
            }

            _downloadProgress = value;
            OnPropertyChanged();
        }
    }

    public string DownloadStatus
    {
        get => _downloadStatus;
        set
        {
            if (_downloadStatus == value)
            {
                return;
            }

            _downloadStatus = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public bool IsGameExecutableAvailable => !string.IsNullOrWhiteSpace(_gameExecutablePath) && File.Exists(_gameExecutablePath);

    public ServerState ServerState
    {
        get => _serverState;
        private set
        {
            if (_serverState == value)
            {
                return;
            }

            _serverState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ServerStatusLabel));
            OnPropertyChanged(nameof(ServerStatusBrush));
        }
    }

    public string ServerStatusLabel => ServerState switch
    {
        ServerState.Online => "SERVIDORES ONLINE",
        ServerState.Maintenance => "EM MANUTENÇÃO",
        ServerState.Offline => "SERVIDORES OFFLINE",
        _ => "STATUS DESCONHECIDO"
    };

    public Brush ServerStatusBrush => ServerState switch
    {
        ServerState.Online => new SolidColorBrush(Color.FromRgb(73, 228, 255)),
        ServerState.Maintenance => new SolidColorBrush(Color.FromRgb(255, 202, 77)),
        ServerState.Offline => new SolidColorBrush(Color.FromRgb(255, 94, 94)),
        _ => Brushes.White
    };

    private async Task InitializeAsync()
    {
        if (!string.IsNullOrWhiteSpace(_config.NewsFeed))
        {
            await LoadNewsFromEndpointAsync(_config.NewsFeed!);
        }

        if (!string.IsNullOrWhiteSpace(_config.PatchNotesFeed))
        {
            await LoadPatchNotesFromEndpointAsync(_config.PatchNotesFeed!);
        }

        if (!string.IsNullOrWhiteSpace(_config.StatusEndpoint))
        {
            await UpdateServerStatusAsync(_config.StatusEndpoint!);
        }

        RaiseCommandStates();
    }

    private async Task LoadNewsFromEndpointAsync(string endpoint)
    {
        try
        {
            using var httpClient = new HttpClient();
            await using var stream = await httpClient.GetStreamAsync(endpoint);
            var entries = await JsonSerializer.DeserializeAsync<List<NewsEntry>>(stream, JsonOptions);
            if (entries is { Count: > 0 })
            {
                ReplaceCollection(News, entries);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Falha ao carregar novidades de {endpoint}: {ex}");
        }
    }

    private async Task LoadPatchNotesFromEndpointAsync(string endpoint)
    {
        try
        {
            using var httpClient = new HttpClient();
            await using var stream = await httpClient.GetStreamAsync(endpoint);
            var entries = await JsonSerializer.DeserializeAsync<List<PatchNoteEntry>>(stream, JsonOptions);
            if (entries is { Count: > 0 })
            {
                ReplaceCollection(PatchNotes, entries);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Falha ao carregar patch notes de {endpoint}: {ex}");
        }
    }

    private async Task UpdateServerStatusAsync(string endpoint)
    {
        try
        {
            using var httpClient = new HttpClient();
            await using var stream = await httpClient.GetStreamAsync(endpoint);
            using var document = await JsonDocument.ParseAsync(stream);
            var state = ExtractServerState(document.RootElement);
            if (state.HasValue)
            {
                ServerState = state.Value;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Falha ao consultar status do servidor em {endpoint}: {ex}");
        }
    }

    private void VerifyClientAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_clientDirectory))
        {
            DownloadStatus = "Diretório do client não configurado.";
            return;
        }

        if (!Directory.Exists(_clientDirectory))
        {
            DownloadStatus = $"Diretório do client não encontrado: {_clientDirectory}";
            return;
        }

        var manifestPath = ResolveManifestPath();
        if (manifestPath == null || !File.Exists(manifestPath))
        {
            DownloadStatus = "Manifesto assets.json não encontrado.";
            return;
        }

        _ = RunVerificationAsync(manifestPath);
    }

    private async Task RunVerificationAsync(string manifestPath)
    {
        try
        {
            IsBusy = true;
            DownloadProgress = 0;
            DownloadStatus = "Verificando arquivos do client...";

            await using var stream = File.OpenRead(manifestPath);
            var entries = await JsonSerializer.DeserializeAsync<List<AssetManifestEntry>>(stream, JsonOptions);

            if (entries == null || entries.Count == 0)
            {
                DownloadStatus = "Manifesto vazio. Nenhum arquivo verificado.";
                return;
            }

            var missing = new List<string>();
            var total = entries.Count;
            var processed = 0;

            foreach (var entry in entries)
            {
                processed++;

                if (!string.IsNullOrWhiteSpace(entry.LocalFile) && _clientDirectory != null)
                {
                    var localPath = ResolveClientFilePath(_clientDirectory, entry.LocalFile);
                    if (!File.Exists(localPath))
                    {
                        missing.Add(entry.LocalFile);
                    }
                }

                DownloadProgress = processed * 100.0 / total;
                await Task.Delay(5);
            }

            DownloadProgress = 100;

            if (missing.Count > 0)
            {
                var preview = string.Join(", ", missing.Take(3));
                DownloadStatus = missing.Count == 1
                    ? $"1 arquivo faltando: {preview}"
                    : $"{missing.Count} arquivos faltando. Exemplos: {preview}";
            }
            else
            {
                DownloadStatus = "Verificação concluída. Nenhum arquivo faltando.";
            }
        }
        catch (Exception ex)
        {
            DownloadStatus = $"Erro durante verificação: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void TriggerPlay()
    {
        if (!IsGameExecutableAvailable)
        {
            DownloadStatus = "Executável não configurado ou não encontrado.";
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _gameExecutablePath!,
                WorkingDirectory = Path.GetDirectoryName(_gameExecutablePath!),
                UseShellExecute = true
            };

            Process.Start(startInfo);
            DownloadStatus = "Iniciando o client...";
        }
        catch (Exception ex)
        {
            DownloadStatus = $"Falha ao iniciar o client: {ex.Message}";
        }
    }

    private void OpenSite()
    {
        if (string.IsNullOrWhiteSpace(_websiteUrl))
        {
            DownloadStatus = "URL do site não configurada.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _websiteUrl,
                UseShellExecute = true
            });
            DownloadStatus = "Abrindo site oficial...";
        }
        catch (Exception ex)
        {
            DownloadStatus = $"Não foi possível abrir o site: {ex.Message}";
        }
    }

    private void OpenSettings()
    {
        try
        {
            if (!File.Exists(_configService.ConfigPath))
            {
                DownloadStatus = "Arquivo de configuração não encontrado.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _configService.ConfigPath,
                UseShellExecute = true
            });
            DownloadStatus = "Abrindo configurações do launcher...";
        }
        catch (Exception ex)
        {
            DownloadStatus = $"Falha ao abrir config: {ex.Message}";
        }
    }

    private void RaiseCommandStates()
    {
        if (PlayCommand is RelayCommand relayPlay)
        {
            relayPlay.RaiseCanExecuteChanged();
        }

        if (VerifyCommand is RelayCommand relayVerify)
        {
            relayVerify.RaiseCanExecuteChanged();
        }

        if (OpenSettingsCommand is RelayCommand relaySettings)
        {
            relaySettings.RaiseCanExecuteChanged();
        }

        if (OpenSiteCommand is RelayCommand relaySite)
        {
            relaySite.RaiseCanExecuteChanged();
        }
    }

    private string? ResolveManifestPath()
    {
        if (!string.IsNullOrWhiteSpace(_assetsManifestPath))
        {
            return _assetsManifestPath;
        }

        return _clientDirectory == null ? null : Path.Combine(_clientDirectory, "assets.json");
    }

    private static string? ResolveAssetsManifestPath(string? clientDirectory, string? manifestSetting)
    {
        if (string.IsNullOrWhiteSpace(manifestSetting))
        {
            return null;
        }

        if (Path.IsPathRooted(manifestSetting))
        {
            return manifestSetting;
        }

        if (clientDirectory == null)
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, manifestSetting));
        }

        return Path.Combine(clientDirectory, manifestSetting.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string? ResolveGameExecutablePath(string? executableSetting, string? clientDirectory)
    {
        if (!string.IsNullOrWhiteSpace(executableSetting))
        {
            return ResolvePathRelativeToBase(executableSetting, clientDirectory);
        }

        if (clientDirectory != null)
        {
            var packagePath = Path.Combine(clientDirectory, "package.json");
            if (File.Exists(packagePath))
            {
                try
                {
                    using var stream = File.OpenRead(packagePath);
                    using var document = JsonDocument.Parse(stream);
                    if (document.RootElement.TryGetProperty("executable", out var executableElement))
                    {
                        var packageExecutable = executableElement.GetString();
                        if (!string.IsNullOrWhiteSpace(packageExecutable))
                        {
                            return Path.Combine(clientDirectory, packageExecutable.Replace('/', Path.DirectorySeparatorChar));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Falha ao ler package.json: {ex}");
                }
            }
        }

        return null;
    }

    private static string? ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private static string? ResolvePathRelativeToBase(string path, string? clientDirectory)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        if (!string.IsNullOrWhiteSpace(clientDirectory))
        {
            return Path.Combine(clientDirectory, path.Replace('/', Path.DirectorySeparatorChar));
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private static string ResolveClientFilePath(string clientDirectory, string relativePath)
    {
        var sanitized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(clientDirectory, sanitized);
    }

    private static ServerState? ExtractServerState(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (TryGetString(root, "state", out var stateString) || TryGetString(root, "status", out stateString))
            {
                var parsed = ParseServerState(stateString);
                if (parsed.HasValue)
                {
                    return parsed;
                }
            }

            if (TryGetBoolean(root, "online", out var isOnline))
            {
                return isOnline ? ServerState.Online : ServerState.Offline;
            }

            if (TryGetBoolean(root, "maintenance", out var maintenance) && maintenance)
            {
                return ServerState.Maintenance;
            }
        }

        return null;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string? value)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            value = prop.GetString();
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryGetBoolean(JsonElement element, string propertyName, out bool value)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.True)
            {
                value = true;
                return true;
            }

            if (prop.ValueKind == JsonValueKind.False)
            {
                value = false;
                return true;
            }
        }

        value = false;
        return false;
    }

    private static ServerState? ParseServerState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        return state.Trim().ToLowerInvariant() switch
        {
            "online" => ServerState.Online,
            "maintenance" or "manutenção" or "manutencao" => ServerState.Maintenance,
            "offline" => ServerState.Offline,
            _ => null
        };
    }

    private void SeedDefaultNews()
    {
        ReplaceCollection(News, new[]
        {
            new NewsEntry("Temporada Eclipse", "Eventos dinâmicos chegam com novos desafios", "19 NOV 2025", "EVENTO"),
            new NewsEntry("Passe de Batalha", "Colecione aparências exclusivas e recompensas limitadas", "16 NOV 2025", "LOJA"),
            new NewsEntry("Atualização de Guildas", "Ferramentas avançadas para líderes estratégicos", "10 NOV 2025", "GUILDAS")
        });
    }

    private void SeedDefaultPatchNotes()
    {
        ReplaceCollection(PatchNotes, new[]
        {
            new PatchNoteEntry("1.0.0", "Release inicial do Crystal Launcher com visual moderno e monitoramento de status."),
            new PatchNoteEntry("0.9.4", "Otimizações na verificação de integridade e suporte a múltiplas regiões."),
            new PatchNoteEntry("0.9.0", "Reestruturação do pipeline de atualização com feedback em tempo real.")
        });
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
