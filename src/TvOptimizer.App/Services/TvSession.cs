using TvOptimizer.Transport.Adb;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TvOptimizer.App.Services;

/// <summary>Сесія до одного ТВ: послідовні shell-виклики через SemaphoreSlim.</summary>
public sealed class TvSession
{
    public static TvSession Current { get; } = new();

    private AdbClient? _client;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsConnected => _client is not null;
    public string? DeviceModel { get; private set; }

    public async Task<string> ConnectAsync(string host, int port, bool tls,
        CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_client is not null) await _client.DisposeAsync();
            _client = new AdbClient(host, port, tls);
            await _client.ConnectAsync(ct);
            var props = await _client.ShellAsync(
                "getprop ro.product.model; getprop ro.product.brand; getprop ro.build.version.sdk",
                ct: ct);
            DeviceModel = props.Replace("\r", "").Replace("\n", " / ").Trim();
            return DeviceModel;
        }
        catch
        {
            if (_client is not null) await _client.DisposeAsync();
            _client = null;
            throw;
        }
        finally { _gate.Release(); }
    }

    public async Task<string> ShellAsync(string command, int timeoutSec = 25,
        CancellationToken ct = default)
    {
        if (_client is null) throw new InvalidOperationException("Not connected");
        await _gate.WaitAsync(ct);
        try { return await _client.ShellAsync(command, timeoutSec, ct); }
        finally { _gate.Release(); }
    }

    public async Task DisconnectAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_client is not null) await _client.DisposeAsync();
            _client = null;
        }
        finally { _gate.Release(); }
    }

    // New methods for tweaks
    public async Task<List<TweakItem>> GetTweaksAsync()
    {
        // For now, return an empty list. We can implement real tweak reading later.
        return new List<TweakItem>();
    }

    public async Task ExecuteCommandAsync(string command)
    {
        await ShellAsync(command);
    }
}