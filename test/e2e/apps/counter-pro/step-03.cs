using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace test_go_app;

public class CounterProPage : View
{
    readonly Reactive<int> count = new(0);

    [Body]
    View body() =>
        VStack(20f,
            Text("Counter Pro")
                .FontSize(32)
                .FontWeight(FontWeight.Bold)
                .Color(Colors.DarkBlue)
                .HorizontalTextAlignment(TextAlignment.Center),

            Text(() => $"Count: {count.Value}")
                .FontSize(48)
                .HorizontalTextAlignment(TextAlignment.Center),

            Button("Count up!", () => count.Value++)
                .Color(Colors.White)
                .Background(new SolidPaint(Colors.Blue))
                .CornerRadius(12)
                .Frame(height: 50),

            Button("Reset", () => count.Value = 0)
                .Color(Colors.White)
                .Background(new SolidPaint(Colors.Gray))
                .CornerRadius(12)
                .Frame(height: 50)
        )
        .Padding(new Thickness(32))
        .Background(Colors.AliceBlue)
        .Alignment(Alignment.Center);
}
