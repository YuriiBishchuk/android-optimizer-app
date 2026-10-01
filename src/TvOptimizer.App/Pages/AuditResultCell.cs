using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using TvOptimizer.Core.Audit;

namespace TvOptimizer.App.Pages;

public class AuditResultCell : Border
{
    public AuditResultCell()
    {
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) };
        Stroke = Color.FromArgb("#334155");
        StrokeThickness = 1;
        Padding = new Thickness(12, 8);
        Margin = new Thickness(0, 4);
        BackgroundColor = Color.FromArgb("#1e293b");

        var pkgLabel = new Label
        {
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            TextColor = Color.FromArgb("#f8fafc"),
            LineBreakMode = LineBreakMode.TailTruncation
        };
        pkgLabel.SetBinding(Label.TextProperty, "PackageName");

        var badgeLabel = new Label
        {
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            Padding = new Thickness(6, 2)
        };

        var badgeBorder = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4) },
            Content = badgeLabel,
            HorizontalOptions = LayoutOptions.End
        };

        var modeLabel = new Label
        {
            FontSize = 12,
            TextColor = Color.FromArgb("#94a3b8")
        };
        modeLabel.SetBinding(Label.TextProperty, "Details");

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            RowSpacing = 4
        };

        grid.Add(pkgLabel, 0, 0);
        grid.Add(badgeBorder, 1, 0);
        grid.Add(modeLabel, 0, 1);
        Grid.SetColumnSpan(modeLabel, 2);

        Content = grid;

        BindingContextChanged += (s, e) =>
        {
            if (BindingContext is AuditResult res)
            {
                badgeLabel.Text = res.Tier switch
                {
                    Tier.Safe => "TIER_1 SAFE",
                    Tier.Review => "TIER_2 REVIEW",
                    Tier.Heuristic => "HEURISTIC",
                    Tier.Protected => "PROTECTED",
                    _ => "UNIDENTIFIED"
                };

                badgeBorder.BackgroundColor = res.Tier switch
                {
                    Tier.Safe => Color.FromArgb("#16a34a"),
                    Tier.Review => Color.FromArgb("#2563eb"),
                    Tier.Heuristic => Color.FromArgb("#d97706"),
                    Tier.Protected => Color.FromArgb("#dc2626"),
                    _ => Color.FromArgb("#64748b")
                };
            }
        };
    }
}
