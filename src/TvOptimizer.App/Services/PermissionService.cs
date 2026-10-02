using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TvOptimizer.App.Services;

public class PermissionService
{
    public static PermissionService Current { get; } = new();
    private readonly TvSession _session = TvSession.Current;

    private static readonly Dictionary<string, string> PackageNames = new()
    {
        { "Button Mapper", "flar2.homebutton" },
        { "Projectivy Launcher", "com.spocky.projengmenu" },
        { "Shizuku", "moe.shizuku.privileged.api" },
        { "Tasker", "net.dinglisch.android.taskerm" },
        { "Macrodroid", "com.arlosoft.macrodroid" }
    };

    public async Task<Dictionary<string, bool>> CheckInstalledUtilitiesAsync(CancellationToken ct = default)
    {
        var results = new Dictionary<string, bool>();
        foreach (var (name, packageName) in PackageNames)
        {
            results[name] = await IsPackageInstalledAsync(packageName, ct);
        }
        return results;
    }

    private async Task<bool> IsPackageInstalledAsync(string packageName, CancellationToken ct = default)
    {
        var output = await _session.ShellAsync($"pm list packages {packageName}", ct: ct);
        return output.Contains($"package:{packageName}");
    }

    public async Task GrantPermissionsAsync(string packageName, CancellationToken ct = default)
    {
        // Try using Shizuku to grant permissions via appops
        // WRITE_SECURE_SETTINGS
        await _session.ShellAsync($"shizuku shell appops set {packageName} WRITE_SECURE_SETTINGS allow", ct: ct);
        // SYSTEM_ALERT_WINDOW
        await _session.ShellAsync($"shizuku shell appops set {packageName} SYSTEM_ALERT_WINDOW allow", ct: ct);
        // PACKAGE_USAGE_STATS
        await _session.ShellAsync($"shizuku shell appops set {packageName} GET_USAGE_STATS allow", ct: ct);
    }

    public async Task LaunchShizukuAsync(CancellationToken ct = default)
    {
        await _session.ShellAsync("sh /sdcard/Android/data/moe.shizuku.privileged.api/start.sh", ct: ct);
    }
}
