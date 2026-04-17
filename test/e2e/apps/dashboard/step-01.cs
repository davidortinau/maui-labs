using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace test_go_app;

public class DashboardPage : View
{
    readonly Reactive<double> progress = new(0.0);

    [Body]
    View body() =>
        VStack(16f,
            Text("Dashboard")
                .FontSize(28)
                .FontWeight(FontWeight.Bold)
                .Color(Colors.DarkBlue)
                .HorizontalTextAlignment(TextAlignment.Center),

            Grid(
                new object[] { "*", "*" },
                new object[] { "*", "*" },

                Border(
                    VStack(8f,
                        Text("Sales")
                            .FontSize(20)
                            .Color(Colors.White)
                            .HorizontalTextAlignment(TextAlignment.Center),
                        ProgressBar(progress)
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
                    Text("Revenue")
                        .FontSize(20)
                        .Color(Colors.White)
                        .HorizontalTextAlignment(TextAlignment.Center)
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
                .GridRow(1).GridColumn(1)
            )
            .Frame(height: 300)
        )
        .Padding(new Thickness(20));
}
