using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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
    private readonly Button _discoverBtn;

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
            TextColor = Color.FromArgb("#64748b"),
            VerticalOptions = LayoutOptions.Center
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
            CornerRadius = 8,
            HeightRequest = 46
        };
        _pairBtn.Clicked += async (s, e) => await OnPairClicked();

        _connectBtn = new Button
        {
            Text = "🔌 Підключитися",
            BackgroundColor = Color.FromArgb("#2563eb"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 46
        };
        _connectBtn.Clicked += async (s, e) => await OnConnectClicked();

        _disconnectBtn = new Button
        {
            Text = "❌ Відключитися",
            BackgroundColor = Color.FromArgb("#dc2626"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 46,
            IsEnabled = false
        };
        _disconnectBtn.Clicked += async (s, e) => await OnDisconnectClicked();

        _retryBtn = new Button
        {
            Text = "🔄 Спробувати знову",
            BackgroundColor = Color.FromArgb("#d97706"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 46,
            IsVisible = false
        };
        _retryBtn.Clicked += async (s, e) => await OnConnectClicked();

        _discoverBtn = new Button
        {
            Text = "🔍 Автопошук пристрою у Wi-Fi",
            BackgroundColor = Color.FromArgb("#0284c7"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 46
        };
        _discoverBtn.Clicked += async (s, e) => await OnDiscoverClicked();

        _refreshBtn = new Button
        {
            Text = "🔄 Оновити",
            BackgroundColor = Color.FromArgb("#334155"),
            TextColor = Colors.White,
            FontSize = 12,
            CornerRadius = 6,
            Padding = new Thickness(12, 6)
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
                new Label { Text = "1. На пристрої: Налаштування → Для розробників → Бездротове налагодження (Увімкнути).", TextColor = Color.FromArgb("#94a3b8"), FontSize = 12 },
                new Label { Text = "2. Якщо підключаєтесь вперше: увімкніть «Режим створення пари» нижче.", TextColor = Color.FromArgb("#94a3b8"), FontSize = 12 },
                new Label { Text = "3. Після успішної пари: введіть основний порт пристрою та натисніть «Підключитися».", TextColor = Color.FromArgb("#94a3b8"), FontSize = 12 },
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

        var buttonsGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8,
            RowSpacing = 8
        };
        buttonsGrid.Add(_connectBtn, 0, 0);
        buttonsGrid.Add(_disconnectBtn, 1, 0);
        buttonsGrid.Add(_retryBtn, 0, 1);
        Grid.SetColumnSpan(_retryBtn, 2);
        buttonsGrid.Add(_discoverBtn, 0, 2);
        Grid.SetColumnSpan(_discoverBtn, 2);

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
                buttonsGrid
            }
        });

        var syncRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        syncRow.Add(_lastUpdatedLabel, 0, 0);
        syncRow.Add(_refreshBtn, 1, 0);

        var syncCard = CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                syncRow,
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

    private async Task OnDiscoverClicked()
    {
        _discoverBtn.IsEnabled = false;
        _discoverBtn.Text = "⏳ Пошук у Wi-Fi...";
        _statusLabel.Text = "Стан: Сканування мережі на відкритий ADB...";
        _statusLabel.TextColor = Color.FromArgb("#38bdf8");

        try
        {
            var found = await Task.Run(async () =>
            {
                var baseSubnets = new List<string> { "192.168.0.", "192.168.1.", "100.66.28." };
                var currentHost = _hostEntry.Text?.Trim() ?? "";
                if (currentHost.Contains('.'))
                {
                    var lastDot = currentHost.LastIndexOf('.');
                    var prefix = currentHost.Substring(0, lastDot + 1);
                    if (!string.IsNullOrEmpty(prefix) && !baseSubnets.Contains(prefix))
                    {
                        baseSubnets.Insert(0, prefix);
                    }
                }

                int.TryParse(_portEntry.Text?.Trim(), out var customPort);
                var portsToScan = customPort > 0 && customPort != 5555
                    ? new[] { customPort, 5555 }
                    : new[] { 5555 };

                foreach (var subnet in baseSubnets)
                {
                    var tasks = new List<Task<(string ip, int port)?>>();
                    for (int i = 1; i <= 254; i++)
                    {
                        var ip = $"{subnet}{i}";
                        foreach (var port in portsToScan)
                        {
                            tasks.Add(Task.Run(async () =>
                            {
                                try
                                {
                                    using var tcp = new TcpClient();
                                    var connectTask = tcp.ConnectAsync(ip, port);
                                    if (await Task.WhenAny(connectTask, Task.Delay(120)) == connectTask && tcp.Connected)
                                    {
                                        return (string ip, int port)? (ip, port);
                                    }
                                }
                                catch { }
                                return null;
                            }));
                        }
                    }

                    var results = await Task.WhenAll(tasks);
                    var match = results.FirstOrDefault(r => r.HasValue);
                    if (match.HasValue) return match.Value;
                }
                return null;
            });

            if (found.HasValue)
            {
                _hostEntry.Text = found.Value.ip;
                _portEntry.Text = found.Value.port.ToString();
                _statusLabel.Text = $"Знайдено пристрій: {found.Value.ip}:{found.Value.port} ✅";
                _statusLabel.TextColor = Color.FromArgb("#4ade80");
                await DisplayAlert("Знайдено пристрій", $"Виявлено активний ADB сервіс:\n{found.Value.ip}:{found.Value.port}\n\nНатисніть «Підключитися».", "OK");
            }
            else
            {
                _statusLabel.Text = "Стан: Пристроїв не знайдено";
                _statusLabel.TextColor = Color.FromArgb("#f87171");
                await DisplayAlert("Пошук завершено", "У локальній підмережі не знайдено пристроїв зі стандартним портом 5555.\nЯкщо ви використовуєте випадковий порт Wireless Debugging, введіть його вручну з екрана розробника.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка сканування", ex.Message, "OK");
        }
        finally
        {
            _discoverBtn.IsEnabled = true;
            _discoverBtn.Text = "🔍 Автопошук пристрою у Wi-Fi";
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
