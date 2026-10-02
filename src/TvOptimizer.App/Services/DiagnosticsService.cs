using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace TvOptimizer.App.Services;

public record DiagnosticData(
    string MemoryInfo,
    string StorageInfo,
    string Temperature,
    string WifiInfo,
    string HdrInfo,
    string AudioInfo
);

public class DiagnosticsService
{
    private readonly TvSession _session = TvSession.Current;

    public async Task<DiagnosticData> GetDiagnosticsAsync(CancellationToken ct = default)
    {
        if (!_session.IsConnected) throw new InvalidOperationException("Не підключено до Android-пристрою");

        // CPU/RAM
        var memInfo = await _session.ShellAsync("cat /proc/meminfo", ct: ct);
        
        // Storage
        var storageInfo = await _session.ShellAsync("df -h /data", ct: ct);
        
        // SoC Temp
        var temp = await _session.ShellAsync("cat /sys/class/thermal/thermal_zone*/temp", ct: ct);
        
        // Wi-Fi
        var wifiInfo = await _session.ShellAsync("dumpsys wifi", ct: ct);
        
        // HDR
        var hdrInfo = await _session.ShellAsync("dumpsys display", ct: ct);
        
        // Audio
        var audioInfo = await _session.ShellAsync("dumpsys media.extractor", ct: ct);

        return new DiagnosticData(
            ParseMemInfo(memInfo),
            ParseStorageInfo(storageInfo),
            ParseTemperature(temp),
            ParseWifiInfo(wifiInfo),
            ParseHdrInfo(hdrInfo),
            ParseAudioInfo(audioInfo)
        );
    }

    private string ParseMemInfo(string raw)
    {
        // Simple extraction of MemTotal and MemFree
        return "RAM: (Parsed info)"; // Placeholder
    }

    private string ParseStorageInfo(string raw) => "Storage: (Parsed info)";
    private string ParseTemperature(string raw) => "Temp: (Parsed info)";
    private string ParseWifiInfo(string raw) => "Wi-Fi: (Parsed info)";
    private string ParseHdrInfo(string raw) => "HDR: (Parsed info)";
    private string ParseAudioInfo(string raw) => "Audio: (Parsed info)";
}
