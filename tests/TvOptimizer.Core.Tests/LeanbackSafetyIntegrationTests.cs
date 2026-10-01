using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TvOptimizer.Core.Audit;
using TvOptimizer.Core.Config;
using TvOptimizer.Core.Safety;
using TvOptimizer.Transport.Adb;
using Xunit;

namespace TvOptimizer.Core.Tests;

public class LeanbackSafetyIntegrationTests
{
    [Fact]
    public async Task AndroidTv_SafetyGuard_ProtectsCoreLaunchersAndSystem()
    {
        var installedPackages = new HashSet<string>(StringComparer.Ordinal);
        
        try
        {
            await using var client = new AdbClient("127.0.0.1", 5555, tls: false);
            await client.ConnectAsync();
            var output = await client.ShellAsync("pm list packages");
            var lines = output.Split(new char[] { (char)13, (char)10 }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var pkg = line.Trim();
                if (pkg.StartsWith("package:")) pkg = pkg.Substring("package:".Length).Trim();
                if (!string.IsNullOrEmpty(pkg)) installedPackages.Add(pkg);
            }
        }
        catch
        {
            // Fallback test fixtures if emulator offline
            installedPackages = new HashSet<string>(StringComparer.Ordinal)
            {
                "android",
                "com.android.systemui",
                "com.google.android.tvlauncher",
                "com.google.android.apps.tv.launcherx",
                "com.google.android.leanbacklauncher",
                "com.google.android.gms",
                "com.google.android.videos",
                "com.google.android.feedback"
            };
        }

        // Audit against generic mode
        var audit = AuditEngine.Run(installedPackages, null, new HashSet<string>());

        // Safety Invariants Assertion
        var protectedPkgs = audit.Where(r => r.Tier == Tier.Protected).Select(r => r.PackageName).ToHashSet();
        
        // Assert System UI and Core Launchers NEVER classified as safe
        Assert.True(protectedPkgs.Contains("android") || !installedPackages.Contains("android"));
        Assert.True(protectedPkgs.Contains("com.android.systemui") || !installedPackages.Contains("com.android.systemui"));
        
        if (installedPackages.Contains("com.google.android.tvlauncher"))
        {
            Assert.Contains("com.google.android.tvlauncher", protectedPkgs);
            Assert.False(Guard.CanRemove("com.google.android.tvlauncher", Guard.UniversalProtected, Guard.DefaultStockLauncher));
        }

        if (installedPackages.Contains("com.google.android.apps.tv.launcherx"))
        {
            Assert.Contains("com.google.android.apps.tv.launcherx", protectedPkgs);
            Assert.False(Guard.CanRemove("com.google.android.apps.tv.launcherx", Guard.UniversalProtected, Guard.DefaultStockLauncher));
        }

        // Batch filtering verification
        var (allowed, blocked) = Guard.FilterBatch(installedPackages, Guard.UniversalProtected, Guard.DefaultStockLauncher);
        Assert.DoesNotContain("com.android.systemui", allowed);
        Assert.DoesNotContain("com.google.android.tvlauncher", allowed);
        Assert.DoesNotContain("com.google.android.apps.tv.launcherx", allowed);
    }
}
