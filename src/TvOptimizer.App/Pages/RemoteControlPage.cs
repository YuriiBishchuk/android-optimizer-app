using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using TvOptimizer.App.Services;

namespace TvOptimizer.App.Pages;

public class RemoteControlPage : ContentPage
{
    private readonly Label _statusLabel;
    private readonly Border _statusBadge;

    public RemoteControlPage()
    {
        Title = "Пульт";
        BackgroundColor = Color.FromArgb("#0f172a");

        _statusLabel = new Label
        {
            Text = "Очікування команд",
            FontSize = 13,
            TextColor = Color.FromArgb("#94a3b8"),
            HorizontalOptions = LayoutOptions.Center
        };

        _statusBadge = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Stroke = Color.FromArgb("#334155"),
            BackgroundColor = Color.FromArgb("#1e293b"),
            Padding = new Thickness(14, 6),
            HorizontalOptions = LayoutOptions.Center,
            Content = _statusLabel
        };

        // --- 1. D-Pad Controller ---
        var dpadGrid = new Grid
        {
            WidthRequest = 230,
            HeightRequest = 230,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = new GridLength(1.1, GridUnitType.Star) },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        var btnUp = CreateDpadButton("▲", 19, "DPAD_UP");
        var btnDown = CreateDpadButton("▼", 20, "DPAD_DOWN");
        var btnLeft = CreateDpadButton("◀", 21, "DPAD_LEFT");
        var btnRight = CreateDpadButton("▶", 22, "DPAD_RIGHT");
        var btnCenter = CreateCenterOkButton();

        dpadGrid.Add(btnUp, 1, 0);
        dpadGrid.Add(btnLeft, 0, 1);
        dpadGrid.Add(btnCenter, 1, 1);
        dpadGrid.Add(btnRight, 2, 1);
        dpadGrid.Add(btnDown, 1, 2);

        var dpadCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(120) },
            Stroke = Color.FromArgb("#334155"),
            StrokeThickness = 2,
            BackgroundColor = Color.FromArgb("#1e293b"),
            WidthRequest = 250,
            HeightRequest = 250,
            HorizontalOptions = LayoutOptions.Center,
            Padding = 10,
            Content = dpadGrid
        };

        // --- 2. Navigation Row ---
        var navGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        navGrid.Add(CreatePillButton("↩ Назад", 4, "BACK", "#334155"), 0, 0);
        navGrid.Add(CreatePillButton("⌂ Додому", 3, "HOME", "#334155"), 1, 0);
        navGrid.Add(CreatePillButton("☰ Меню", 82, "MENU", "#334155"), 2, 0);

        // --- 3. Volume & Media Row ---
        var volumeGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        volumeGrid.Add(CreatePillButton("🔉 Vol -", 25, "VOLUME_DOWN", "#1e293b", "#64748b"), 0, 0);
        volumeGrid.Add(CreatePillButton("🔇 Mute", 164, "MUTE", "#1e293b", "#64748b"), 1, 0);
        volumeGrid.Add(CreatePillButton("🔊 Vol +", 24, "VOLUME_UP", "#1e293b", "#64748b"), 2, 0);

        // --- 4. System / Reboot Row ---
        var powerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        powerGrid.Add(CreatePillButton("🔄 Перезавантажити", 0, "reboot", "#7f1d1d", isCommand: true), 0, 0);
        powerGrid.Add(CreatePillButton("⚡ В Recovery", 0, "reboot recovery", "#854d0e", isCommand: true), 1, 0);

        var layout = new VerticalStackLayout
        {
            Padding = new Thickness(16, 20),
            Spacing = 20,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                new Label
                {
                    Text = "🎮 Пульт дистанційного керування",
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#f8fafc"),
                    HorizontalOptions = LayoutOptions.Center
                },
                _statusBadge,
                dpadCard,
                CreateCardSection("Навігація", navGrid),
                CreateCardSection("Гучність", volumeGrid),
                CreateCardSection("Система", powerGrid)
            }
        };

        Content = new ScrollView
        {
            Content = layout,
            BackgroundColor = Color.FromArgb("#0f172a")
        };
    }

    private static Border CreateCardSection(string title, View content)
    {
        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Stroke = Color.FromArgb("#334155"),
            BackgroundColor = Color.FromArgb("#1e293b"),
            Padding = new Thickness(14, 12),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = title, FontSize = 12, TextColor = Color.FromArgb("#94a3b8"), FontAttributes = FontAttributes.Bold },
                    content
                }
            }
        };
    }

    private Button CreateDpadButton(string symbol, int keycode, string label)
    {
        var btn = new Button
        {
            Text = symbol,
            FontSize = 20,
            TextColor = Color.FromArgb("#f8fafc"),
            BackgroundColor = Color.FromArgb("#334155"),
            CornerRadius = 16,
            Padding = 0,
            WidthRequest = 62,
            HeightRequest = 62,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        btn.Clicked += async (s, e) => await SendKey(keycode, label);
        return btn;
    }

    private Button CreateCenterOkButton()
    {
        var btn = new Button
        {
            Text = "OK",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#2563eb"),
            CornerRadius = 35,
            Padding = 0,
            WidthRequest = 70,
            HeightRequest = 70,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        btn.Clicked += async (s, e) => await SendKey(23, "DPAD_CENTER");
        return btn;
    }

    private Button CreatePillButton(string text, int keycode, string label, string bgColor, string? strokeColor = null, bool isCommand = false)
    {
        var btn = new Button
        {
            Text = text,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb(bgColor),
            CornerRadius = 10,
            HeightRequest = 46,
            Padding = new Thickness(4)
        };

        btn.Clicked += async (s, e) =>
        {
            if (isCommand)
            {
                await SendShell(label);
            }
            else
            {
                await SendKey(keycode, label);
            }
        };
        return btn;
    }

    private async Task SendKey(int keycode, string label)
    {
        if (!TvSession.Current.IsConnected)
        {
            await DisplayAlert("Помилка", "Спочатку підключіться до пристрою на вкладці «Підключення»", "OK");
            return;
        }

        try
        {
            await TvSession.Current.ShellAsync($"input keyevent {keycode}");
            _statusLabel.Text = $"Натиснуто: {label}";
            _statusLabel.TextColor = Color.FromArgb("#38bdf8");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Помилка: {ex.Message}";
            _statusLabel.TextColor = Color.FromArgb("#f87171");
        }
    }

    private async Task SendShell(string command)
    {
        if (!TvSession.Current.IsConnected)
        {
            await DisplayAlert("Помилка", "Спочатку підключіться до пристрою на вкладці «Підключення»", "OK");
            return;
        }

        try
        {
            await TvSession.Current.ShellAsync(command);
            _statusLabel.Text = $"Виконано: {command}";
            _statusLabel.TextColor = Color.FromArgb("#4ade80");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Помилка: {ex.Message}";
            _statusLabel.TextColor = Color.FromArgb("#f87171");
        }
    }
}