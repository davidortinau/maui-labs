using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace test_go_app;

public class CounterProPage : View
{
    readonly Reactive<int> count = new(0);
    readonly Reactive<bool> isDark = new(false);
    readonly Reactive<double> fontSize = new(28.0);

    [Body]
    View body() =>
        VStack(20f,
            Text("Counter Pro")
                .FontSize(32)
                .FontWeight(FontWeight.Bold)
                .Color(() => isDark.Value ? Colors.White : Colors.DarkBlue)
                .HorizontalTextAlignment(TextAlignment.Center),

            Text(() => $"Count: {count.Value}")
                .FontSize(() => fontSize.Value)
                .Color(() => isDark.Value ? Colors.LightGray : Colors.Black)
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
                .Frame(height: 50),

            HStack(10f,
                Text("Dark Mode")
                    .Color(() => isDark.Value ? Colors.White : Colors.Black),
                Toggle(isDark)
            ),

            VStack(8f,
                Text(() => $"Font Size: {fontSize.Value:F0}")
                    .FontSize(14)
                    .Color(() => isDark.Value ? Colors.White : Colors.Gray),
                Slider(fontSize, minimum: 16.0, maximum: 48.0)
            )
        )
        .Padding(new Thickness(32))
        .Background(() => isDark.Value ? Colors.DarkSlateGray : Colors.AliceBlue)
        .Alignment(Alignment.Center);
}
