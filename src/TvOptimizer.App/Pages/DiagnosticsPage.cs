using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using TvOptimizer.App.Services;

namespace TvOptimizer.App.Pages;

public class DiagnosticsPage : ContentPage
{
    private readonly DiagnosticsService _diagnostics = new();
    private readonly Label _dataLabel;
    private readonly Button _refreshBtn;

    public DiagnosticsPage()
    {
        Title = "Діагностика";
        BackgroundColor = Color.FromArgb("#0f172a");

        _dataLabel = new Label
        {
            TextColor = Colors.White,
            Padding = 16
        };

        _refreshBtn = new Button
        {
            Text = "🔄 Оновити",
            BackgroundColor = Color.FromArgb("#2563eb"),
            TextColor = Colors.White
        };
        _refreshBtn.Clicked += async (s, e) => await RefreshDiagnosticsAsync();

        Content = new VerticalStackLayout
        {
            Children = { _refreshBtn, _dataLabel }
        };
    }

    private async Task RefreshDiagnosticsAsync()
    {
        try
        {
            _dataLabel.Text = "Отримання даних...";
            var data = await _diagnostics.GetDiagnosticsAsync();
            _dataLabel.Text = $"RAM: {data.MemoryInfo}\n" +
                              $"Storage: {data.StorageInfo}\n" +
                              $"Temp: {data.Temperature}\n" +
                              $"Wi-Fi: {data.WifiInfo}\n" +
                              $"HDR: {data.HdrInfo}\n" +
                              $"Audio: {data.AudioInfo}";
        }
        catch (Exception ex)
        {
            _dataLabel.Text = $"Помилка: {ex.Message}";
        }
    }
}
