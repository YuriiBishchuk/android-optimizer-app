using System.Collections.Generic;
using System.Linq;
using TvOptimizer.Core.Audit;
using TvOptimizer.Core.Config;
using TvOptimizer.Core.Safety;
using Xunit;

namespace TvOptimizer.Core.Tests;

public class AuditEngineTests
{
    private static readonly IReadOnlySet<string> EmptySet = new HashSet<string>();

    [Fact]
    public void AuditEngine_CategorizesSafePackage()
    {
        var installed = new HashSet<string>
        {
            "com.google.android.gms",
            "com.android.systemui",
        };

        var result = AuditEngine.Run(installed, null, EmptySet);

        Assert.Contains(result, r => r.PackageName == "com.google.android.gms" && r.Tier == Tier.Protected);
        Assert.Contains(result, r => r.PackageName == "com.android.systemui" && r.Tier == Tier.Protected);
    }

    [Fact]
    public void AuditEngine_GenericMode_UsesUniversalSets()
    {
        var installed = new HashSet<string>
        {
            "com.google.android.gms",
            "com.android.systemui",
            "com.google.android.tvlauncher",
            "com.google.android.tvrecommendations"
        };

        var result = AuditEngine.Run(installed, null, EmptySet);

        Assert.Contains(result, r => r.PackageName == "com.google.android.gms" && r.Tier == Tier.Protected);
        Assert.Contains(result, r => r.PackageName == "com.google.android.tvlauncher" && r.Tier == Tier.Protected);
        Assert.Contains(result, r => r.PackageName == "com.google.android.tvrecommendations" && r.Tier == Tier.Safe);
    }
}
