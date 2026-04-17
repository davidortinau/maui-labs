#:package Comet

using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace DashboardApp;

public class MainPage : View
{
    readonly Reactive<double> revenue = new(48750);
    readonly Reactive<int> orders = new(342);

    View StatCard(string label, string value, string trend, Color trendColor) =>
        VStack(6f,
            Text(label).FontSize(13).Color(Color.FromArgb("#8E8E93")),
            Text(value).FontSize(28).FontWeight(FontWeight.Bold).Color(Color.FromArgb("#1C1C1E")),
            Text(trend).FontSize(12).Color(trendColor)
        ).Padding(new Thickness(16, 14)).Background(Colors.White);

    View MetricRow(string label, double pct, Color bar) =>
        VStack(8f,
            HStack(spacing: 0f,
                Text(label).FontSize(14).Color(Color.FromArgb("#1C1C1E")),
                new Spacer(),
                Text($"{pct * 100:F0}%").FontSize(14).FontWeight(FontWeight.Medium).Color(Color.FromArgb("#1C1C1E"))
            ),
            ProgressBar(pct).ProgressColor(bar).Frame(height: 6)
        ).Padding(new Thickness(16, 12)).Background(Colors.White);

    [Body]
    View body()
    {
        var bg = Color.FromArgb("#F2F2F7");
        var accent = Color.FromArgb("#007AFF");
        var green = Color.FromArgb("#34C759");
        var orange = Color.FromArgb("#FF9F0A");
        var purple = Color.FromArgb("#AF52DE");

        return ScrollView(
            VStack(16f,
                VStack(4f,
                    Text("Dashboard").FontSize(34).FontWeight(FontWeight.Bold).Color(Color.FromArgb("#1C1C1E")),
                    Text("Business Overview").FontSize(15).Color(Color.FromArgb("#8E8E93"))
                ).Padding(new Thickness(0, 60, 0, 4)),

                Grid(
                    columns: new object[] { "*", "*" },
                    rows: new object[] { "Auto", "Auto" },
                    StatCard("Revenue", $"${revenue.Value:N0}", "+12% this month", green).Cell(row: 0, column: 0),
                    StatCard("Orders", $"{orders.Value}", "+8% this week", accent).Cell(row: 0, column: 1),
                    StatCard("Avg Rating", "4.8", "1,247 reviews", orange).Cell(row: 1, column: 0),
                    StatCard("Active Users", "2,891", "+156 today", purple).Cell(row: 1, column: 1)
                ).ColumnSpacing(12f).RowSpacing(12f),

                Text("Performance").FontSize(22).FontWeight(FontWeight.Bold).Color(Color.FromArgb("#1C1C1E")).Padding(new Thickness(0, 8, 0, 0)),

                MetricRow("Conversion Rate", 0.68, accent),
                MetricRow("Customer Satisfaction", 0.92, green),
                MetricRow("Monthly Target", 0.73, orange),

                Text("Quick Actions").FontSize(22).FontWeight(FontWeight.Bold).Color(Color.FromArgb("#1C1C1E")).Padding(new Thickness(0, 8, 0, 0)),

                HStack(12f,
                    Button("+ Revenue", () => revenue.Value += 500).FontSize(15).Color(Colors.White).Background(green).CornerRadius(10).Frame(height: 44),
                    Button("+ Order", () => orders.Value++).FontSize(15).Color(Colors.White).Background(accent).CornerRadius(10).Frame(height: 44)
                )
            ).Padding(new Thickness(20, 0, 20, 40))
        ).Background(bg);
    }
}
