using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
    private readonly string _uadUrlBase; // Base URL for UAD JSON files
    private readonly TimeSpan _cacheTtl = TimeSpan.FromDays(7); // 7-day TTL as required
    private readonly HashSet<string> _knownDevices;
    private readonly HashSet<string> _curatedFiles;
    private readonly HashSet<string> _uadFiles; // UAD JSON files to fetch

    // Cached data
    private DateTime _lastUpdatedUtc;
    private IReadOnlySet<string> _curatedTier2 = new HashSet<string>();
    private readonly Dictionary<string, DeviceConfig> _deviceConfigs = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, UadAppInfo> _uadApps = new Dictionary<string, UadAppInfo>();

    public ConfigSyncService(
        HttpMessageHandler httpHandler,
        string cacheDir,
        string urlBase,
        IReadOnlyList<string> knownDevices,
        IReadOnlyList<string> curatedFiles)
        : this(httpHandler, cacheDir, urlBase, "https://raw.githubusercontent.com/Universal-Debloater-Alliance/universal-android-debloater-next-generation/main/resources/assets/uad_lists.json", knownDevices, curatedFiles, Array.Empty<string>())
    {
    }

    public ConfigSyncService(
        HttpMessageHandler httpHandler,
        string cacheDir,
        string urlBase,
        string uadUrlBase,
        IReadOnlyList<string> knownDevices,
        IReadOnlyList<string> curatedFiles,
        IReadOnlyList<string> uadFiles)
    {
        _http = new HttpClient(httpHandler) { Timeout = TimeSpan.FromSeconds(15) };
        _cacheDir = cacheDir;
        _urlBase = urlBase.TrimEnd('/');
        _uadUrlBase = uadUrlBase.TrimEnd('/');
        _knownDevices = new HashSet<string>(knownDevices, StringComparer.OrdinalIgnoreCase);
        _curatedFiles = new HashSet<string>(curatedFiles, StringComparer.OrdinalIgnoreCase);
        _uadFiles = new HashSet<string>(uadFiles, StringComparer.OrdinalIgnoreCase);
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Synchronize all configs from GitHub, updating internal cache.
    /// Returns true if sync succeeded, false if failed and no cached data available.
    /// </summary>
    public async Task<bool> SyncAsync(CancellationToken ct)
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

            // Fetch UAD JSON files
            var uadApps = new Dictionary<string, UadAppInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in _uadFiles)
            {
                var url = $"{_uadUrlBase}/{file}";
                var cacheName = $"uad_{file}";
                var text = await FetchWithCacheAsync(url, cacheName, ct).ConfigureAwait(false);
                if (text != null)
                {
                    var apps = ParseUadJson(text);
                    foreach (var app in apps)
                    {
                        // If duplicate id, we overwrite (or we could skip, but we'll overwrite with the last one)
                        uadApps[app.Id] = app;
                    }
                }
            }
            _uadApps = uadApps;

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
        var etagPath = Path.Combine(_cacheDir, cacheName + ".etag");

        // If we have a cached ETag, use it for If-None-Match
        if (File.Exists(etagPath))
        {
            var etag = await File.ReadAllTextAsync(etagPath, ct).ConfigureAwait(false);
            _http.DefaultRequestHeaders.Remove("If-None-Match");
            _http.DefaultRequestHeaders.Add("If-None-Match", etag);
        }

        HttpResponseMessage? response = null;
        try
        {
            response = await _http.GetAsync(url, ct).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                // Use cached content
                if (File.Exists(cachePath))
                {
                    return await File.ReadAllTextAsync(cachePath, ct).ConfigureAwait(false);
                }
                else
                {
                    // Cache missing but server says not modified - this shouldn't happen
                    return null;
                }
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                // Update cache
                await File.WriteAllTextAsync(cachePath, content, ct).ConfigureAwait(false);
                // Save ETag if present
                if (response.Headers.TryGetValues("ETag", out var etagValues))
                {
                    var etag = etagValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(etag))
                    {
                        await File.WriteAllTextAsync(etagPath, etag, ct).ConfigureAwait(false);
                    }
                }
                return content;
            }
            else
            {
                // Error status code - fallback to cache if available
                if (File.Exists(cachePath) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath)) < _cacheTtl)
                {
                    return await File.ReadAllTextAsync(cachePath, ct).ConfigureAwait(false);
                }
                return null;
            }
        }
        catch
        {
            // Network error etc. - fallback to cache if available
            if (File.Exists(cachePath) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath)) < _cacheTtl)
            {
                return await File.ReadAllTextAsync(cachePath, ct).ConfigureAwait(false);
            }
            return null;
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <summary>
    /// Parse UAD JSON text into a list of UadAppInfo.
    /// Expected format: array of objects with id, label, description, removal, suggestions (array of strings).
    /// </summary>
    private static List<UadAppInfo> ParseUadJson(string jsonText)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonText);
            if (jsonDoc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new List<UadAppInfo>();
            }

            var apps = new List<UadAppInfo>();
            foreach (var element in jsonDoc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                    continue;

                string id = "";
                string label = "";
                string description = "";
                string removal = "";
                List<string> suggestions = new();

                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("id"))
                    {
                        id = property.Value.GetString() ?? "";
                    }
                    else if (property.NameEquals("label"))
                    {
                        label = property.Value.GetString() ?? "";
                    }
                    else if (property.NameEquals("description"))
                    {
                        description = property.Value.GetString() ?? "";
                    }
                    else if (property.NameEquals("removal"))
                    {
                        removal = property.Value.GetString() ?? "";
                    }
                    else if (property.NameEquals("suggestions"))
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var sug in property.Value.EnumerateArray())
                            {
                                if (sug.ValueKind == JsonValueKind.String)
                                {
                                    suggestions.Add(sug.GetString()!);
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(id))
                {
                    apps.Add(new UadAppInfo
                    {
                        Id = id,
                        Label = label,
                        Description = description,
                        Removal = removal,
                        Suggestions = suggestions
                    });
                }
            }

            return apps;
        }
        catch
        {
            // If parsing fails, return empty list
            return new List<UadAppInfo>();
        }
    }

    public IReadOnlySet<string> CuratedTier2 => _curatedTier2;
    public DateTime LastUpdatedUtc => _lastUpdatedUtc;
    public IReadOnlyDictionary<string, DeviceConfig> DeviceConfigs => _deviceConfigs;

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
            if (string.Equals(config.Model, modelProp, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(config.Maker, brandProp, StringComparison.OrdinalIgnoreCase))
            {
                return config;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the parsed UAD app information.
    /// </summary>
    public IReadOnlyDictionary<string, UadAppInfo> GetUadApps() => _uadApps;
    public IReadOnlyDictionary<string, UadAppInfo> GetCachedUadApps() => _uadApps;
}

/// <summary>
/// Information about an app from the UAD lists.
/// </summary>
public class UadAppInfo
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public string Removal { get; set; } = ""; // Recommended, Advanced, Expert, Unsafe
    public List<string> Suggestions { get; set; } = new();
}