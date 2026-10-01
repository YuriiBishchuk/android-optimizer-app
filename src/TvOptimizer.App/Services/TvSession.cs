using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TvOptimizer.Transport.Adb;

namespace TvOptimizer.App.Services;

public enum TvConnectionState
{
    Idle,
    Pairing,
    Connecting,
    Connected,
    Error
}

/// <summary>Сесія до одного ТВ: послідовні shell-виклики через SemaphoreSlim.</summary>
public sealed class TvSession
{
    public static TvSession Current { get; } = new();

    private AdbClient? _client;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsConnected => _client is not null && State == TvConnectionState.Connected;
    public TvConnectionState State { get; private set; } = TvConnectionState.Idle;
    public string? LastError { get; private set; }

    public string? DeviceModel { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? AndroidVersion { get; private set; }
    public string? SdkVersion { get; private set; }

    public async Task<bool> PairAsync(string host, int pairingPort, string pairingCode, CancellationToken ct = default)
    {
        State = TvConnectionState.Pairing;
        LastError = null;
        await _gate.WaitAsync(ct);
        try
        {
            await using var pairClient = new AdbClient(host, pairingPort, tls: true);
            await Task.Run(async () => await pairClient.PairAsync(ct), ct);
            State = TvConnectionState.Idle;
            return true;
        }
        catch (Exception ex)
        {
            State = TvConnectionState.Error;
            LastError = $"Помилка створення пари: {ex.Message}";
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> ConnectAsync(string host, int port, bool tls, CancellationToken ct = default)
    {
        State = TvConnectionState.Connecting;
        LastError = null;
        await _gate.WaitAsync(ct);
        try
        {
            if (_client is not null) await _client.DisposeAsync();
            _client = new AdbClient(host, port, tls);
            
            await Task.Run(async () => await _client.ConnectAsync(ct), ct);

            var model = await _client.ShellAsync("getprop ro.product.model", ct: ct);
            var manufacturer = await _client.ShellAsync("getprop ro.product.manufacturer", ct: ct);
            var version = await _client.ShellAsync("getprop ro.build.version.release", ct: ct);
            var sdk = await _client.ShellAsync("getprop ro.build.version.sdk", ct: ct);

            DeviceModel = model.Trim();
            Manufacturer = manufacturer.Trim();
            AndroidVersion = version.Trim();
            SdkVersion = sdk.Trim();

            State = TvConnectionState.Connected;
            return $"{Manufacturer} {DeviceModel} (Android {AndroidVersion}, SDK {SdkVersion})";
        }
        catch (Exception ex)
        {
            if (_client is not null) await _client.DisposeAsync();
            _client = null;
            State = TvConnectionState.Error;
            LastError = $"Помилка підключення: {ex.Message}";
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> ShellAsync(string command, int timeoutSec = 25, CancellationToken ct = default)
    {
        if (_client is null) throw new InvalidOperationException("Not connected");
        await _gate.WaitAsync(ct);
        try
        {
            return await Task.Run(async () => await _client.ShellAsync(command, timeoutSec, ct), ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_client is not null) await _client.DisposeAsync();
            _client = null;
            State = TvConnectionState.Idle;
            LastError = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    }