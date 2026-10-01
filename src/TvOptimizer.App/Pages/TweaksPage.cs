using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using TvOptimizer.App.Services;
using TvOptimizer.Core.Tweaks;

namespace TvOptimizer.App.Pages;

public class TweaksPage : ContentPage
{
    private readonly Label _statusLabel;
    private readonly Label _currentScaleLabel;
    private readonly Label _currentBgLabel;
    private readonly Button _refreshBtn;
    private readonly Button _applyGuestPresetBtn;
    private readonly Button _animFastBtn;
    private readonly Button _animOffBtn;
    private readonly Button _animStockBtn;
    private readonly Button _dozeBtn;
    private readonly Button _screensaverBtn;

    public TweaksPage()
    {
        Title = "Твіки Швидкодії";
        Padding = new Thickness(16);

        _statusLabel = new Label
        {
            Text = "Твіки оптимізації реактивності інтерфейсу",
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            TextColor = Colors.SteelBlue
        };

        _currentScaleLabel = new Label
        {
            Text = "Масштаб анімацій: не визначено",
            FontSize = 13,
            TextColor = Color.FromArgb("#334155")
        };

        _currentBgLabel = new Label
        {
            Text = "Фонові процеси: не визначено",
            FontSize = 13,
            TextColor = Color.FromArgb("#334155")
        };

        _refreshBtn = new Button
        {
            Text = "🔄 Зчитати поточний стан",
            BackgroundColor = Color.FromArgb("#2563eb"),
            TextColor = Colors.White,
            CornerRadius = 8
        };
        _refreshBtn.Clicked += async (s, e) => await ReadCurrentStateAsync();

        _applyGuestPresetBtn = new Button
        {
            Text = "⚡ Пресет «Швидкий ТВ» (0.5x + Doze + Bg 4)",
            BackgroundColor = Color.FromArgb("#16a34a"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8
        };
        _applyGuestPresetBtn.Clicked += async (s, e) => await ApplyGuestPresetAsync();

        _animFastBtn = new Button
        {
            Text = "Швидкі анімації (0.5x)",
            BackgroundColor = Color.FromArgb("#0284c7"),
            TextColor = Colors.White,
            CornerRadius = 8
        };
        _animFastBtn.Clicked += async (s, e) => await ApplyAnimationAsync(AnimSpeed.Fast05);

        _animOffBtn = new Button
        {
            Text = "Вимкнути анімації (0.0x)",
            BackgroundColor = Color.FromArgb("#475569"),
            TextColor = Colors.White,
            CornerRadius = 8
        };
        _animOffBtn.Clicked += async (s, e) => await ApplyAnimationAsync(AnimSpeed.Off);

        _animStockBtn = new Button
        {
            Text = "Стандартні анімації (1.0x)",
            BackgroundColor = Color.FromArgb("#64748b"),
            TextColor = Colors.White,
            CornerRadius = 8
        };
        _animStockBtn.Clicked += async (s, e) => await ApplyAnimationAsync(AnimSpeed.Stock1x);

        _dozeBtn = new Button
        {
            Text = "Увімкнути Doze (економія в простої)",
            BackgroundColor = Color.FromArgb("#0d9488"),
            TextColor = Colors.White,
            CornerRadius = 8
        };
        _dozeBtn.Clicked += async (s, e) => await ApplyCommandAsync(TweaksEngine.DozeOn());

        _screensaverBtn = new Button
        {
            Text = "Вимкнути скрінсейвер",
            BackgroundColor = Color.FromArgb("#d97706"),
            TextColor = Colors.White,
            CornerRadius = 8
        };
        _screensaverBtn.Clicked += async (s, e) => await ApplyCommandAsync(TweaksEngine.ScreensaverOff());

        var statusCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
            Stroke = Color.FromArgb("#cbd5e1"),
            Padding = new Thickness(14),
            BackgroundColor = Color.FromArgb("#f1f5f9"),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children = { _statusLabel, _currentScaleLabel, _currentBgLabel, _refreshBtn }
            }
        };

        var quickCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
            Stroke = Color.FromArgb("#cbd5e1"),
            Padding = new Thickness(14),
            BackgroundColor = Color.FromArgb("#ffffff"),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Швидка оптимізація", FontAttributes = FontAttributes.Bold, FontSize = 15 },
                    _applyGuestPresetBtn
                }
            }
        };

        var animCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
            Stroke = Color.FromArgb("#cbd5e1"),
            Padding = new Thickness(14),
            BackgroundColor = Color.FromArgb("#ffffff"),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Швидкість анімацій інтерфейсу", FontAttributes = FontAttributes.Bold, FontSize = 15 },
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _animFastBtn, _animOffBtn, _animStockBtn }
                    }
                }
            }
        };

        var extraCard = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
            Stroke = Color.FromArgb("#cbd5e1"),
            Padding = new Thickness(14),
            BackgroundColor = Color.FromArgb("#ffffff"),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Додаткові системні налаштування", FontAttributes = FontAttributes.Bold, FontSize = 15 },
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { _dozeBtn, _screensaverBtn }
                    }
                }
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children = { statusCard, quickCard, animCard, extraCard }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (TvSession.Current.IsConnected)
        {
            await ReadCurrentStateAsync();
        }
    }

    private async Task ReadCurrentStateAsync()
    {
        if (!TvSession.Current.IsConnected)
        {
            _statusLabel.Text = "Не підключено до ТВ";
            _statusLabel.TextColor = Colors.Red;
            return;
        }

        _statusLabel.Text = "⏳ Зчитування параметрів...";
        _statusLabel.TextColor = Colors.DarkOrange;

        try
        {
            var winScale = (await TvSession.Current.ShellAsync("settings get global window_animation_scale")).Trim();
            var transScale = (await TvSession.Current.ShellAsync("settings get global transition_animation_scale")).Trim();
            var animScale = (await TvSession.Current.ShellAsync("settings get global animator_duration_scale")).Trim();
            var bgProc = (await TvSession.Current.ShellAsync("settings get global activity_manager_max_proc")).Trim();

            _currentScaleLabel.Text = $"Масштаб анімацій: Window={winScale}, Transition={transScale}, Animator={animScale}";
            _currentBgLabel.Text = $"Ліміт фонових процесів: {(string.IsNullOrWhiteSpace(bgProc) || bgProc == "null" ? "за замовчуванням" : bgProc)}";

            _statusLabel.Text = "✅ Параметри успішно зчитано";
            _statusLabel.TextColor = Color.FromArgb("#15803d");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Помилка: {ex.Message}";
            _statusLabel.TextColor = Colors.Red;
        }
    }

    private async Task ApplyGuestPresetAsync()
    {
        if (!TvSession.Current.IsConnected)
        {
            await DisplayAlert("Помилка", "Спочатку підключіться до ТВ", "OK");
            return;
        }

        bool confirm = await DisplayAlert(
            "Пресет «Швидкий ТВ»",
            "Застосувати оптимальні налаштування для Android TV (анімації 0.5x, оптимізація фону, увімкнення Doze)?",
            "Застосувати", "Скасувати");

        if (!confirm) return;

        try
        {
            foreach (var cmd in TweaksEngine.GuestPreset())
            {
                await TvSession.Current.ShellAsync(cmd.Command);
            }

            await DisplayAlert("Успіх", "Пресет швидкодії застосовано!", "OK");
            await ReadCurrentStateAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", ex.Message, "OK");
        }
    }

    private async Task ApplyAnimationAsync(AnimSpeed speed)
    {
        if (!TvSession.Current.IsConnected)
        {
            await DisplayAlert("Помилка", "Спочатку підключіться до ТВ", "OK");
            return;
        }

        try
        {
            foreach (var cmd in TweaksEngine.Animation(speed))
            {
                await TvSession.Current.ShellAsync(cmd.Command);
            }

            await DisplayAlert("Успіх", $"Встановлено анімації: {speed}", "OK");
            await ReadCurrentStateAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", ex.Message, "OK");
        }
    }

    private async Task ApplyCommandAsync(TweakCmd cmd)
    {
        if (!TvSession.Current.IsConnected)
        {
            await DisplayAlert("Помилка", "Спочатку підключіться до ТВ", "OK");
            return;
        }

        try
        {
            await TvSession.Current.ShellAsync(cmd.Command);
            await DisplayAlert("Успіх", $"Виконано: {cmd.Label}", "OK");
            await ReadCurrentStateAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", ex.Message, "OK");
        }
    }
}
