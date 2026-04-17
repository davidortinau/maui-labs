using Comet;
using Comet.Reactive;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace test_go_app;

public class TodoListPage : View
{
    readonly Signal<string> newItemText = new("");
    readonly Reactive<List<string>> items = new(new List<string> { "Buy groceries", "Walk the dog", "Write code" });

    [Body]
    View body() =>
        VStack(16f,
            Text("Todo List")
                .FontSize(28)
                .FontWeight(FontWeight.Bold)
                .Color(Colors.DarkBlue)
                .HorizontalTextAlignment(TextAlignment.Center),

            HStack(8f,
                TextField(newItemText, "Add a new item...")
                    .Frame(height: 44),
                Button("Add", () =>
                {
                    var text = newItemText.Value?.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        var list = new List<string>(items.Value) { text };
                        items.Value = list;
                        newItemText.Value = "";
                    }
                })
                    .Color(Colors.White)
                    .Background(new SolidPaint(Colors.Green))
                    .CornerRadius(8)
                    .Frame(width: 60, height: 44)
            ),

            VStack(4f,
                items.Value.Select((item, index) =>
                    HStack(8f,
                        Text(item)
                            .FontSize(18)
                            .Padding(new Thickness(8)),
                        Spacer(),
                        Button("X", () =>
                        {
                            var list = new List<string>(items.Value);
                            list.RemoveAt(index);
                            items.Value = list;
                        })
                            .Color(Colors.White)
                            .Background(new SolidPaint(Colors.Red))
                            .CornerRadius(4)
                            .Frame(width: 36, height: 36)
                    )
                ).ToArray()
            )
        )
        .Padding(new Thickness(20));
}
