# Dashboard — Hot Reload Step Progression

A dashboard with Grid layout, multiple controls, and error recovery testing. Exercises the full range of Comet controls and delta compilation features.

## Steps

| Step | Description | Expected Outcome |
|------|-------------|-----------------|
| 00 | Initial: 2×2 Grid with colored Border cells containing Text labels | Initial assembly load |
| 01 | Add `Reactive<double> progress` + ProgressBar in cell (0,0) | Delta success (new field + body edit) |
| 02 | Add a "Timer" button that increments progress by 10% | Delta success (body edit) |
| 03 | Add DatePicker in cell (1,0) | Delta success (new field + body edit) |
| 04 | Expand to 3×2 grid, add Stepper in new cell | Delta success (body rewrite) |
| 05 | Add helper method `View StatCard(string title, string value, Color color)` | Delta success (new method detected) |
| 06 | Refactor body to use `StatCard()` for each grid cell | Delta success (body rewrite using new helper) |
| 07 | **INTENTIONAL ERROR:** Use `FontWeight.SemiBold` (does not exist) | Compile error (with hint: use `FontWeight.Bold`) |
| 08 | Fix: `FontWeight.Bold` | Delta success (recovery from error) |
| 09 | Change `Reactive<double> progress` initial value from `0.0` to `0.5` | Delta success (field initializer change) |
| 10 | Final polish: add ScrollView wrapper, Reset button, spacing, margins | Delta success (10th delta in chain) |

## Usage

```bash
# Load step-00 as the initial assembly
dotnet run --project src/Go/Server/Microsoft.Maui.Go.Server.csproj -- test/e2e/apps/dashboard/step-00.cs --no-qr
```
