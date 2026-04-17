using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace test_go_app;

public class DashboardPage : View
{
    readonly Reactive<double> progress = new(0.5);
    readonly Reactive<DateTime?> selectedDate = new(DateTime.Today);
    readonly Reactive<double> stepperValue = new(0.0);

    [Body]
    View body() =>
        VStack(16f,
            Text("Dashboard")
                .FontSize(28)
                .FontWeight(FontWeight.Bold)
                .Color(Colors.DarkBlue)
                .HorizontalTextAlignment(TextAlignment.Center),

            Grid(
                new object[] { "*", "*", "*" },
                new object[] { "*", "*" },

                StatCard("Sales", $"{(progress.Value * 100):F0}%", Colors.CornflowerBlue)
                    .GridRow(0).GridColumn(0),

                StatCard("Users", "1,234", Colors.MediumSeaGreen)
                    .GridRow(0).GridColumn(1),

                StatCard("Steps", $"{stepperValue.Value:F0}", Colors.Teal)
                    .GridRow(0).GridColumn(2),

                StatCard("Date", selectedDate.Value?.ToString("MMM dd") ?? "N/A", Colors.Coral)
                    .GridRow(1).GridColumn(0),

                StatCard("Growth", "+12%", Colors.MediumPurple)
                    .GridRow(1).GridColumn(1).GridColumnSpan(2)
            )
            .Frame(height: 300),

            HStack(12f,
                Button("+ Sales", () =>
                {
                    progress.Value = Math.Min(1.0, progress.Value + 0.1);
                })
                    .Color(Colors.White)
                    .Background(new SolidPaint(Colors.CornflowerBlue))
                    .CornerRadius(8)
                    .Frame(height: 40),
                Stepper(stepperValue, maximum: 50.0, minimum: 0.0, interval: 5.0)
            )
        )
        .Padding(new Thickness(24));

    View StatCard(string title, string value, Color color) =>
        Border(
            VStack(8f,
                Text(title)
                    .FontSize(14)
                    .Color(Colors.White)
                    .HorizontalTextAlignment(TextAlignment.Center),
                Text(value)
                    .FontSize(28)
                    .FontWeight(FontWeight.Bold)
                    .Color(Colors.White)
                    .HorizontalTextAlignment(TextAlignment.Center)
            )
            .Padding(new Thickness(12))
        )
        .Background(color)
        .CornerRadius(8);
}
