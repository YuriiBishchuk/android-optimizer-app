using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TvOptimizer.Core.Config;

/// <summary>
/// Core service for fetching and caching device/config files from GitHub with ETag support and 7-day TTL.
/// Testable via injected HttpMessageHandler.
/// </summary>
public class ConfigSyncService
{
    private readonly HttpClient _http;
    private readonly string _cacheDir;
    private readonly string _urlBase;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromDays(7); // 7-day TTL as required
    private readonly HashSet<string> _knownDevices;
    private readonly HashSet<string> _curatedFiles;

    // Cached data
    private DateTime _lastUpdatedUtc;
    private IReadOnlySet<string> _curatedTier2 = new HashSet<string>();
    private readonly Dictionary<string, DeviceConfig> _deviceConfigs = new(StringComparer.OrdinalIgnoreCase);

    public ConfigSyncService(
        HttpMessageHandler httpHandler,
        string cacheDir,
        string urlBase,
        IReadOnlyList<string> knownDevices,
        IReadOnlyList<string> curatedFiles)
    {
        _http = new HttpClient(httpHandler) { Timeout = TimeSpan.FromSeconds(15) };
        _cacheDir = cacheDir;
        _urlBase = urlBase.TrimEnd('/');
        _knownDevices = new HashSet<string>(knownDevices, StringComparer.OrdinalIgnoreCase);
        _curatedFiles = new HashSet<string>(curatedFiles, StringComparer.OrdinalIgnoreCase);
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Synchronize all configs from GitHub, updating internal cache.
    /// Returns true if sync succeeded, false if failed and no cached data available.
    /// </summary>
    public async Task<bool> SyncAsync(CancellationToken ct = default)
    {
        try
        {
            _lastUpdatedUtc = DateTime.UtcNow;

            // Fetch curated files
            var curated = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in _curatedFiles)
            {
                var url = $"{_urlBase}/data/community/curated/{file}";
                var cacheName = $"curated_{file}";
                var text = await FetchWithCacheAsync(url, cacheName, ct).ConfigureAwait(false);
                if (text != null)
                {
                    curated.UnionWith(ConfigParser.ParseCurated(text));
                }
            }
            _curatedTier2 = curated;

            // Fetch device configs
            foreach (var device in _knownDevices)
            {
                var url = $"{_urlBase}/devices/{device}.conf";
                var cacheName = $"device_{device}.conf";
                var text = await FetchWithCacheAsync(url, cacheName, ct).ConfigureAwait(false);
                if (text != null)
                {
                    var config = ConfigParser.ParseDeviceConf(device, text);
                    _deviceConfigs[device] = config;
                }
            }

            return true;
        }
        catch
        {
            // Sync failed - will use cached data if available (stale fallback)
            return _lastUpdatedUtc != default && (DateTime.UtcNow - _lastUpdatedUtc) < _cacheTtl;
        }
    }

    /// <summary>
    /// Fetch a URL with ETag/If-None-Match support, 7-day cache TTL, and stale-cache fallback when offline.
    /// Returns null if fetch failed and no usable cache exists.
    /// </summary>
    public async Task<string?> FetchWithCacheAsync(string url, string cacheName, CancellationToken ct)
    {
        var cachePath = Path.Combine(_cacheDir, cacheName);
        var etagPath = cachePath + ".etag";

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(12));

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Add If-None-Match header if we have a saved ETag
            if (File.Exists(etagPath))
            {
                var etag = await File.ReadAllTextAsync(etagPath, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(etag))
                {
                    request.Headers.Add("If-None-Match", etag.Trim('\"'));
                }
            }

            using var response = await _http.SendAsync(request, cts.Token).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                // 304 Not Modified - use cached content if it exists and is fresh enough
                if (File.Exists(cachePath))
                {
                    var cacheAge = DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath);
                    if (cacheAge < _cacheTtl)
                    {
                        return await File.ReadAllTextAsync(cachePath, ct).ConfigureAwait(false);
                    }
                }
                // If cache is stale or missing, we'll fall through to return null below
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            await File.WriteAllTextAsync(cachePath, content, ct).ConfigureAwait(false);

            // Save ETag if present
            if (response.Headers.TryGetValues("ETag", out var etagValues))
            {
                foreach (var etag in etagValues)
                {
                    await File.WriteAllTextAsync(etagPath, etag.Trim(), ct).ConfigureAwait(false);
                    break; // Only take the first ETag
                }
            }

            return content;
        }
        catch
        {
            // Offline or network error: try to use cache if it's not too stale (< 30 days as per existing behavior)
            if (File.Exists(cachePath) &&
                DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath) < TimeSpan.FromDays(30))
            {
                return await File.ReadAllTextAsync(cachePath, ct).ConfigureAwait(false);
            }
            return null;
        }
    }

    /// <summary>
    /// Try to match a device config by fingerprint (model + maker).
    /// Returns null if no match found (indicating GENERIC_MODE should be used).
    /// </summary>
    public DeviceConfig? MatchDevice(string modelProp, string brandProp)
    {
        if (string.IsNullOrWhiteSpace(modelProp) || string.IsNullOrWhiteSpace(brandProp))
            return null;

        modelProp = modelProp.Trim();
        brandProp = brandProp.Trim();

        foreach (var config in _deviceConfigs.Values)
        {
            // Case-insensitive match on model and maker
            if (string.Equals(config.Model, modelProp, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(config.Maker, brandProp, StringComparison.OrdinalIgnoreCase))
            {
                return config;
            }
        }

        // No match found -> GENERIC_MODE
        return null;
    }

    /// <summary>
    /// Get the timestamp of the last successful sync.
    /// </summary>
    public DateTime? LastUpdatedUtc => _lastUpdatedUtc == default ? (DateTime?)null : _lastUpdatedUtc;

    /// <summary>
    /// Get the cached curated tier-2 packages.
    /// </summary>
    public IReadOnlySet<string> CuratedTier2 => _curatedTier2;

    /// <summary>
    /// Get all cached device configs.
    /// </summary>
    public IReadOnlyDictionary<string, DeviceConfig> DeviceConfigs => _deviceConfigs;
}