using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using TvOptimizer.App.Pages;

namespace TvOptimizer.App;

public class App : Application
{
    public App()
    {
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var tabbedPage = new TabbedPage
        {
            Title = "Android TV Optimizer",
            BarBackgroundColor = Color.FromArgb("#0f172a"),
            BarTextColor = Colors.White,
            SelectedTabColor = Color.FromArgb("#38bdf8"),
            UnselectedTabColor = Color.FromArgb("#64748b"),
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        var connectNav = new NavigationPage(new ConnectPage())
        {
            Title = "🔌 Підключення",
            BarBackgroundColor = Color.FromArgb("#0f172a"),
            BarTextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        var auditNav = new NavigationPage(new AuditPage())
        {
            Title = "🛡️ Аудит",
            BarBackgroundColor = Color.FromArgb("#0f172a"),
            BarTextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        var tweaksNav = new NavigationPage(new TweaksPage())
        {
            Title = "⚡ Твіки",
            BarBackgroundColor = Color.FromArgb("#0f172a"),
            BarTextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        var remoteNav = new NavigationPage(new RemoteControlPage())
        {
            Title = "🎮 Пульт",
            BarBackgroundColor = Color.FromArgb("#0f172a"),
            BarTextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        var diagNav = new NavigationPage(new DiagnosticsPage())
        {
            Title = "📊 Діагностика",
            BarBackgroundColor = Color.FromArgb("#0f172a"),
            BarTextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        tabbedPage.Children.Add(connectNav);
        tabbedPage.Children.Add(auditNav);
        tabbedPage.Children.Add(tweaksNav);
        tabbedPage.Children.Add(remoteNav);
        tabbedPage.Children.Add(diagNav);

        return new Window(tabbedPage);
    }
}
