using TvOptimizer.App.Services;

namespace TvOptimizer.App.Pages;

public class ConnectPage : ContentPage
{
    private readonly Label _statusLabel;
    private readonly Entry _hostEntry;
    private readonly Entry _portEntry;
    private readonly Switch _tlsSwitch;
    private readonly Button _connectBtn;
    private readonly Button _disconnectBtn;

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
        _connectBtn.Clicked += (s, e) => OnConnectClicked();

        _disconnectBtn = new Button
        {
            Text = "Відключити",
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            IsEnabled = false
        };
        _disconnectBtn.Clicked += (s, e) => OnDisconnectClicked();

        var layout = new StackLayout
        {
            Spacing = 15,
            Children =
            {
                new Label { Text = "Підключення до Android TV через ADB", FontSize = 18 },
                _statusLabel,
                _hostEntry,
                _portEntry,
                _tlsSwitch,
                _connectBtn,
                _disconnectBtn
            }
        };

        Content = layout;
    }

    private async void OnConnectClicked()
    {
        _statusLabel.Text = "Підключення...";
        _statusLabel.TextColor = Colors.Blue;
        var host = _hostEntry.Text;
        var port = _portEntry.Text;

        try
        {
            var service = TvSession.Current;
            await service.ConnectAsync(host, int.Parse(port), _tlsSwitch.IsToggled);
            _statusLabel.Text = "Підключено";
            _statusLabel.TextColor = Colors.Green;
            _connectBtn.IsEnabled = false;
            _disconnectBtn.IsEnabled = true;
            await DisplayAlert("Успіх", "Підключено успішно", "OK");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Помилка підключення";
            _statusLabel.TextColor = Colors.Red;
            await DisplayAlert("Помилка", $"Помилка: {ex.Message}", "OK");
        }
    }

    private async void OnDisconnectClicked()
    {
        _statusLabel.Text = "Відключення...";
        _statusLabel.TextColor = Colors.Blue;
        await TvSession.Current.DisconnectAsync();
        _statusLabel.Text = "Не підключено";
        _statusLabel.TextColor = Colors.Red;
        _connectBtn.IsEnabled = true;
        _disconnectBtn.IsEnabled = false;
        await DisplayAlert("Успіх", "Відключено", "OK");
    }
}