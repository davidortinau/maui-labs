using Comet;
using Comet.Reactive;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace test_go_app;

public class TodoItem
{
    public string Text { get; set; } = "";
    public bool IsCompleted { get; set; }
}

public class TodoListPage : View
{
    readonly Signal<string> newItemText = new("");
    readonly Reactive<List<TodoItem>> items = new(new List<TodoItem>
    {
        new TodoItem { Text = "Buy groceries" },
        new TodoItem { Text = "Walk the dog" },
        new TodoItem { Text = "Write code" },
    });

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
                        var list = new List<TodoItem>(items.Value) { new TodoItem { Text = text } };
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
                        CheckBox(() => item.IsCompleted)
                            .OnCheckedChanged(v =>
                            {
                                var list = new List<TodoItem>(items.Value);
                                list[index] = new TodoItem { Text = item.Text, IsCompleted = v };
                                items.Value = list;
                            }),
                        Text(item.Text)
                            .FontSize(18)
                            .Color(item.IsCompleted ? Colors.Gray : Colors.Black)
                            .Padding(new Thickness(8)),
                        Spacer(),
                        Button("X", () =>
                        {
                            var list = new List<TodoItem>(items.Value);
                            list.RemoveAt(index);
                            items.Value = list;
                        })
                            .Color(Colors.White)
                            .Background(new SolidPaint(Colors.Red))
                            .CornerRadius(4)
                            .Frame(width: 36, height: 36)
                    )
                ).ToArray()
            ),

            Text(() => $"{items.Value.Count(i => !i.IsCompleted)} items remaining")
                .FontSize(14)
                .Color(Colors.Gray)
                .HorizontalTextAlignment(TextAlignment.Center)
        )
        .Padding(new Thickness(20));
}
