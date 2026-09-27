using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Threading.Tasks;
using TvOptimizer.App.Services;

namespace TvOptimizer.App.Pages;

public class ConnectPage : ContentPage
{
    private readonly Label _statusLabel;
    private readonly Label _lastUpdatedLabel;
    private readonly Entry _hostEntry;
    private readonly Entry _portEntry;
    private readonly Switch _tlsSwitch;
    private readonly Button _connectBtn;
    private readonly Button _disconnectBtn;
    private readonly Button _refreshBtn;
    private readonly Label _attributionLabel;

    public ConnectPage()
    {
        Title = "Підключення";
        Padding = new Thickness(20);

        _statusLabel = new Label
        {
            Text = "Не підключено",
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Colors.Red
        };

        _lastUpdatedLabel = new Label
        {
            Text = "Last updated: Never",
            HorizontalOptions = LayoutOptions.Start,
            FontSize = 12,
            TextColor = Colors.Gray
        };

        _hostEntry = new Entry
        {
            Placeholder = "адреса ТВ (IP або hostname)",
            Text = "192.168.1.100"
        };

        _portEntry = new Entry
        {
            Placeholder = "порт ADB (за замовчуванням 5555)",
            Text = "5555",
            Keyboard = Keyboard.Numeric
        };

        _tlsSwitch = new Switch
        {
            IsToggled = false
        };

        _connectBtn = new Button
        {
            Text = "Підключитися",
            BackgroundColor = Colors.Green,
            TextColor = Colors.White
        };
        _connectBtn.Clicked += async (s, e) => await OnConnectClicked();

        _disconnectBtn = new Button
        {
            Text = "Відключити",
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            IsEnabled = false
        };
        _disconnectBtn.Clicked += async (s, e) => await OnDisconnectClicked();

        _refreshBtn = new Button
        {
            Text = "Update now",
            BackgroundColor = Colors.Blue,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Start
        };
        _refreshBtn.Clicked += async (s, e) => await OnRefreshClicked();

        _attributionLabel = new Label
        {
            Text = "Community lists attribution: Not loaded",
            HorizontalOptions = LayoutOptions.Start,
            FontSize = 12,
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var layout = new StackLayout
        {
            Spacing = 15,
            Children = {
                new Label { Text = "Підключення до Android TV через ADB", FontSize = 18, HorizontalOptions = LayoutOptions.Center },
                new BoxView { HeightRequest = 1, Color = Colors.LightGray, HorizontalOptions = LayoutOptions.FillAndExpand },
                _statusLabel,
                new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 10,
                    HorizontalOptions = LayoutOptions.Start,
                    Children = { _lastUpdatedLabel, _refreshBtn }
                },
                new Frame { Padding = 15, Content = new StackLayout
                {
                    Spacing = 10,
                    Children = {
                        new Label { Text = "Хост:", FontAttributes = FontAttributes.Bold },
                        _hostEntry,
                        new Label { Text = "Порт:", FontAttributes = FontAttributes.Bold },
                        _portEntry,
                        new Label { Text = "TLS/SSL:", FontAttributes = FontAttributes.Bold },
                        _tlsSwitch
                    }
                }},
                new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 10,
                    HorizontalOptions = LayoutOptions.Center,
                    Children = { _connectBtn, _disconnectBtn }
                },
                new Frame
                {
                    Padding = 15,
                    Content = new StackLayout
                    {
                        Spacing = 10,
                        Children = {
                            new Label { Text = "Community lists attribution:", FontAttributes = FontAttributes.Bold },
                            _attributionLabel
                        }
                    }
                }
            }
        };

        Content = new ScrollView { Content = layout };
        UpdateUiState();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateUiState();
        await LoadAttribution();
    }

    private async Task OnRefreshClicked()
    {
        _refreshBtn.IsEnabled = false;
        try
        {
            var success = await ConfigSync.Current.RefreshAsync();
            _lastUpdatedLabel.Text = success ? $"Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" : "Update failed";
            await Toast.Make(success ? "Configs refreshed" : "Update failed").Show();
        }
        catch (Exception ex)
        {
            await Toast.Make($"Error: {ex.Message}").Show();
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
            var attribution = await GetCommunityListsAttributionAsync();
            _attributionLabel.Text = attribution ?? "Not available";
        }
        catch
        {
            _attributionLabel.Text = "Failed to load attribution";
        }
    }

    private async Task<string?> GetCommunityListsAttributionAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var url = "https://raw.githubusercontent.com/YuriiBishchuk/android-tv-optimizer/main/docs/COMMUNITY_LISTS.md";
            var response = await http.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var lines = content.Split('\n');
                var summary = new List<string>();
                foreach (var line in lines)
                {
                    if (line.StartsWith("##") || line.StartsWith("###") || line.StartsWith("####"))
                        summary.Add($"\n{line.Trim()}\n");
                    else if (string.IsNullOrWhiteSpace(line))
                        summary.Add("");
                    else if (summary.Count >= 20)
                        break;
                    else
                        summary.Add(line);
                }
                return string.Join("\n", summary);
            }
        }
        catch { }
        return null;
    }

    private void UpdateUiState()
    {
        var connected = TvSession.Current.IsConnected;
        _statusLabel.Text = connected ? $"Підключено: {TvSession.Current.DeviceModel}" : "Не підключено";
        _statusLabel.TextColor = connected ? Colors.Green : Colors.Red;
        _connectBtn.IsEnabled = !connected;
        _disconnectBtn.IsEnabled = connected;
        _hostEntry.IsEnabled = !connected;
        _portEntry.IsEnabled = !connected;
        _tlsSwitch.IsEnabled = !connected;
        _refreshBtn.IsEnabled = true;

        if (ConfigSync.Current != null)
        {
            _lastUpdatedLabel.Text = ConfigSync.Current.LastUpdatedUtc.HasValue
                ? $"Last updated: {ConfigSync.Current.LastUpdatedUtc.Value:yyyy-MM-dd HH:mm:ss}"
                : "Last updated: Never";
        }
    }

    private async Task OnConnectClicked()
    {
        if (!int.TryParse(_portEntry.Text, out var port) || port < 1 || port > 65535)
        {
            await Toast.Make("Неправильний порт").Show();
            return;
        }

        _connectBtn.IsEnabled = false;
        try
        {
            var model = await TvSession.Current.ConnectAsync(
                _hostEntry.Text.Trim(), port, _tlsSwitch.IsToggled);
            await Toast.Make($"Підключено до {model}").Show();
        }
        catch (Exception ex)
        {
            await Toast.Make($"Помилка: {ex.Message}").Show();
        }
        finally
        {
            UpdateUiState();
            _connectBtn.IsEnabled = true;
        }
    }

    private async Task OnDisconnectClicked()
    {
        await TvSession.Current.DisconnectAsync();
        await Toast.Make("Відключено").Show();
        UpdateUiState();
    }
}
