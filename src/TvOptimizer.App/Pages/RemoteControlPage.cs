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

    public RemoteControlPage()
    {
        Title = "Пульт ДУ";
        BackgroundColor = Color.FromArgb("#0f172a");
        Padding = new Thickness(16);

        _statusLabel = new Label
        {
            Text = "Готово",
            FontSize = 13,
            TextColor = Color.FromArgb("#94a3b8"),
            HorizontalOptions = LayoutOptions.Center
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto }, // 0: Status
                new RowDefinition { Height = GridLength.Auto }, // 1: D-pad up
                new RowDefinition { Height = GridLength.Auto }, // 2: D-pad middle
                new RowDefinition { Height = GridLength.Auto }, // 3: D-pad down
                new RowDefinition { Height = GridLength.Auto }, // 4: Side buttons
                new RowDefinition { Height = GridLength.Auto }, // 5: Volume
                new RowDefinition { Height = GridLength.Auto }, // 6: Reboot
                new RowDefinition { Height = GridLength.Auto }  // 7: Recovery
            },
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        // Status label
        grid.Add(_statusLabel, 0, 0);
        Grid.SetColumnSpan(_statusLabel, 3);

        // D-pad buttons
        var btnUp = CreateButton("↑", 19, "DPAD_UP");
        var btnLeft = CreateButton("←", 21, "DPAD_LEFT");
        var btnCenter = CreateButton("●", 23, "DPAD_CENTER");
        var btnRight = CreateButton("→", 22, "DPAD_RIGHT");
        var btnDown = CreateButton("↓", 20, "DPAD_DOWN");

        grid.Add(btnUp, 1, 1);        // row1, col1
        grid.Add(btnLeft, 0, 2);      // row2, col0
        grid.Add(btnCenter, 1, 2);    // row2, col1
        grid.Add(btnRight, 2, 2);     // row2, col2
        grid.Add(btnDown, 1, 3);      // row3, col1

        // Side buttons: Back, Home, Menu
        var btnBack = CreateButton("Назад", 4, "BACK");
        var btnHome = CreateButton("Дом", 3, "HOME");
        var btnMenu = CreateButton("Меню", 82, "MENU");

        grid.Add(btnBack, 0, 4);
        grid.Add(btnHome, 1, 4);
        grid.Add(btnMenu, 2, 4);

        // Volume buttons
        var btnVolUp = CreateButton("Гучність +", 24, "VOLUME_UP");
        var btnVolDown = CreateButton("Гучність -", 25, "VOLUME_DOWN");
        var btnMute = CreateButton("Вимк звук", 164, "MUTE");

        grid.Add(btnVolUp, 0, 5);
        grid.Add(btnVolDown, 1, 5);
        grid.Add(btnMute, 2, 5);

        // System buttons: Reboot, Reboot Recovery (each full width)
        var btnReboot = CreateButton("Перезавантажити", 0, "REBOOT", isCommand: true);
        var btnRebootRecovery = CreateButton("Перезавантажити у recovery", 0, "REBOOT_RECOVERY", isCommand: true);

        grid.Add(btnReboot, 0, 6);
        Grid.SetColumnSpan(btnReboot, 3);
        grid.Add(btnRebootRecovery, 0, 7);
        Grid.SetColumnSpan(btnRebootRecovery, 3);

        Content = new ScrollView
        {
            Content = grid
        };
    }

    private Button CreateButton(string text, int keycode, string label, bool isCommand = false)
    {
        var button = new Button
        {
            Text = text,
            FontSize = 16,
            BackgroundColor = Color.FromArgb("#2563eb"),
            TextColor = Colors.White,
            CornerRadius = 8
        };

        button.Clicked += async (s, e) =>
        {
            if (!TvSession.Current.IsConnected)
            {
                await DisplayAlert("Помилка", "Спочатку підключіться до ТВ", "OK");
                return;
            }

            try
            {
                if (isCommand)
                {
                    // For reboot and reboot recovery, we send a shell command
                    string command = label; // label is the command string
                    await TvSession.Current.ShellAsync(command);
                }
                else
                {
                    // For keyevents
                    await TvSession.Current.ShellAsync($"input keyevent {keycode}");
                }

                _statusLabel.Text = $"Виконано: {label}";
                _statusLabel.TextColor = Color.FromArgb("#4ade80");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Помилка: {ex.Message}";
                _statusLabel.TextColor = Color.FromArgb("#f87171");
            }
        };

        return button;
    }
}