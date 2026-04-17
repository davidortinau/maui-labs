using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace test_go_app;

public class MainPage : View
{
    readonly Reactive<int> count = new(0);

    [Body]
    View body() =>
        VStack(20,
            Text("Yo, this is the test-go-app!")
                .FontSize(28)
                .FontWeight(FontWeight.Bold)
                .Color(Colors.Orange)
                .HorizontalTextAlignment(TextAlignment.Center),

            Text(() => $"Count: {count.Value}")
                .FontSize(22)
                .HorizontalTextAlignment(TextAlignment.Center),

            Button("Tap me!", () => count.Value++)
                .Color(Colors.White)
                .Background(new SolidPaint(Colors.Orange))
                .CornerRadius(12)
                .Frame(height: 50),

            Text("Edit MainPage.cs and save -- the UI updates live!")
                .FontSize(14)
                .Color(Colors.Gray)
                .HorizontalTextAlignment(TextAlignment.Center)
        )
        .Padding(new Thickness(32))
        .Alignment(Alignment.Center);
}
