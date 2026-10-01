using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
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

        _hostEntry = new Entry
        {
            Placeholder = "IP адреса ТВ (напр. 192.168.0.50)",
            Keyboard = Keyboard.Numeric,
            Text = Preferences.Default.Get("last_tv_host", "127.0.0.1")
        };

        _portEntry = new Entry
        {
            Placeholder = "Порт підключення (напр. 5555 або 37123)",
            Keyboard = Keyboard.Numeric,
            Text = Preferences.Default.Get("last_tv_port", "5555")
        };

        _pairingPortEntry = new Entry
        {
            Placeholder = "Порт створення пари (з екрана ТВ)",
            Keyboard = Keyboard.Numeric,
            Text = ""
        };

        _pairingCodeEntry = new Entry
        {
            Placeholder = "6-значний код підключення",
            Keyboard = Keyboard.Numeric,
            MaxLength = 6,
            Text = ""
        };

        _tlsSwitch = new Switch { IsToggled = false };
        _pairingModeSwitch = new Switch { IsToggled = false };

        _statusLabel = new Label
        {
            Text = "Стан: Очікування підключення",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Gray
        };

        _errorLabel = new Label
        {
            Text = "",
            FontSize = 13,
            TextColor = Colors.Red,
            IsVisible = false
        };

        _deviceInfoLabel = new Label
        {
            Text = "",
            FontSize = 14,
            TextColor = Colors.DarkGreen,
            IsVisible = false
        };

        _lastUpdatedLabel = new Label
        {
            Text = "Оновлення конфігів: Завантаження...",
            FontSize = 12,
            TextColor = Colors.DimGray
        };

        _attributionLabel = new Label
        {
            Text = "Правила: YuriiBishchuk/android-tv-optimizer",
            FontSize = 11,
            TextColor = Colors.DimGray
        };

        _pairBtn = new Button
        {
            Text = "Створити пару",
            BackgroundColor = Colors.Purple,
            TextColor = Colors.White
        };
        _pairBtn.Clicked += async (s, e) => await OnPairClicked();

        _connectBtn = new Button
        {
            Text = "Підключитися",
            BackgroundColor = Colors.DarkBlue,
            TextColor = Colors.White
        };
        _connectBtn.Clicked += async (s, e) => await OnConnectClicked();

        _disconnectBtn = new Button
        {
            Text = "Відключитися",
            BackgroundColor = Colors.DarkRed,
            TextColor = Colors.White,
            IsEnabled = false
        };
        _disconnectBtn.Clicked += async (s, e) => await OnDisconnectClicked();

        _retryBtn = new Button
        {
            Text = "Повторити спробу",
            BackgroundColor = Colors.DarkOrange,
            TextColor = Colors.White,
            IsVisible = false
        };
        _retryBtn.Clicked += async (s, e) => await OnConnectClicked();

        _refreshBtn = new Button
        {
            Text = "Оновити правила",
            FontSize = 12
        };
        _refreshBtn.Clicked += async (s, e) => await OnRefreshClicked();

        _pairingSection = new VerticalStackLayout
        {
            Spacing = 8,
            IsVisible = false,
            Children =
            {
                new Label { Text = "Порт створення пари:", FontAttributes = FontAttributes.Bold },
                _pairingPortEntry,
                new Label { Text = "Код створення пари (6 цифр):", FontAttributes = FontAttributes.Bold },
                _pairingCodeEntry,
                _pairBtn
            }
        };

        _pairingModeSwitch.Toggled += (s, e) =>
        {
            _pairingSection.IsVisible = _pairingModeSwitch.IsToggled;
        };

        var instructionsFrame = new Frame
        {
            Padding = 12,
            BorderColor = Colors.LightSkyBlue,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = "📺 Інструкція з активації Wireless ADB на ТВ:", FontAttributes = FontAttributes.Bold, FontSize = 14 },
                    new Label { Text = "1. Налаштування ТВ > Про пристрій > 7 разів натисніть Номер збірки", FontSize = 12 },
                    new Label { Text = "2. Для розробників > Бездротове налагодження > УВІМКНУТИ", FontSize = 12 },
                    new Label { Text = "3. Введіть IP та порт нижче (після перезавантаження ТВ порт змінюється!)", FontSize = 12, TextColor = Colors.DarkOrange }
                }
            }
        };

        var formFrame = new Frame
        {
            Padding = 15,
            Content = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label { Text = "IP адреса ТВ:", FontAttributes = FontAttributes.Bold },
                    _hostEntry,
                    new Label { Text = "Порт ADB:", FontAttributes = FontAttributes.Bold },
                    _portEntry,
                    new HorizontalStackLayout
                    {
                        Spacing = 10,
                        Children = { new Label { Text = "TLS/SSL:", VerticalOptions = LayoutOptions.Center }, _tlsSwitch }
                    },
                    new HorizontalStackLayout
                    {
                        Spacing = 10,
                        Children = { new Label { Text = "Режим створення пари (Pairing):", VerticalOptions = LayoutOptions.Center }, _pairingModeSwitch }
                    },
                    _pairingSection,
                    new HorizontalStackLayout
                    {
                        Spacing = 10,
                        HorizontalOptions = LayoutOptions.Center,
                        Children = { _connectBtn, _disconnectBtn, _retryBtn }
                    }
                }
            }
        };

        var syncFrame = new Frame
        {
            Padding = 12,
            Content = new VerticalStackLayout
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
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 15,
                Children =
                {
                    new Label { Text = "Підключення до Android TV через ADB", FontSize = 18, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center },
                    _statusLabel,
                    _errorLabel,
                    _deviceInfoLabel,
                    instructionsFrame,
                    formFrame,
                    syncFrame
                }
            }
        };

        UpdateUiState();
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
                _statusLabel.TextColor = Colors.Gray;
                _connectBtn.IsEnabled = true;
                _disconnectBtn.IsEnabled = false;
                _retryBtn.IsVisible = false;
                _errorLabel.IsVisible = false;
                _deviceInfoLabel.IsVisible = false;
                break;

            case TvConnectionState.Pairing:
                _statusLabel.Text = "Стан: Створення пари з ТВ...";
                _statusLabel.TextColor = Colors.Purple;
                _connectBtn.IsEnabled = false;
                _disconnectBtn.IsEnabled = false;
                _retryBtn.IsVisible = false;
                break;

            case TvConnectionState.Connecting:
                _statusLabel.Text = "Стан: Підключення до ADB...";
                _statusLabel.TextColor = Colors.Blue;
                _connectBtn.IsEnabled = false;
                _disconnectBtn.IsEnabled = false;
                _retryBtn.IsVisible = false;
                break;

            case TvConnectionState.Connected:
                _statusLabel.Text = "Стан: Підключено ✅";
                _statusLabel.TextColor = Colors.Green;
                _deviceInfoLabel.Text = $"Пристрій: {session.Manufacturer} {session.DeviceModel} (Android {session.AndroidVersion})";
                _deviceInfoLabel.IsVisible = true;
                _connectBtn.IsEnabled = false;
                _disconnectBtn.IsEnabled = true;
                _retryBtn.IsVisible = false;
                _errorLabel.IsVisible = false;
                break;

            case TvConnectionState.Error:
                _statusLabel.Text = "Стан: Помилка підключення ❌";
                _statusLabel.TextColor = Colors.Red;
                _errorLabel.Text = $"{session.LastError}\nПідказка: якщо ТВ перезавантажувався, перевірте новий порт у Wireless Debugging!";
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
            await DisplayAlert("Помилка", "Введіть IP адресу ТВ", "OK");
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
            await DisplayAlert("Помилка", "Введіть IP адресу ТВ", "OK");
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
            await DisplayAlert("Помилка підключення", $"{ex.Message}\n\nПеревірте IP адресу, порт та чи активне бездротове налагодження на ТВ.", "OK");
        }
    }

    private async Task OnDisconnectClicked()
    {
        await TvSession.Current.DisconnectAsync();
        UpdateUiState();
        await DisplayAlert("Інфо", "Відключено від ТВ", "OK");
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