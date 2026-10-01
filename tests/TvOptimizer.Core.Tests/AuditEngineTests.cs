using System.Collections.Generic;
using System.Linq;
using TvOptimizer.Core.Audit;
using TvOptimizer.Core.Config;
using TvOptimizer.Core.Safety;
using Xunit;

namespace TvOptimizer.Core.Tests;

public class AuditEngineTests
{
    private static IReadOnlySet<string> EmptySet = new HashSet<string>();

    [Fact]
    public void AuditEngine_CategorizesSafePackage()
    {
        var installed = new HashSet<string>
        {
            "com.google.android.gms",
            "com.android.systemui",
        };

        var result = AuditEngine.Run(installed, null, EmptySet);

        Assert.Contains("com.google.android.gms", result.ProtectedPresent);
        Assert.Contains("com.android.systemui", result.ProtectedPresent);
    }

    [Fact]
    public void AuditEngine_GenericMode_UsesUniversalSets()
    {
        var installed = new HashSet<string>
        {
            "com.google.android.gms",
            "com.android.systemui",
        };

        var result = AuditEngine.Run(installed, null, EmptySet);

        Assert.True(result.GenericMode);
        Assert.Contains("com.google.android.gms", result.ProtectedPresent);
        Assert.Contains("com.android.systemui", result.ProtectedPresent);
    }
}