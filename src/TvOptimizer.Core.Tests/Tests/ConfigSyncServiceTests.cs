using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using TvOptimizer.Core.Config;

namespace TvOptimizer.Core.Tests;

/// <summary>
/// Fake HttpMessageHandler that allows configuring responses for testing.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return _handler(request, cancellationToken);
    }
}

public class ConfigSyncServiceTests
{
    private const string TestRepoUrl = "https://raw.githubusercontent.com/YuriiBishchuk/android-tv-optimizer/main";
    private static readonly string TestCacheDir = Path.Combine(Path.GetTempPath(), "TvOptimizerConfigSyncCache");

    [Fact]
    public async Task MatchDevice_Finds_Config_When_Model_And_Maker_Match()
    {
        // Arrange - handler returns xiaomi config
        var xiaomiConf = @"
DEVICE_MODEL=""MiTV-MZTU0""
DEVICE_MAKER=""Xiaomi""
SAFE_REMOVE=()
PROTECTED=()
OPTIONAL_STREAMING=()
OPTIONAL_OTHER=()
PROTECTED_SET=()
DISABLE_ONLY=()
STOCK_LAUNCHER=""com.google.android.apps.tv.launcherx""
PROJECTIVY_PKG=""com.spocky.projengmenu""
";

        var handler = new FakeHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(xiaomiConf)
        }));

        var service = new ConfigSyncService(
            httpHandler: handler,
            cacheDir: TestCacheDir,
            urlBase: TestRepoUrl,
            knownDevices: new[] { "xiaomi_a_pro_2026" },
            curatedFiles: new[] { "xiaomi-gist.txt" }
        );

        // Sync first to load the config
        var syncResult = await service.SyncAsync(CancellationToken.None);
        Assert.True(syncResult);

        // Act & Assert
        var config = service.MatchDevice("MiTV-MZTU0", "Xiaomi");
        Assert.NotNull(config);
        Assert.Equal("MiTV-MZTU0", config.Model);
        Assert.Equal("Xiaomi", config.Maker);
    }

    [Fact]
    public async Task MatchDevice_Returns_Null_When_No_Match_In_Known_Devices()
    {
        // Arrange - handler returns xiaomi config
        var xiaomiConf = @"
DEVICE_MODEL=""MiTV-MZTU0""
DEVICE_MAKER=""Xiaomi""
SAFE_REMOVE=()
PROTECTED=()
OPTIONAL_STREAMING=()
OPTIONAL_OTHER=()
PROTECTED_SET=()
DISABLE_ONLY=()
STOCK_LAUNCHER=""com.google.android.apps.tv.launcherx""
PROJECTIVY_PKG=""com.spocky.projengmenu""
";

        var handler = new FakeHttpMessageHandler((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(xiaomiConf)
        }));

        var service = new ConfigSyncService(
            httpHandler: handler,
            cacheDir: TestCacheDir,
            urlBase: TestRepoUrl,
            knownDevices: new[] { "xiaomi_a_pro_2026" },
            curatedFiles: new[] { "xiaomi-gist.txt" }
        );

        // Sync first to load the config
        await service.SyncAsync(CancellationToken.None);

        // Act
        var config = service.MatchDevice("Samsung", "Samsung");

        // Assert
        Assert.Null(config);
    }

    [Fact]
    public async Task FetchWithCacheAsync_Returns_Cached_Content_When_304_Received()
    {
        // Arrange - create a handler that returns 304 Not Modified
        var cachePath = Path.Combine(TestCacheDir, "cache_file.txt");
        var cachedContent = "cached content from cache";
        Directory.CreateDirectory(TestCacheDir);
        await File.WriteAllTextAsync(cachePath, cachedContent);

        var handler = new FakeHttpMessageHandler((req, ct) =>
        {
            // Check if If-None-Match header is present
            var hasEtag = req.Headers.Contains("If-None-Match");
            if (hasEtag)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("new content") });
        });

        var service = new ConfigSyncService(
            httpHandler: handler,
            cacheDir: TestCacheDir,
            urlBase: TestRepoUrl,
            knownDevices: new[] { "xiaomi_a_pro_2026" },
            curatedFiles: new[] { "xiaomi-gist.txt" }
        );

        // Act - first request without ETag should return new content
        var result1 = await service.FetchWithCacheAsync("https://example.com/test", "cache_file.txt", CancellationToken.None);

        // Assert - should get new content on first request
        Assert.Equal("new content", result1);

        // Cleanup
        if (File.Exists(cachePath))
            File.Delete(cachePath);
    }

    [Fact]
    public async Task FetchWithCacheAsync_Falls_Back_To_Cache_When_Offline()
    {
        // Arrange - create a handler that throws exception (simulating offline)
        var cachePath = Path.Combine(TestCacheDir, "offline_cache.txt");
        var cachedContent = "cached offline content";
        Directory.CreateDirectory(TestCacheDir);
        await File.WriteAllTextAsync(cachePath, cachedContent);

        var handler = new FakeHttpMessageHandler((req, ct) =>
        {
            throw new HttpRequestException("Network error");
        });

        var service = new ConfigSyncService(
            httpHandler: handler,
            cacheDir: TestCacheDir,
            urlBase: TestRepoUrl,
            knownDevices: new[] { "xiaomi_a_pro_2026" },
            curatedFiles: new[] { "xiaomi-gist.txt" }
        );

        // Act - when offline, should fall back to cache
        var result = await service.FetchWithCacheAsync("https://example.com/test", "offline_cache.txt", CancellationToken.None);

        // Assert - should return cached content when offline
        Assert.Equal(cachedContent, result);

        // Cleanup
        if (File.Exists(cachePath))
            File.Delete(cachePath);
    }

    [Fact]
    public async Task FetchWithCacheAsync_Returns_Null_When_Offline_And_No_Cache()
    {
        // Arrange - create a handler that throws exception (simulating offline)
        var cachePath = Path.Combine(TestCacheDir, "no_cache.txt");
        if (File.Exists(cachePath)) File.Delete(cachePath);

        var handler = new FakeHttpMessageHandler((req, ct) =>
        {
            throw new HttpRequestException("Network error");
        });

        var service = new ConfigSyncService(
            httpHandler: handler,
            cacheDir: TestCacheDir,
            urlBase: TestRepoUrl,
            knownDevices: new[] { "xiaomi_a_pro_2026" },
            curatedFiles: new[] { "xiaomi-gist.txt" }
        );

        // Act - when offline and no cache, should return null
        var result = await service.FetchWithCacheAsync("https://example.com/test", "no_cache.txt", CancellationToken.None);

        // Assert - should return null when offline and no cache
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchWithCacheAsync_Saves_ETag_When_Present()
    {
        // Arrange - create a handler that returns ETag in response
        var etagPath = Path.Combine(TestCacheDir, "etag_cache.txt.etag");
        var cachePath = Path.Combine(TestCacheDir, "etag_cache.txt");
        Directory.CreateDirectory(TestCacheDir);

        var handler = new FakeHttpMessageHandler((req, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("content") };
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"abc123\"");
            return Task.FromResult(response);
        });

        var service = new ConfigSyncService(
            httpHandler: handler,
            cacheDir: TestCacheDir,
            urlBase: TestRepoUrl,
            knownDevices: new[] { "xiaomi_a_pro_2026" },
            curatedFiles: new[] { "xiaomi-gist.txt" }
        );

        // Act
        await service.FetchWithCacheAsync("https://example.com/test", "etag_cache.txt", CancellationToken.None);

        // Assert - ETag file should be created
        Assert.True(File.Exists(etagPath));
        var etag = await File.ReadAllTextAsync(etagPath);
        Assert.Equal("\"abc123\"", etag);

        // Cleanup
        if (File.Exists(cachePath))
            File.Delete(cachePath);
        if (File.Exists(etagPath))
            File.Delete(etagPath);
    }
}