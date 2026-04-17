# TodoList — Hot Reload Step Progression

A todo list app with add/remove/complete functionality, testing LINQ, helper methods, and error recovery.

## Steps

| Step | Description | Expected Outcome |
|------|-------------|-----------------|
| 00 | Initial: text input + add button + list of items using `List<string>` in state | Initial assembly load |
| 01 | Add "X" delete buttons next to each item | Delta success (body edit) |
| 02 | Switch to `TodoItem` class with `Reactive<bool>` completion state via CheckBox | Delta success (type change + body edit) |
| 03 | Add item count footer `"N items remaining"` with LINQ `.Count()` | Delta success (body edit) |
| 04 | Add conditional styling — completed items show gray text | Delta success (property change) |
| 05 | **INTENTIONAL ERROR:** Use `VStack(0, ...)` — ambiguous between `int` and `float?` overloads | Compile error (ambiguous call) |
| 06 | Fix: `VStack(spacing: 0f, ...)` — use named argument with float suffix | Delta success (recovery from error) |
| 07 | Extract item rendering into helper method `View RenderTodoItem(TodoItem item, int index)` | Delta success (new method detected) |
| 08 | Add "Clear completed" button using `.Where()` LINQ filter | Delta success (uses implicit `System.Linq`) |

## Usage

```bash
# Load step-00 as the initial assembly
dotnet run --project src/Go/Server/Microsoft.Maui.Go.Server.csproj -- test/e2e/apps/todo-list/step-00.cs --no-qr
```
