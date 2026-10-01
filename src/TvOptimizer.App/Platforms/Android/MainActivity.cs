using Android.App;
using Android.Content;
using Android.Content.PM;

namespace TvOptimizer.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionMain },
    Categories = new[] { "android.intent.category.LAUNCHER" })]
public class MainActivity : MauiAppCompatActivity
{
}