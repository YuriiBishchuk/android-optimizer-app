using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using TvOptimizer.App.Services;

namespace TvOptimizer.App.Pages;

public class ConnectPage : ContentPage
{
    private readonly Entry _hostEntry;
    private readonly Entry _portEntry;
    private readonly Entry _pairingPortEntry;
    private readonly Entry _pairingCodeEntry;
    private readonly Switch _tlsSwitch;
    private readonly Switch _pairingModeSwitch;
    private readonly VerticalStackLayout _pairingSection;
    private readonly Label _statusLabel;
    private readonly Label _errorLabel;
    private readonly Label _deviceInfoLabel;
    private readonly Label _lastUpdatedLabel;
    private readonly Label _attributionLabel;
    private readonly Button _pairBtn;
    private readonly Button _connectBtn;
    private readonly Button _disconnectBtn;
    private readonly Button _retryBtn;
    private readonly Button _refreshBtn;

    public ConnectPage()
    {
        Title = "Підключення";
        BackgroundColor = Color.FromArgb("#0f172a");

        _hostEntry = CreateStyledEntry("IP адреса пристрою (напр. 192.168.0.50)", Preferences.Default.Get("last_tv_host", "192.168.0."));
        _portEntry = CreateStyledEntry("Порт підключення (напр. 5555 або 37123)", Preferences.Default.Get("last_tv_port", "5555"));
        _pairingPortEntry = CreateStyledEntry("Порт створення пари (з екрана пристрою)", "");
        _pairingCodeEntry = CreateStyledEntry("6-значний код підключення", "", maxLength: 6);

        _tlsSwitch = new Switch { IsToggled = false, OnColor = Color.FromArgb("#38bdf8"), ThumbColor = Colors.White };
        _pairingModeSwitch = new Switch { IsToggled = false, OnColor = Color.FromArgb("#a855f7"), ThumbColor = Colors.White };

        _statusLabel = new Label
        {
            Text = "Стан: Очікування підключення",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#94a3b8")
        };

        _errorLabel = new Label
        {
            Text = "",
            FontSize = 13,
            TextColor = Color.FromArgb("#f87171"),
            IsVisible = false
        };

        _deviceInfoLabel = new Label
        {
            Text = "",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#4ade80"),
            IsVisible = false
        };

        _lastUpdatedLabel = new Label
        {
            Text = "Конфіги: Локальний кеш",
            FontSize = 12,
            TextColor = Color.FromArgb("#64748b")
        };

        _attributionLabel = new Label
        {
            Text = "Джерело правил: YuriiBishchuk/android-tv-optimizer (MIT)",
            FontSize = 11,
            TextColor = Color.FromArgb("#475569")
        };

        _pairBtn = new Button
        {
            Text = "🔑 Створити пару",
            BackgroundColor = Color.FromArgb("#9333ea"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8
        };
        _pairBtn.Clicked += async (s, e) => await OnPairClicked();

        _connectBtn = new Button
        {
            Text = "🔌 Підключитися",
            BackgroundColor = Color.FromArgb("#2563eb"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8
        };
        _connectBtn.Clicked += async (s, e) => await OnConnectClicked();

        _disconnectBtn = new Button
        {
            Text = "❌ Відключитися",
            BackgroundColor = Color.FromArgb("#dc2626"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            IsEnabled = false
        };
        _disconnectBtn.Clicked += async (s, e) => await OnDisconnectClicked();

        _retryBtn = new Button
        {
            Text = "🔄 Спробувати знову",
            BackgroundColor = Color.FromArgb("#d97706"),
            TextColor = Colors.White,
            CornerRadius = 8,
            IsVisible = false
        };
        _retryBtn.Clicked += async (s, e) => await OnConnectClicked();

        _refreshBtn = new Button
        {
            Text = "🔄 Оновити правила",
            BackgroundColor = Color.FromArgb("#334155"),
            TextColor = Colors.White,
            FontSize = 12,
            CornerRadius = 6,
            Padding = new Thickness(8, 4)
        };
        _refreshBtn.Clicked += async (s, e) => await OnRefreshClicked();

        _pairingSection = new VerticalStackLayout
        {
            Spacing = 10,
            IsVisible = false,
            Children =
            {
                new Label { Text = "Порт створення пари (Pairing Port):", TextColor = Color.FromArgb("#cbd5e1"), FontSize = 13 },
                _pairingPortEntry,
                new Label { Text = "Код підключення (6 цифр):", TextColor = Color.FromArgb("#cbd5e1"), FontSize = 13 },
                _pairingCodeEntry,
                _pairBtn
            }
        };

        _pairingModeSwitch.Toggled += (s, e) =>
        {
            _pairingSection.IsVisible = e.Value;
        };

        var instructionsCard = CreateCard(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = "📱 Інструкція для Android 11+:", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#f8fafc"), FontSize = 14 },
                new Label { Text = "1. На ТВ: Налаштування → Для розробників → Бездротове налагодження (Увімкнути).", TextColor = Color.FromArgb("#94a3b8"), FontSize = 12 },
                new Label { Text = "2. Якщо підключаєтесь вперше: увімкніть «Режим створення пари» нижче.", TextColor = Color.FromArgb("#94a3b8"), FontSize = 12 },
                new Label { Text = "3. Після успішної пари: введіть основний порт ТВ та натисніть «Підключитися».", TextColor = Color.FromArgb("#94a3b8"), FontSize = 12 },
            }
        });

        var tlsRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        tlsRow.Add(new Label { Text = "TLS шифрування (Android 11+)", TextColor = Color.FromArgb("#cbd5e1"), VerticalOptions = LayoutOptions.Center }, 0, 0);
        tlsRow.Add(_tlsSwitch, 1, 0);

        var pairModeRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        pairModeRow.Add(new Label { Text = "Режим створення пари (Pairing Mode)", TextColor = Color.FromArgb("#a855f7"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center }, 0, 0);
        pairModeRow.Add(_pairingModeSwitch, 1, 0);

        var formCard = CreateCard(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "IP-адреса Android пристрою:", TextColor = Color.FromArgb("#cbd5e1"), FontSize = 13 },
                _hostEntry,
                new Label { Text = "Основний порт ADB (Connection Port):", TextColor = Color.FromArgb("#cbd5e1"), FontSize = 13 },
                _portEntry,
                tlsRow,
                pairModeRow,
                _pairingSection,
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children = { _connectBtn, _disconnectBtn, _retryBtn }
                }
            }
        });

        var syncCard = CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children = { _lastUpdatedLabel, _refreshBtn }
                },
                _attributionLabel
            }
        });

        var mainLayout = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = "🤖 Android Optimizer",
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#38bdf8"),
                    HorizontalOptions = LayoutOptions.Center
                },
                _statusLabel,
                _errorLabel,
                _deviceInfoLabel,
                instructionsCard,
                formCard,
                syncCard
            }
        };

        Content = new ScrollView { Content = mainLayout, BackgroundColor = Color.FromArgb("#0f172a") };
        UpdateUiState();
    }

    private static Border CreateCard(View inner)
    {
        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
            Stroke = Color.FromArgb("#334155"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#1e293b"),
            Padding = new Thickness(14),
            Content = inner
        };
    }

    private static Entry CreateStyledEntry(string placeholder, string text, int? maxLength = null)
    {
        var entry = new Entry
        {
            Placeholder = placeholder,
            PlaceholderColor = Color.FromArgb("#64748b"),
            TextColor = Color.FromArgb("#f8fafc"),
            BackgroundColor = Color.FromArgb("#0f172a"),
            Keyboard = Keyboard.Numeric,
            Text = text
        };
        if (maxLength.HasValue) entry.MaxLength = maxLength.Value;
        return entry;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateUiState();
        await LoadAttribution();
    }

    private void UpdateUiState()
    {
        var session = TvSession.Current;
        switch (session.State)
        {
            case TvConnectionState.Idle:
                _statusLabel.Text = "Стан: Очікування підключення";
                _statusLabel.TextColor = Color.FromArgb("#94a3b8");
                _connectBtn.IsEnabled = true;
                _disconnectBtn.IsEnabled = false;
                _retryBtn.IsVisible = false;
                _errorLabel.IsVisible = false;
                _deviceInfoLabel.IsVisible = false;
                break;

            case TvConnectionState.Pairing:
                _statusLabel.Text = "Стан: Створення пари з пристроєм...";
                _statusLabel.TextColor = Color.FromArgb("#c084fc");
                _connectBtn.IsEnabled = false;
                _disconnectBtn.IsEnabled = false;
                _retryBtn.IsVisible = false;
                break;

            case TvConnectionState.Connecting:
                _statusLabel.Text = "Стан: Підключення до ADB...";
                _statusLabel.TextColor = Color.FromArgb("#60a5fa");
                _connectBtn.IsEnabled = false;
                _disconnectBtn.IsEnabled = false;
                _retryBtn.IsVisible = false;
                break;

            case TvConnectionState.Connected:
                _statusLabel.Text = "Стан: Підключено ✅";
                _statusLabel.TextColor = Color.FromArgb("#4ade80");
                _deviceInfoLabel.Text = $"Пристрій: {session.Manufacturer} {session.DeviceModel} (Android {session.AndroidVersion})";
                _deviceInfoLabel.IsVisible = true;
                _connectBtn.IsEnabled = false;
                _disconnectBtn.IsEnabled = true;
                _retryBtn.IsVisible = false;
                _errorLabel.IsVisible = false;
                break;

            case TvConnectionState.Error:
                _statusLabel.Text = "Стан: Помилка підключення ❌";
                _statusLabel.TextColor = Color.FromArgb("#f87171");
                _errorLabel.Text = $"{session.LastError}\nПідказка: якщо пристрій перезавантажувався, перевірте новий порт у Wireless Debugging!";
                _errorLabel.IsVisible = true;
                _connectBtn.IsEnabled = true;
                _disconnectBtn.IsEnabled = false;
                _retryBtn.IsVisible = true;
                break;
        }

        if (ConfigSync.Current != null)
        {
            _lastUpdatedLabel.Text = ConfigSync.Current.LastUpdatedUtc.HasValue
                ? $"Конфіги оновлено: {ConfigSync.Current.LastUpdatedUtc.Value:yyyy-MM-dd HH:mm:ss}"
                : "Конфіги оновлено: Локальний кеш";
        }
    }

    private async Task OnPairClicked()
    {
        var host = _hostEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            await DisplayAlert("Помилка", "Введіть IP адресу пристрою", "OK");
            return;
        }

        if (!int.TryParse(_pairingPortEntry.Text, out var pairingPort) || pairingPort < 1 || pairingPort > 65535)
        {
            await DisplayAlert("Помилка", "Неправильний порт створення пари", "OK");
            return;
        }

        var code = _pairingCodeEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
        {
            await DisplayAlert("Помилка", "Код має містити 6 цифр", "OK");
            return;
        }

        try
        {
            UpdateUiState();
            await TvSession.Current.PairAsync(host, pairingPort, code);
            await DisplayAlert("Успіх", "Пару створено успішно! Тепер натисніть Підключитися", "OK");
            UpdateUiState();
        }
        catch (Exception ex)
        {
            UpdateUiState();
            await DisplayAlert("Помилка створення пари", ex.Message, "OK");
        }
    }

    private async Task OnConnectClicked()
    {
        var host = _hostEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            await DisplayAlert("Помилка", "Введіть IP адресу пристрою", "OK");
            return;
        }

        if (!int.TryParse(_portEntry.Text, out var port) || port < 1 || port > 65535)
        {
            await DisplayAlert("Помилка", "Неправильний порт підключення", "OK");
            return;
        }

        Preferences.Default.Set("last_tv_host", host);
        Preferences.Default.Set("last_tv_port", _portEntry.Text.Trim());

        try
        {
            UpdateUiState();
            var info = await TvSession.Current.ConnectAsync(host, port, _tlsSwitch.IsToggled);
            UpdateUiState();
            await DisplayAlert("Успіх", $"Підключено: {info}", "OK");
        }
        catch (Exception ex)
        {
            UpdateUiState();
            await DisplayAlert("Помилка підключення", $"{ex.Message}\n\nПеревірте IP адресу, порт та чи активне бездротове налагодження на пристрої.", "OK");
        }
    }

    private async Task OnDisconnectClicked()
    {
        await TvSession.Current.DisconnectAsync();
        UpdateUiState();
        await DisplayAlert("Інфо", "Відключено від пристрою", "OK");
    }

    private async Task OnRefreshClicked()
    {
        _refreshBtn.IsEnabled = false;
        try
        {
            var success = await ConfigSync.Current.RefreshAsync();
            UpdateUiState();
            await DisplayAlert("Синхронізація", success ? "Конфігурації успішно оновлено" : "Використовується кеш правил", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", ex.Message, "OK");
        }
        finally
        {
            _refreshBtn.IsEnabled = true;
        }
    }

    private async Task LoadAttribution()
    {
        try
        {
            _attributionLabel.Text = "Джерело правил: YuriiBishchuk/android-tv-optimizer (MIT License)";
        }
        catch
        {
            _attributionLabel.Text = "Правила: Вбудовані та перевірені спільнотою";
        }
        await Task.CompletedTask;
    }
}
