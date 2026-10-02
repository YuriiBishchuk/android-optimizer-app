using Android.App;
using Android.Content;
using Android.Content.PM;

namespace TvOptimizer.App;

[Activity(
    Label = "Android Optimizer",
    Theme = "@style/Maui.SplashTheme", 
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    LaunchMode = LaunchMode.SingleTop, 
    Icon = "@mipmap/appicon",
    RoundIcon = "@mipmap/appicon_round",
    Banner = "@drawable/banner",
    Exported = true)]
[IntentFilter(new[] { Intent.ActionMain },
    Categories = new[] { "android.intent.category.LEANBACK_LAUNCHER" })]
public class MainActivity : MauiAppCompatActivity
{
}