using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TvOptimizer.Core.Config;

namespace TvOptimizer.App.Services;

/// <summary>
/// Syncs device configs from GitHub with offline cache support.
/// </summary>
public sealed class ConfigSync
{
    public static ConfigSync Current { get; } = new();

    private readonly ConfigSyncService _service;
    private string CacheDir => Path.Combine(FileSystem.AppDataDirectory, "configs");

    public DeviceConfig? LastDevice { get; private set; }
    public IReadOnlySet<string> CuratedTier2 { get; private set; } = new HashSet<string>();
    public string Status { get; private set; } = "не синхронізовано";
    public DateTime? LastUpdatedUtc { get; private set; }

    public ConfigSync()
    {
        _service = new ConfigSyncService(
            httpHandler: new HttpClientHandler(),
            cacheDir: CacheDir,
            urlBase: ConfigUrls.UrlBase,
            knownDevices: ConfigUrls.KnownDevices,
            curatedFiles: ConfigUrls.CuratedFiles
        );
    }

    /// <summary>Sync all configs from GitHub (or cache). Returns true if synced successfully.</summary>
    public async Task<bool> SyncAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(CacheDir);

        var success = await _service.SyncAsync(ct).ConfigureAwait(false);
        if (!success)
        {
            Status = "помилка синхронізації";
            return false;
        }

        CuratedTier2 = _service.CuratedTier2;
        LastUpdatedUtc = _service.LastUpdatedUtc;

        // Try to match the first device config as LastDevice (best-effort)
        var deviceConfigs = _service.DeviceConfigs;
        if (deviceConfigs.Count > 0)
        {
            LastDevice = deviceConfigs.Values.FirstOrDefault();
        }

        var sb = new System.Text.StringBuilder($"curated TIER_2: {CuratedTier2.Count} pkg. devices:");
        foreach (var kvp in deviceConfigs)
        {
            var cfg = kvp.Value;
            sb.Append(' ').Append(cfg.Name).Append('(').Append(cfg.Model).Append(')');
        }
        Status = sb.ToString();

        return true;
    }

    /// <summary>Force immediate refresh, ignoring cache.</summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await SyncAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Match device by fingerprint. Returns null → generic mode (community lists + universal safe list only).
    /// </summary>
    public DeviceConfig? MatchDevice(string modelProp, string brandProp)
    {
        if (string.IsNullOrWhiteSpace(modelProp) || string.IsNullOrWhiteSpace(brandProp))
            return null;

        return _service.MatchDevice(modelProp, brandProp);
    }
}