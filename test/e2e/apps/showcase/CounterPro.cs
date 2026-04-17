#:package Comet

using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace CounterProApp;

public class MainPage : View
{
    readonly Reactive<int> count = new(0);

    [Body]
    View body()
    {
        var bg = Color.FromArgb("#1C1C1E");
        var accent = Color.FromArgb("#007AFF");
        var gray = Color.FromArgb("#8E8E93");

        return VStack(spacing: 0f,
            Spacer(),

            Text(() => $"{count.Value}")
                .FontSize(120)
                .FontWeight(FontWeight.Thin)
                .Color(Colors.White)
                .HorizontalTextAlignment(TextAlignment.Center),

            Text(() => count.Value == 1 ? "tap" : "taps")
                .FontSize(20)
                .Color(gray)
                .HorizontalTextAlignment(TextAlignment.Center),

            Spacer(),

            HStack(20f,
                Button("-", () => { if (count.Value > 0) count.Value--; })
                    .FontSize(36)
                    .Color(Colors.White)
                    .Background(gray)
                    .CornerRadius(32)
                    .Frame(width: 64, height: 64),

                Button("+", () => count.Value++)
                    .FontSize(36)
                    .Color(Colors.White)
                    .Background(accent)
                    .CornerRadius(32)
                    .Frame(width: 64, height: 64)
            )
            .Padding(new Thickness(0, 0, 0, 20)),

            Button("Reset", () => count.Value = 0)
                .FontSize(17)
                .Color(accent)
                .Background(Colors.Transparent)
                .Padding(new Thickness(0, 0, 0, 60))
        )
        .Background(bg);
    }
}
