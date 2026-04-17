# CounterPro — Hot Reload Step Progression

A counter app that progressively adds features, testing various hot-reload scenarios.

## Steps

| Step | Description | Expected Outcome |
|------|-------------|-----------------|
| 00 | Basic counter: `Reactive<int>`, VStack, Text, Button | Initial assembly load |
| 01 | Change button label `"Tap me!"` → `"Count up!"` | Delta success (string literal change) |
| 02 | Add a "Reset" button that sets count to 0 | Delta success (body edit, new control) |
| 03 | Add `.Background(Colors.AliceBlue)` to VStack | Delta success (property chain addition) |
| 04 | Add `Reactive<bool> isDark` + Toggle for dark/light theme | Delta success (new field + body edit — may require restart for new field) |
| 05 | Add Slider bound to `Reactive<double> fontSize` (16–48) | Delta success (new state + body edit) |
| 06 | Extract counter UI into helper method `View CounterDisplay()` | Delta success (new method detected by DeltaCompiler) |
| 07 | **INTENTIONAL ERROR:** Remove closing brace — syntax error | Compile error (companion shows error) |
| 08 | Fix the syntax error (restore closing brace) | Delta success (recovery from error) |
| 09 | Use `Math.Abs(count.Value)` in display text | Delta success (tests implicit `System` using) |

## Usage

```bash
# Load step-00 as the initial assembly
dotnet run --project src/Go/Server/Microsoft.Maui.Go.Server.csproj -- test/e2e/apps/counter-pro/step-00.cs --no-qr

# Then copy step-01.cs over step-00.cs to simulate a file edit, etc.
```
