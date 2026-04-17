#:package Comet

using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace CalculatorApp;

public class MainPage : View
{
    readonly Reactive<string> display = new("0");
    readonly Reactive<string> subDisplay = new("");

    double accumulator = 0;
    string pendingOp = "";
    bool resetOnNext = false;

    void Append(string digit)
    {
        if (resetOnNext) { display.Value = "0"; resetOnNext = false; }
        if (digit == "." && display.Value.Contains(".")) return;
        display.Value = (display.Value == "0" && digit != ".") ? digit : display.Value + digit;
    }

    void SetOp(string op)
    {
        if (pendingOp != "") DoEquals();
        accumulator = double.Parse(display.Value);
        pendingOp = op;
        subDisplay.Value = $"{Fmt(accumulator)} {op}";
        resetOnNext = true;
    }

    void DoEquals()
    {
        if (pendingOp == "") return;
        double cur = double.Parse(display.Value);
        double r = pendingOp switch
        {
            "+" => accumulator + cur,
            "-" => accumulator - cur,
            "x" => accumulator * cur,
            "/" => cur != 0 ? accumulator / cur : 0,
            _ => cur
        };
        display.Value = Fmt(r);
        subDisplay.Value = "";
        accumulator = r;
        pendingOp = "";
        resetOnNext = true;
    }

    void DoClear() { display.Value = "0"; subDisplay.Value = ""; accumulator = 0; pendingOp = ""; resetOnNext = false; }
    void DoSign() { if (display.Value != "0") display.Value = Fmt(-double.Parse(display.Value)); }
    void DoPct() { display.Value = Fmt(double.Parse(display.Value) / 100.0); }
    static string Fmt(double v) => v == Math.Floor(v) && Math.Abs(v) < 1e15 ? v.ToString("F0") : v.ToString("G10");

    [Body]
    View body()
    {
        var dark = Color.FromArgb("#1C1C1E");
        var num = Color.FromArgb("#333333");
        var op = Color.FromArgb("#FF9F0A");
        var fn = Color.FromArgb("#A5A5A5");

        return VStack(spacing: 0f,
            Spacer(),
            Text(() => subDisplay.Value).FontSize(20).Color(Color.FromArgb("#888888")).HorizontalTextAlignment(TextAlignment.End).Padding(new Thickness(24, 0)),
            Text(() => display.Value).FontSize(64).FontWeight(FontWeight.Light).Color(Colors.White).HorizontalTextAlignment(TextAlignment.End).Padding(new Thickness(24, 0, 24, 20)),
            Grid(
                columns: new object[] { "*", "*", "*", "*" },
                rows: new object[] { 72, 72, 72, 72, 72 },
                Button("AC", () => DoClear()).FontSize(22).Color(Colors.Black).Background(fn).CornerRadius(36).Cell(row: 0, column: 0),
                Button("+/-", () => DoSign()).FontSize(22).Color(Colors.Black).Background(fn).CornerRadius(36).Cell(row: 0, column: 1),
                Button("%", () => DoPct()).FontSize(22).Color(Colors.Black).Background(fn).CornerRadius(36).Cell(row: 0, column: 2),
                Button("/", () => SetOp("/")).FontSize(28).Color(Colors.White).Background(op).CornerRadius(36).Cell(row: 0, column: 3),
                Button("7", () => Append("7")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 1, column: 0),
                Button("8", () => Append("8")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 1, column: 1),
                Button("9", () => Append("9")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 1, column: 2),
                Button("x", () => SetOp("x")).FontSize(28).Color(Colors.White).Background(op).CornerRadius(36).Cell(row: 1, column: 3),
                Button("4", () => Append("4")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 2, column: 0),
                Button("5", () => Append("5")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 2, column: 1),
                Button("6", () => Append("6")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 2, column: 2),
                Button("-", () => SetOp("-")).FontSize(28).Color(Colors.White).Background(op).CornerRadius(36).Cell(row: 2, column: 3),
                Button("1", () => Append("1")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 3, column: 0),
                Button("2", () => Append("2")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 3, column: 1),
                Button("3", () => Append("3")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 3, column: 2),
                Button("+", () => SetOp("+")).FontSize(28).Color(Colors.White).Background(op).CornerRadius(36).Cell(row: 3, column: 3),
                Button("0", () => Append("0")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 4, column: 0, colSpan: 2),
                Button(".", () => Append(".")).FontSize(28).Color(Colors.White).Background(num).CornerRadius(36).Cell(row: 4, column: 2),
                Button("=", () => DoEquals()).FontSize(28).Color(Colors.White).Background(Color.FromArgb("#34C759")).CornerRadius(36).Cell(row: 4, column: 3)
            ).ColumnSpacing(10f).RowSpacing(10f).Padding(new Thickness(14, 0, 14, 20))
        ).Background(dark);
    }
}
