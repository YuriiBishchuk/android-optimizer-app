using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using TvOptimizer.Core.Audit;
using TvOptimizer.Core.Safety;
using TvOptimizer.App.Services;

namespace TvOptimizer.App.Pages;

public class AuditPage : ContentPage
{
    private readonly Label _statusLabel;
    private readonly Button _refreshBtn;
    private readonly Button _applySafeBtn;
    private readonly Button _rollbackBtn;
    private readonly CollectionView _resultsView;
    private readonly List<string> _disabledHistory = new();

    public AuditPage()
    {
        Title = "Аудит";
        BackgroundColor = Color.FromArgb("#0f172a");
        Padding = new Thickness(16);

        _statusLabel = new Label
        {
            Text = "Підключіться до пристрою для аналізу пакетів",
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            TextColor = Color.FromArgb("#94a3b8")
        };

        _refreshBtn = new Button
        {
            Text = "🔄 Просканувати пакети",
            BackgroundColor = Color.FromArgb("#2563eb"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 44
        };
        _refreshBtn.Clicked += async (s, e) => await RefreshAuditAsync();

        _applySafeBtn = new Button
        {
            Text = "🛡️ TIER_1 (Безпечно)",
            BackgroundColor = Color.FromArgb("#16a34a"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 44,
            IsEnabled = false
        };
        _applySafeBtn.Clicked += async (s, e) => await OnApplySafeClicked();

        _rollbackBtn = new Button
        {
            Text = "↩️ Відкат",
            BackgroundColor = Color.FromArgb("#d97706"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 44,
            IsEnabled = false
        };
        _rollbackBtn.Clicked += async (s, e) => await OnRollbackClicked();

        _resultsView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(typeof(AuditResultCell)),
            BackgroundColor = Colors.Transparent
        };

        var actionsGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            RowSpacing = 8,
            ColumnSpacing = 8
        };
        actionsGrid.Add(_refreshBtn, 0, 0);
        Grid.SetColumnSpan(_refreshBtn, 2);
        actionsGrid.Add(_applySafeBtn, 0, 1);
        actionsGrid.Add(_rollbackBtn, 1, 1);

        var statusCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
            Stroke = Color.FromArgb("#334155"),
            BackgroundColor = Color.FromArgb("#1e293b"),
            Padding = new Thickness(14),
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    _statusLabel,
                    actionsGrid
                }
            }
        };

        var headerLabel = new Label
        {
            Text = "Класифіковані пакети:",
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = Color.FromArgb("#f8fafc")
        };

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            RowSpacing = 12,
            BackgroundColor = Color.FromArgb("#0f172a")
        };

        grid.Add(statusCard, 0, 0);
        grid.Add(headerLabel, 0, 1);
        grid.Add(_resultsView, 0, 2);

        Content = grid;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (TvSession.Current.IsConnected)
        {
            await RefreshAuditAsync();
        }
    }

    private async Task RefreshAuditAsync()
    {
        if (!TvSession.Current.IsConnected)
        {
            await DisplayAlert("Помилка", "Спочатку підключіться до пристрою на вкладці Підключення", "OK");
            return;
        }

        _statusLabel.Text = "⏳ Сканування пакетів та аналіз правил безпеки...";
        _statusLabel.TextColor = Color.FromArgb("#fbbf24");
        _refreshBtn.IsEnabled = false;
        _applySafeBtn.IsEnabled = false;

        try
        {
            var raw = await TvSession.Current.ShellAsync("pm list packages -u");
            var packages = raw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.StartsWith("package:"))
                .Select(line => line.Substring("package:".Length).Trim())
                .Where(pkg => !string.IsNullOrWhiteSpace(pkg))
                .ToHashSet();

            var deviceConfig = ConfigSync.Current.LastDevice;
            var curatedTier2 = ConfigSync.Current.CuratedTier2;

            var results = AuditEngine.Run(packages, deviceConfig, curatedTier2, ConfigSync.Current.UadApps)
                .OrderBy(r => GetTierSortOrder(r.Tier))
                .ThenBy(r => r.PackageName)
                .ToList();

            _resultsView.ItemsSource = results;

            var safeCount = results.Count(r => r.Tier == Tier.Safe);
            var reviewCount = results.Count(r => r.Tier == Tier.Review);
            var protCount = results.Count(r => r.Tier == Tier.Protected);

            _statusLabel.Text = $"Всього: {results.Count} | Safe Bloat (TIER_1): {safeCount} | Опційні (TIER_2): {reviewCount} | Захищені: {protCount}";
            _statusLabel.TextColor = Color.FromArgb("#4ade80");
            _applySafeBtn.IsEnabled = safeCount > 0;
            _rollbackBtn.IsEnabled = _disabledHistory.Count > 0;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Помилка сканування: {ex.Message}";
            _statusLabel.TextColor = Color.FromArgb("#f87171");
            await DisplayAlert("Помилка аудиту", ex.Message, "OK");
        }
        finally
        {
            _refreshBtn.IsEnabled = true;
        }
    }

    private static int GetTierSortOrder(Tier tier) => tier switch
    {
        Tier.Safe => 0,
        Tier.Review => 1,
        Tier.Heuristic => 2,
        Tier.Unidentified => 3,
        Tier.Protected => 4,
        _ => 5
    };

    private async Task OnApplySafeClicked()
    {
        if (!TvSession.Current.IsConnected) return;

        var results = (_resultsView.ItemsSource as IEnumerable<AuditResult>)?.ToList();
        if (results == null) return;

        var safeList = results.Where(r => r.Tier == Tier.Safe).ToList();
        if (safeList.Count == 0) return;

        bool confirm = await DisplayAlert(
            "Підтвердження оптимізації",
            $"Буде безпечно вимкнено {safeList.Count} пакетів (TIER_1 Universal Safe Bloat). Системні лаунчери та сервіси захищені Safety Guard. Продовжити?",
            "Так, вимкнути", "Скасувати");

        if (!confirm) return;

        _applySafeBtn.IsEnabled = false;
        _statusLabel.Text = "⏳ Вимикання безпечних блоатваре-пакетів...";
        _statusLabel.TextColor = Color.FromArgb("#fbbf24");

        int successCount = 0;
        foreach (var item in safeList)
        {
            try
            {
                if (Guard.IsProtected(item.PackageName)) continue;
                await TvSession.Current.ShellAsync($"pm disable-user --user 0 {item.PackageName}");
                _disabledHistory.Add(item.PackageName);
                successCount++;
            }
            catch
            {
                // continue with other packages
            }
        }

        _statusLabel.Text = $"✅ Успішно вимкнено {successCount} пакетів";
        _statusLabel.TextColor = Color.FromArgb("#4ade80");
        _rollbackBtn.IsEnabled = _disabledHistory.Count > 0;
        await DisplayAlert("Готово", $"Оптимізація завершена. Вимкнено пакетів: {successCount}. Відкат доступний у будь-який момент.", "OK");
        await RefreshAuditAsync();
    }

    private async Task OnRollbackClicked()
    {
        if (!TvSession.Current.IsConnected || _disabledHistory.Count == 0) return;

        bool confirm = await DisplayAlert(
            "Відкат змін",
            $"Увімкнути назад {_disabledHistory.Count} раніше вимкнених пакетів?",
            "Так, відновити", "Скасувати");

        if (!confirm) return;

        _rollbackBtn.IsEnabled = false;
        _statusLabel.Text = "⏳ Відновлення пакетів...";

        int restored = 0;
        foreach (var pkg in _disabledHistory.ToList())
        {
            try
            {
                await TvSession.Current.ShellAsync($"pm enable {pkg}");
                _disabledHistory.Remove(pkg);
                restored++;
            }
            catch
            {
                // ignore
            }
        }

        _statusLabel.Text = $"✅ Відновлено пакетів: {restored}";
        _statusLabel.TextColor = Color.FromArgb("#4ade80");
        await DisplayAlert("Відкат завершено", $"Відновлено {restored} пакетів.", "OK");
        await RefreshAuditAsync();
    }
}
