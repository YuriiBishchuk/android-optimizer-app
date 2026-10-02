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

        var connectPage = new ConnectPage
        {
            Title = "🔌 Підключення",
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        var auditPage = new AuditPage
        {
            Title = "🛡️ Аудит",
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        var tweaksPage = new TweaksPage
        {
            Title = "⚡ Твіки",
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        var remotePage = new RemoteControlPage
        {
            Title = "🎮 Пульт",
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        var diagPage = new DiagnosticsPage
        {
            Title = "📊 Діагностика",
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        tabbedPage.Children.Add(connectPage);
        tabbedPage.Children.Add(auditPage);
        tabbedPage.Children.Add(tweaksPage);
        tabbedPage.Children.Add(remotePage);
        tabbedPage.Children.Add(diagPage);

        return new Window(tabbedPage);
    }
}
