using System;
using Xunit;
using TvOptimizer.Core.Config;

namespace TvOptimizer.Core.Tests;

public class ConfigParserTests
{
    private static readonly string XiaomiConf = @"
# Xiaomi TV A Pro 2026 — MiTV-MZTU0 (river), MT9676, Android TV 14 (SDK 34)
# Статус: протестовано 2026-09-27. RAM 54 -> 259 МБ (free), Projectivy default.
# ADB-порт динамічний (Wireless Debugging): напр. TV_IP=192.168.0.103:5555
DEVICE_MODEL=""MiTV-MZTU0""
DEVICE_MAKER=""Xiaomi""
ANDROID_SDK=""34""
SAFE_REMOVE=(
  ""com.xiaomi.statistic""
  ""com.xiaomo.tv.milegal""
  ""com.xiamobile.webcontent""
  ""com.xiaomi.android.tvsetup.partnercustomizer""
  ""com.mitv.tvhome.mitvplus""
  ""com.mitv.tvhome.michannel""
  ""com.mitv.tvhome.oemtab""
  ""com.mitv.toolhouse""
  ""com.xiaomi.mitv.tvmanager""
)
PROTECTED=(
  ""com.google.android.apps.tv.launcherx""
)
OPTIONAL_STREAMING=(
  ""com.xiaomi.statistic""
  ""com.xiaomo.tv.milegal""
  ""com.xiamobile.webcontent""
)
OPTIONAL_OTHER=(
  ""com.google.android.gms.feed""
  ""com.google.android.feedback""
  ""com.android.nearby.halfsheet""
)
PROTECTED_SET=(
  ""com.google.android.apps.tv.launcherx""
)
DISABLE_ONLY=(
  ""com.google.android.feedback""
)
STOCK_LAUNCHER=""com.google.android.apps.tv.launcherx""
PROJECTIVY_PKG=""com.spocky.projengmenu""
";

    [Fact]
    public void ParseDeviceConf_Xiaomi_Should_Parse_All_Fields()
    {
        var config = ConfigParser.ParseDeviceConf("xiaomi_a_pro_2026", XiaomiConf);

        Assert.Equal("MiTV-MZTU0", config.Model);
        Assert.Equal("Xiaomi", config.Maker);
        Assert.Contains("com.xiaomi.statistic", config.SafeRemoveSet);
        Assert.Contains("com.xiaomo.tv.milegal", config.SafeRemoveSet);
        Assert.Contains("com.xiaomi.android.tvsetup.partnercustomizer", config.SafeRemoveSet);
        Assert.Contains("com.mitv.tvhome.mitvplus", config.SafeRemoveSet);
        Assert.Contains("com.mitv.tvhome.michannel", config.SafeRemoveSet);
        Assert.Contains("com.mitv.tvhome.oemtab", config.SafeRemoveSet);
        Assert.Contains("com.mitv.toolhouse", config.SafeRemoveSet);
        Assert.Contains("com.xiaomi.mitv.tvmanager", config.SafeRemoveSet);
        Assert.Contains("com.google.android.apps.tv.launcherx", config.ProtectedSet);
        Assert.Contains("com.google.android.gms.feed", config.OptionalOtherSet);
        Assert.Contains("com.google.android.feedback", config.OptionalOtherSet);
        Assert.Contains("com.android.nearby.halfsheet", config.OptionalOtherSet);
        Assert.Contains("com.google.android.feedback", config.DisableOnlySet);
        Assert.Equal("com.google.android.apps.tv.launcherx", config.StockLauncher);
        Assert.Equal("com.spocky.projengmenu", config.ProjectivyPkg);
    }

    [Fact]
    public void ParseDeviceConf_Xiaomi_Should_Parse_Correct_Counts()
    {
        var config = ConfigParser.ParseDeviceConf("xiaomi_a_pro_2026", XiaomiConf);

        Assert.Equal(9, config.SafeRemoveSet.Count);
        Assert.Equal(1, config.ProtectedSet.Count);
        Assert.Equal(3, config.OptionalStreamingSet.Count);
        Assert.Equal(3, config.OptionalOtherSet.Count);
        Assert.Equal(1, config.DisableOnlySet.Count);
    }

    [Fact]
    public void ParseDeviceConf_Empty_Should_Return_Defaults()
    {
        var config = ConfigParser.ParseDeviceConf("empty", "");

        Assert.Equal("", config.Model);
        Assert.Equal("", config.Maker);
        Assert.Empty(config.SafeRemoveSet);
        Assert.Equal("com.google.android.apps.tv.launcherx", config.StockLauncher);
        Assert.Equal("com.spocky.projengmenu", config.ProjectivyPkg);
    }

    [Fact]
    public void ParseCurated_Should_Parse_Package_List()
    {
        var curatedContent = @"
# This is a comment
com.google.android.gms
com.google.android.feedback
com.android.nearby.halfsheet

";
        var result = ConfigParser.ParseCurated(curatedContent);

        Assert.Equal(3, result.Count);
        Assert.Contains("com.google.android.gms", result);
        Assert.Contains("com.google.android.feedback", result);
        Assert.Contains("com.android.nearby.halfsheet", result);
    }

    [Fact]
    public void ParseCurated_Empty_Should_Return_Empty_Set()
    {
        var result = ConfigParser.ParseCurated("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseCurated_With_Comments_Should_Skip_Comment_Only_Lines()
    {
        var curatedContent = @"
# Only a comment
# Another comment
com.google.android.gms
";
        var result = ConfigParser.ParseCurated(curatedContent);

        Assert.Equal(1, result.Count);
        Assert.Contains("com.google.android.gms", result);
    }
}