using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace test_go_app;

public class DashboardPage : View
{
    readonly Reactive<double> progress = new(0.0);
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

                Border(
                    VStack(8f,
                        Text("Sales")
                            .FontSize(20)
                            .Color(Colors.White)
                            .HorizontalTextAlignment(TextAlignment.Center),
                        ProgressBar(progress),
                        Text(() => $"{(progress.Value * 100):F0}%")
                            .FontSize(14)
                            .Color(Colors.White)
                            .HorizontalTextAlignment(TextAlignment.Center),
                        Button("+ 10%", () =>
                        {
                            progress.Value = Math.Min(1.0, progress.Value + 0.1);
                        })
                            .Color(Colors.White)
                            .CornerRadius(4)
                            .Frame(height: 32)
                    )
                )
                .Background(Colors.CornflowerBlue)
                .CornerRadius(8)
                .Padding(new Thickness(12))
                .GridRow(0).GridColumn(0),

                Border(
                    Text("Users")
                        .FontSize(20)
                        .Color(Colors.White)
                        .HorizontalTextAlignment(TextAlignment.Center)
                )
                .Background(Colors.MediumSeaGreen)
                .CornerRadius(8)
                .Padding(new Thickness(12))
                .GridRow(0).GridColumn(1),

                Border(
                    VStack(8f,
                        Text("Stepper")
                            .FontSize(16)
                            .Color(Colors.White)
                            .HorizontalTextAlignment(TextAlignment.Center),
                        Text(() => $"{stepperValue.Value:F0}")
                            .FontSize(24)
                            .FontWeight(FontWeight.Bold)
                            .Color(Colors.White)
                            .HorizontalTextAlignment(TextAlignment.Center),
                        Stepper(stepperValue, maximum: 50.0, minimum: 0.0, interval: 5.0)
                    )
                )
                .Background(Colors.Teal)
                .CornerRadius(8)
                .Padding(new Thickness(12))
                .GridRow(0).GridColumn(2),

                Border(
                    VStack(8f,
                        Text("Date")
                            .FontSize(16)
                            .Color(Colors.White)
                            .HorizontalTextAlignment(TextAlignment.Center),
                        DatePicker(selectedDate,
                            minimumDate: new DateTime(2024, 1, 1),
                            maximumDate: new DateTime(2026, 12, 31))
                            .Format("yyyy-MM-dd")
                    )
                )
                .Background(Colors.Coral)
                .CornerRadius(8)
                .Padding(new Thickness(12))
                .GridRow(1).GridColumn(0),

                Border(
                    Text("Growth")
                        .FontSize(20)
                        .Color(Colors.White)
                        .HorizontalTextAlignment(TextAlignment.Center)
                )
                .Background(Colors.MediumPurple)
                .CornerRadius(8)
                .Padding(new Thickness(12))
                .GridRow(1).GridColumn(1).GridColumnSpan(2)
            )
            .Frame(height: 300)
        )
        .Padding(new Thickness(20));
}
