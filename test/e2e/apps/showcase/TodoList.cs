#:package Comet

using Comet;
using Comet.Reactive;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace TodoListApp;

public class MainPage : View
{
    readonly Signal<string> inputText = new("");
    readonly Reactive<string> items = new("Buy groceries|0,Walk the dog|0,Read a book|0");

    List<(string title, bool done)> Parse()
    {
        if (string.IsNullOrEmpty(items.Value)) return new();
        return items.Value.Split(',').Select(s => {
            var p = s.Split('|');
            return (p[0], p.Length > 1 && p[1] == "1");
        }).ToList();
    }

    void Save(List<(string title, bool done)> list) =>
        items.Value = string.Join(",", list.Select(x => $"{x.title}|{(x.done ? "1" : "0")}"));

    void AddItem()
    {
        var t = inputText.Value?.Trim() ?? "";
        if (t.Length == 0) return;
        var list = Parse(); list.Add((t, false)); Save(list);
        inputText.Value = "";
    }

    void Toggle(int i) { var l = Parse(); l[i] = (l[i].title, !l[i].done); Save(l); }
    void Delete(int i) { var l = Parse(); l.RemoveAt(i); Save(l); }
    void ClearDone() { Save(Parse().Where(x => !x.done).ToList()); }

    [Body]
    View body()
    {
        var bg = Color.FromArgb("#F2F2F7");
        var card = Colors.White;
        var accent = Color.FromArgb("#007AFF");
        var red = Color.FromArgb("#FF3B30");
        var green = Color.FromArgb("#34C759");
        var text = Color.FromArgb("#1C1C1E");
        var sub = Color.FromArgb("#8E8E93");
        var all = Parse();
        var remaining = all.Count(x => !x.done);

        return VStack(spacing: 0f,
            VStack(4f,
                Text("Tasks")
                    .FontSize(34)
                    .FontWeight(FontWeight.Bold)
                    .Color(text),
                Text(() => $"{remaining} remaining")
                    .FontSize(15)
                    .Color(sub)
            ).Padding(new Thickness(20, 60, 20, 16)),

            HStack(8f,
                TextField(inputText, "Add a new task...")
                    .FontSize(16)
                    .Background(card)
                    .Padding(new Thickness(12, 10)),
                Button("+", () => AddItem())
                    .FontSize(24)
                    .Color(Colors.White)
                    .Background(accent)
                    .CornerRadius(10)
                    .Frame(width: 44, height: 44)
            ).Padding(new Thickness(20, 0, 20, 16)),

            ScrollView(
                VStack(2f,
                    all.Select((item, idx) =>
                        HStack(12f,
                            Button(item.done ? "v" : " ", () => Toggle(idx))
                                .FontSize(16)
                                .Color(Colors.White)
                                .Background(item.done ? green : sub)
                                .CornerRadius(14)
                                .Frame(width: 28, height: 28),
                            Text(item.title)
                                .FontSize(17)
                                .Color(item.done ? sub : text),
                            new Spacer(),
                            Button("X", () => Delete(idx))
                                .FontSize(14)
                                .Color(red)
                                .Background(Colors.Transparent)
                                .Frame(width: 32, height: 32)
                        )
                        .Padding(new Thickness(16, 14))
                        .Background(card)
                    ).ToArray()
                ).Padding(new Thickness(20, 0))
            ),

            Button(() => $"Clear completed ({all.Count(x => x.done)})", () => ClearDone())
                .FontSize(15)
                .Color(red)
                .Background(Colors.Transparent)
                .Padding(new Thickness(20, 16, 20, 40))
        ).Background(bg);
    }
}
