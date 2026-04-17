# Plan A: Automated E2E Test Harness + Server Hardening

**Objective:** Make Comet Go survive a 30-minute unbroken coding session where a user incrementally builds a featureful app via hot reload. Catch every class of breakage with automated E2E tests.

---

## 1. Sample Applications (Built Incrementally via Hot Reload)

Each app starts minimal and grows through a sequence of file edits that the test harness applies programmatically. Each edit step is a discrete `.cs` file snapshot written to disk, triggering the file watcher.

### 1.1 App A: "CounterPro" — State + Layout Progression

**Exercises:** `Reactive<T>`, `VStack`, `HStack`, `Text`, `Button`, `Toggle`, `Slider`, `Spacer`, expression-body `[Body]` methods, lambda closures over reactive state.

| Step | Edit Description | What Changes | Validates |
|------|-----------------|--------------|-----------|
| A0 | Initial: counter with a single button | `Reactive<int> count`, `VStack { Text, Button }` | Initial assembly load, basic rendering |
| A1 | Change button label from "Tap me!" to "Count up!" | String literal in method body | Simplest possible hot reload delta |
| A2 | Add a "Reset" button below the counter | New `Button` added to VStack children | Method body edit with new control |
| A3 | Change VStack background to `Colors.LightBlue` | Add `.Background()` call | Property chain edit |
| A4 | Add `Reactive<bool> isDark` and a `Toggle` that switches background | New field + conditional expression in body | Lambda closure over new reactive field |
| A5 | Add `Slider` bound to font size (16–48) | `Reactive<double> fontSize`, bind to `.FontSize()` | Reactive double binding, slider UI |
| A6 | Wrap in `ScrollView` with `HStack` header | Structural layout change (body method rewrite) | Complex body method rewrite |
| A7 | **Intentional compile error:** remove closing `}` | Broken syntax | Error forwarding to companion, error banner appears |
| A8 | Fix the compile error | Restore `}` | Recovery after error, error banner clears, UI resumes |
| A9 | Add `Math.Max(count.Value, 0)` without `using System;` | Missing import | Tests whether server's implicit usings cover `System` |

**File:** `test/e2e/apps/counter-pro/step-{N}.cs`

```csharp
// step-0.cs (A0 — initial)
using Comet;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using static Comet.CometControls;

namespace CounterPro;

public class MainPage : View
{
    readonly Reactive<int> count = new(0);

    [Body]
    View body() =>
        VStack(20,
            Text(() => $"Count: {count.Value}")
                .FontSize(28)
                .FontWeight(FontWeight.Bold)
                .HorizontalTextAlignment(TextAlignment.Center),

            Button("Tap me!", () => count.Value++)
                .Color(Colors.White)
                .Background(new SolidPaint(Colors.Purple))
                .CornerRadius(12)
                .Frame(height: 50)
        )
        .Padding(new Thickness(32))
        .Alignment(Alignment.Center);
}
```

```csharp
// step-7.cs (A7 — intentional compile error)
// ... same as step-6 but with missing closing brace
// The last } is removed, causing CS1513
```

### 1.2 App B: "TodoList" — Collections + Complex State

**Exercises:** `SignalList<T>`, `ScrollView`, `ForEach`-style patterns, `TextField`, `Button` actions that mutate collections, `HStack` with delete buttons, conditional rendering.

| Step | Edit Description | What Changes | Validates |
|------|-----------------|--------------|-----------|
| B0 | Initial: text field + add button + list of items | `SignalList<string>`, `TextField`, `Button`, VStack of Text items | Collection rendering baseline |
| B1 | Add "X" delete buttons next to each item | HStack per item with Button | Body edit with nested lambda closures |
| B2 | Add `Reactive<int>` completed counter | New state + display | New field + lambda reads |
| B3 | Add empty-state message: "No todos yet!" | Conditional `if (todos.Count == 0)` | Conditional rendering in body |
| B4 | Style completed items with strikethrough | `TextDecoration.Strikethrough` | Property API usage |
| B5 | **Edge case:** add `VStack(0, ...)` call | Ambiguous overload (float? vs LayoutAlignment) | Tests server-side VStack ambiguity fix |
| B6 | Fix B5 by changing to `VStack(spacing: 0, ...)` | Named argument resolves ambiguity | Recovery from ambiguity |
| B7 | Add `Action onClear = () => todos.Clear();` | `Action` type without `using System;` | Tests implicit System using |
| B8 | Rapid-fire: 3 saves in 2 seconds | Same file modified 3 times quickly | Debounce handling, no duplicate deltas |

**File:** `test/e2e/apps/todo-list/step-{N}.cs`

### 1.3 App C: "Dashboard" — Grid + Nested Layouts + Error Recovery

**Exercises:** `Grid`, `.Cell()`, `Border`, `ZStack`, `ActivityIndicator`, `ProgressBar`, `DatePicker`, computed properties, multiple hot reload cycles testing baseline chain integrity.

| Step | Edit Description | What Changes | Validates |
|------|-----------------|--------------|-----------|
| C0 | Initial: 2×2 Grid with colored `Border` cells | `Grid(rows, cols)`, `.Cell()` | Grid layout baseline |
| C1 | Add text labels inside each cell | `ZStack { Border, Text }` | Nested layout in grid cells |
| C2 | Add `ProgressBar` in cell (0,0) bound to `Reactive<double>` | Control + state binding | New control type in grid |
| C3 | Add `DatePicker` in cell (1,0) | Date control | Source-generated control |
| C4 | Expand grid to 3×2, add `ActivityIndicator` | Grid size change + new control | Larger body method rewrite |
| C5 | **Add a new helper method** `View BuildCard(string title, View content)` | New method in existing class | Tests `ComputeSemanticEdits` for new methods — CURRENTLY BROKEN |
| C6 | Use `BuildCard()` from body method | Body calls new helper | Method body + new method combo |
| C7 | **Unsupported edit:** Add a new nested class `record DashItem(string Name)` | New type definition | Tests RestartRequired message and companion handling |
| C8 | Restart server, reconnect, verify C7 state | Full restart cycle | End-to-end restart recovery |
| C9 | Continue editing from C7 state (change a label) | Simple body edit after restart | Baseline integrity after restart |
| C10 | **10th delta in sequence** — change a color | Method body edit at generation 10 | Long-lived baseline chain stability |

**File:** `test/e2e/apps/dashboard/step-{N}.cs`

---

## 2. Test Automation Approach

### 2.1 Architecture

```
┌──────────────────────┐     ┌──────────────────────┐     ┌─────────────────────┐
│   Test Orchestrator   │────▶│   Go Dev Server      │────▶│  Companion App      │
│   (C# test runner)   │     │   (subprocess)       │     │  (iOS Simulator)    │
│                      │     │   port 9000           │     │                     │
│  • writes .cs files  │     │   watches file        │     │  • loads assembly   │
│  • waits for delta   │     │   compiles deltas     │     │  • applies deltas   │
│  • screenshots/tree  │     │   broadcasts          │     │  • renders UI       │
│  • assertions        │     │                      │     │                     │
└──────────────────────┘     └──────────────────────┘     └─────────────────────┘
         │                                                          │
         │              maui-devflow MCP tools                      │
         └──────────────────────────────────────────────────────────┘
```

### 2.2 Test Runner Design

**Location:** `test/e2e/CometGoE2E/` — a standalone .NET console app or xUnit project.

#### 2.2.1 Starting the Server

```csharp
// ServerProcess.cs
public class ServerProcess : IAsyncDisposable
{
    Process _process;
    string _testAppDir;
    
    public static async Task<ServerProcess> StartAsync(string appDir, int port = 9000)
    {
        // Copy step-0.cs to appDir/MainPage.cs
        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project src/Go/Server -- {appDir} --port {port} --no-qr",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        
        // Wait for "Waiting for connections..." in stdout
        await WaitForOutputAsync(proc, "Waiting for connections", timeout: 30_000);
        return new ServerProcess { _process = proc, _testAppDir = appDir };
    }
    
    public async Task ApplyEdit(string stepFilePath)
    {
        // Copy step file content to the watched MainPage.cs
        File.Copy(stepFilePath, Path.Combine(_testAppDir, "MainPage.cs"), overwrite: true);
        // Server's FileSystemWatcher debounces (300ms) + compiles
        await Task.Delay(2000); // Wait for compile + broadcast
    }
}
```

#### 2.2.2 Starting the Companion App (iOS Simulator)

Using maui-devflow's `build` and `deploy` commands, plus the `MAUI_GO_SERVER` env var for auto-connect:

```bash
# Step 1: Boot iOS simulator (iPhone 16 Pro)
xcrun simctl boot "iPhone 16 Pro"

# Step 2: Build companion app for simulator
dotnet build src/Go/CompanionApp/ -f net11.0-ios \
  -p:_DeviceRuntime=iossimulator-arm64

# Step 3: Install to simulator
xcrun simctl install booted \
  src/Go/CompanionApp/bin/Debug/net11.0-ios/iossimulator-arm64/CompanionApp.app

# Step 4: Launch with env var for auto-connect
xcrun simctl launch --console booted \
  com.microsoft.maui.go.companion \
  --setenv MAUI_GO_SERVER=ws://localhost:9000/maui-go
```

Alternatively, via maui-devflow MCP tools:
```
maui-devflow build --project src/Go/CompanionApp --target ios-simulator
maui-devflow deploy --simulator "iPhone 16 Pro"
maui-devflow launch --env MAUI_GO_SERVER=ws://localhost:9000/maui-go
```

#### 2.2.3 Connecting the Companion App

The `MAUI_GO_SERVER` env var triggers auto-connect in `GoMainPage.Render()` (line 54). Fallback flow:

```csharp
// Using maui-devflow MCP tools for UI automation
async Task ConnectCompanionManually(string serverUrl)
{
    // 1. Find the ServerUrlField and fill it
    await MauiDevFlow.Fill("ServerUrlField", serverUrl);
    
    // 2. Tap Connect
    await MauiDevFlow.Tap("ConnectButton");
    
    // 3. Wait for status to show "Loaded:" 
    await MauiDevFlow.WaitForText("StatusLabel", "Loaded:", timeout: 15_000);
}
```

#### 2.2.4 Verifying UI After Each Delta

After each edit step, the test harness:

1. **Waits for delta acknowledgment** — monitor server stdout for `"OK (delta #N:"` 
2. **Takes a screenshot** — `maui-devflow screenshot` → save to `test/e2e/results/`
3. **Queries accessibility tree** — `maui-devflow tree` → parse for expected elements
4. **Asserts specific elements:**

```csharp
async Task AssertAfterDelta(int deltaNumber, ExpectedUI expected)
{
    // Wait for server to confirm delta sent
    await _server.WaitForOutput($"OK (delta #{deltaNumber}:", timeout: 5000);
    
    // Wait for UI to settle (re-instantiation + render)
    await Task.Delay(1000);
    
    // Screenshot for visual record
    await MauiDevFlow.Screenshot($"delta-{deltaNumber}.png");
    
    // Query accessibility tree
    var tree = await MauiDevFlow.GetAccessibilityTree();
    
    // Assert expected elements present
    foreach (var label in expected.ExpectedLabels)
        Assert.Contains(label, tree.AllText());
    
    // Assert no error banner visible (unless expected)
    if (!expected.ExpectError)
        Assert.DoesNotContain("error", tree.AllText(), StringComparer.OrdinalIgnoreCase);
}
```

#### 2.2.5 Detecting and Recovering from Crashes

```csharp
async Task<bool> IsCompanionAlive()
{
    try
    {
        // maui-devflow ping or screenshot — if it fails, app has crashed
        var screenshot = await MauiDevFlow.Screenshot("health-check.png", timeout: 5000);
        return screenshot.Success;
    }
    catch { return false; }
}

async Task RecoverFromCrash()
{
    Console.WriteLine("[E2E] Companion app crashed! Attempting recovery...");
    
    // 1. Kill the crashed app
    await MauiDevFlow.Exec("xcrun simctl terminate booted com.microsoft.maui.go.companion");
    
    // 2. Relaunch
    await MauiDevFlow.Exec("xcrun simctl launch booted com.microsoft.maui.go.companion");
    
    // 3. Wait for app to start
    await Task.Delay(3000);
    
    // 4. Reconnect
    await ConnectCompanionManually(_serverUrl);
    
    // 5. Log crash as test failure  
    _testReport.RecordCrash(_currentStep);
}
```

#### 2.2.6 Repeatable Test Suite

```csharp
// E2ETestRunner.cs — orchestrates all three apps
[TestFixture]
public class CometGoE2ETests
{
    ServerProcess _server;
    string _testAppDir;
    
    [OneTimeSetUp]
    public async Task Setup()
    {
        _testAppDir = Path.Combine(Path.GetTempPath(), "comet-go-e2e", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testAppDir);
        
        // Ensure companion app is installed on simulator
        await EnsureCompanionInstalled();
    }
    
    [Test, Order(1)]
    public async Task CounterPro_FullSession()
    {
        await RunAppSession("counter-pro", 10 /* steps A0-A9 */);
    }
    
    [Test, Order(2)]  
    public async Task TodoList_FullSession()
    {
        await RunAppSession("todo-list", 9 /* steps B0-B8 */);
    }
    
    [Test, Order(3)]
    public async Task Dashboard_FullSession()
    {
        await RunAppSession("dashboard", 11 /* steps C0-C10 */);
    }
    
    async Task RunAppSession(string appName, int totalSteps)
    {
        // Prepare app directory with step-0
        var appStepsDir = $"test/e2e/apps/{appName}";
        PrepareAppDir(_testAppDir, $"{appStepsDir}/step-0.cs");
        
        // Start server watching this directory
        _server = await ServerProcess.StartAsync(_testAppDir);
        
        // Launch + connect companion
        await LaunchAndConnectCompanion();
        
        // Verify initial load
        await AssertAfterInitialLoad(appName);
        
        // Apply each subsequent step
        for (int i = 1; i < totalSteps; i++)
        {
            var stepFile = $"{appStepsDir}/step-{i}.cs";
            var expected = LoadExpectedUI($"{appStepsDir}/expected-{i}.json");
            
            await _server.ApplyEdit(stepFile);
            
            // Check if companion is still alive
            if (!await IsCompanionAlive())
            {
                await RecoverFromCrash();
                TestContext.WriteLine($"CRASH at step {i} of {appName}");
                // Continue testing to see how many steps survive
            }
            
            await AssertAfterDelta(i, expected);
        }
        
        await _server.DisposeAsync();
    }
}
```

### 2.3 CI Integration

```yaml
# .github/workflows/comet-go-e2e.yml
name: Comet Go E2E Tests
on:
  pull_request:
    paths: ['src/Go/**']

jobs:
  e2e-ios:
    runs-on: macos-15  # Xcode 16 with iOS 18 simulators
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '11.0' }
      - name: Boot iOS Simulator
        run: xcrun simctl boot "iPhone 16 Pro"
      - name: Build & Install Companion
        run: |
          dotnet build src/Go/CompanionApp -f net11.0-ios
          xcrun simctl install booted src/Go/CompanionApp/bin/Debug/net11.0-ios/iossimulator-arm64/CompanionApp.app
      - name: Run E2E Tests
        run: dotnet test test/e2e/CometGoE2E/ --logger "trx;LogFileName=results.trx"
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: e2e-screenshots
          path: test/e2e/results/
```

---

## 3. Server-Side Fixes

### 3.1 Better Error Recovery in DeltaCompiler

**File:** `src/Go/Server/DeltaCompiler.cs`

**Problem:** When `EmitDifference` throws or produces errors, `_baseline` and `_currentCompilation` are NOT advanced — but the source on disk has changed. The next `CompileDelta()` call re-reads the changed source and tries to diff against the OLD compilation, which can produce stale/wrong edits.

**Fix: Snapshot the "last good" compilation separately from the "current on-disk" compilation.**

```csharp
// DeltaCompiler.cs — line ~171
public CompilationResult CompileDelta()
{
    if (_currentCompilation is null || _baseline is null)
        return new CompilationResult { Success = false, Errors = ["Initial compilation required first"] };

    var sourceFiles = GetSourceFiles();
    var newTrees = sourceFiles
        .Select(f => CSharpSyntaxTree.ParseText(
            ReadSourceText(f),
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
            path: f,
            encoding: System.Text.Encoding.UTF8))
        .ToArray();

    var newCompilation = CSharpCompilation.Create(
        AssemblyName,
        newTrees,
        _references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithOptimizationLevel(OptimizationLevel.Debug));

    // Check for errors FIRST
    var diagnostics = newCompilation.GetDiagnostics();
    var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
    if (errors.Length > 0)
    {
        // *** FIX: Do NOT update _currentCompilation on error ***
        // The user will fix the error and save again.
        // _currentCompilation stays at last successful state.
        return new CompilationResult
        {
            Success = false,
            Errors = errors.Select(FormatDiagnostic).ToList()
        };
    }

    // Compute edits against the LAST SUCCESSFULLY COMPILED version
    var edits = ComputeSemanticEdits(_currentCompilation, newCompilation);
    
    // ... rest of method ...
    
    // *** FIX: Only advance on SUCCESS ***
    if (emitResult.Success)
    {
        _baseline = emitResult.Baseline;
        _currentCompilation = newCompilation;
        _generation++;
    }
    // On failure, leave baseline/compilation unchanged so next save retries
}
```

**Key insight:** The current code at line 268-271 already advances only on success, which is correct. But the `ComputeSemanticEdits` comparison needs to compare against `_currentCompilation` (last good), not against some intermediate state. This is already the case. The real risk is if the compilation has errors but `GetDiagnostics()` returns no errors for some reason — we should guard with a try/catch around `EmitDifference`:

```csharp
// Wrap EmitDifference in recovery logic (around line 222-240)
EmitDifferenceResult emitResult;
try
{
    emitResult = newCompilation.EmitDifference(
        _baseline, edits, s => false,
        metadataStream, ilStream, pdbStream);
}
catch (Exception ex)
{
    // *** FIX: Log but don't corrupt state ***
    Console.WriteLine($"  [DeltaCompiler] EmitDifference threw: {ex.GetType().Name}: {ex.Message}");
    return new CompilationResult
    {
        Success = true, // compilation succeeded; edit is unsupported
        Errors = [$"Unsupported edit for hot reload: {ex.Message}"]
    };
    // Baseline and _currentCompilation are unchanged — safe to retry
}
```

### 3.2 Better Semantic Edit Detection

**File:** `src/Go/Server/DeltaCompiler.cs`, method `ComputeSemanticEdits` (line 286)

**Problem:** Only tracks `MethodDeclarationSyntax` and `PropertyDeclarationSyntax`. Misses:
- **New methods** added to existing types (line 343 has a comment but no implementation)
- **Constructor changes** (`ConstructorDeclarationSyntax`)
- **Local function changes** (inside method bodies — these are covered by method body diffs, but standalone ones aren't)
- **Expression-bodied members** with `=>` that aren't properties

**Fix: Expand `ComputeSemanticEdits` to handle new methods and constructors:**

```csharp
List<SemanticEdit> ComputeSemanticEdits(CSharpCompilation oldCompilation, CSharpCompilation newCompilation)
{
    var edits = new List<SemanticEdit>();

    // Build maps of old symbols
    var oldSymbols = new Dictionary<string, ISymbol>();
    CollectSymbols(oldCompilation, oldSymbols);

    foreach (var newTree in newCompilation.SyntaxTrees)
    {
        var newModel = newCompilation.GetSemanticModel(newTree);
        var newRoot = newTree.GetRoot();

        var oldTree = oldCompilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath == newTree.FilePath);

        if (oldTree is null)
        {
            // New file — all types are additions (existing logic is fine)
            foreach (var typeDecl in newRoot.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var newSymbol = newModel.GetDeclaredSymbol(typeDecl);
                if (newSymbol is not null)
                    edits.Add(new SemanticEdit(SemanticEditKind.Insert, null, newSymbol));
            }
            continue;
        }

        var oldModel = oldCompilation.GetSemanticModel(oldTree);
        var oldRoot = oldTree.GetRoot();

        // === METHODS (existing + FIX for new methods) ===
        var newMethods = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        var oldMethods = oldRoot.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .ToDictionary(m => GetMethodKey(oldModel, m));

        foreach (var newMethod in newMethods)
        {
            var newSymbol = newModel.GetDeclaredSymbol(newMethod);
            if (newSymbol is null) continue;

            var key = GetMethodKey(newModel, newMethod);

            if (oldMethods.TryGetValue(key, out var oldMethod))
            {
                // Existing method — check if body changed (existing logic)
                var oldBody = oldMethod.Body?.ToFullString() ?? oldMethod.ExpressionBody?.ToFullString() ?? "";
                var newBody = newMethod.Body?.ToFullString() ?? newMethod.ExpressionBody?.ToFullString() ?? "";

                if (oldBody != newBody)
                {
                    var oldSymbol = oldModel.GetDeclaredSymbol(oldMethod);
                    edits.Add(new SemanticEdit(SemanticEditKind.Update, oldSymbol, newSymbol));
                }
            }
            else
            {
                // *** FIX: NEW METHOD in existing type ***
                // Check if the containing type exists in the old compilation.
                // If so, this is a method Insert. If the type is also new, skip
                // (handled by type-level Insert above).
                var containingTypeKey = newSymbol.ContainingType?
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (containingTypeKey is not null && oldSymbols.ContainsKey(containingTypeKey))
                {
                    edits.Add(new SemanticEdit(SemanticEditKind.Insert, null, newSymbol));
                }
            }
        }

        // === CONSTRUCTORS (new) ===
        var newCtors = newRoot.DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToArray();
        var oldCtors = oldRoot.DescendantNodes().OfType<ConstructorDeclarationSyntax>()
            .ToDictionary(c => GetConstructorKey(oldModel, c));

        foreach (var newCtor in newCtors)
        {
            var newSymbol = newModel.GetDeclaredSymbol(newCtor);
            if (newSymbol is null) continue;

            var key = GetConstructorKey(newModel, newCtor);

            if (oldCtors.TryGetValue(key, out var oldCtor))
            {
                var oldBody = oldCtor.Body?.ToFullString() ?? oldCtor.ExpressionBody?.ToFullString() ?? "";
                var newBody = newCtor.Body?.ToFullString() ?? newCtor.ExpressionBody?.ToFullString() ?? "";

                if (oldBody != newBody)
                {
                    var oldSymbol = oldModel.GetDeclaredSymbol(oldCtor);
                    edits.Add(new SemanticEdit(SemanticEditKind.Update, oldSymbol, newSymbol));
                }
            }
            else
            {
                var containingTypeKey = newSymbol.ContainingType?
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (containingTypeKey is not null && oldSymbols.ContainsKey(containingTypeKey))
                    edits.Add(new SemanticEdit(SemanticEditKind.Insert, null, newSymbol));
            }
        }

        // === PROPERTIES (existing logic, unchanged) ===
        // ... existing property comparison code ...
    }

    return edits;
}

static string GetConstructorKey(SemanticModel model, ConstructorDeclarationSyntax ctor)
{
    var symbol = model.GetDeclaredSymbol(ctor);
    return symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) 
        ?? $"{ctor.Identifier.Text}..ctor";
}
```

### 3.3 Better Error Messages for Common AI-Generated Mistakes

**File:** `src/Go/Server/DeltaCompiler.cs`

**Problem:** AI tools generate `FontWeight.SemiBold` (doesn't exist — it's `FontWeight.Semibold` with lowercase 'b'), and `VStack(0, ...)` which is ambiguous. Also, missing `using System;` blocks resolution of `Action`, `Math`, `Func`, `Console`.

#### 3.3.1 Implicit Usings for Single-File Mode

Add default `using` directives that the compiler prepends:

```csharp
// In CompileInitial() and CompileDelta(), when parsing source text:
static string PrependImplicitUsings(string sourceText, bool isSingleFileMode)
{
    if (!isSingleFileMode) return sourceText;
    
    // These usings are added to every single-file compilation
    // to match what a normal MAUI project gets via ImplicitUsings
    const string implicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using Microsoft.Maui;
        global using Microsoft.Maui.Graphics;
        
        """;
    
    return implicitUsings + sourceText;
}
```

Alternatively, add a synthetic syntax tree with global usings:

```csharp
// In CompileInitial() — after building syntaxTrees array:
if (IsSingleFileMode)
{
    var implicitTree = CSharpSyntaxTree.ParseText(
        "global using System;\n" +
        "global using System.Collections.Generic;\n" +
        "global using System.Linq;\n" +
        "global using System.Threading;\n" +
        "global using System.Threading.Tasks;\n",
        path: "<implicit-usings>.cs");
    syntaxTrees = [implicitTree, .. syntaxTrees];
}
```

#### 3.3.2 Friendly Error Messages

Post-process compilation errors before sending to companion:

```csharp
// New method in DeltaCompiler.cs
static string EnhanceDiagnosticMessage(Diagnostic d)
{
    var msg = FormatDiagnostic(d);
    
    // FontWeight.SemiBold → FontWeight.Semibold
    if (msg.Contains("FontWeight") && msg.Contains("SemiBold"))
        return msg + "\n  💡 Hint: Use FontWeight.Semibold (lowercase 'b'), not SemiBold.";
    
    // VStack(0, ...) ambiguity
    if (d.Id == "CS0121" && msg.Contains("VStack"))
        return msg + "\n  💡 Hint: Use VStack(spacing: 0, ...) to avoid overload ambiguity.";
    
    // Missing System types
    if (d.Id == "CS0246" && (msg.Contains("'Action'") || msg.Contains("'Func'") || msg.Contains("'Math'")))
        return msg + "\n  💡 Hint: Add 'using System;' at the top of your file.";
    
    return msg;
}
```

#### 3.3.3 Fix the Companion App's Own FontWeight.Semibold Issue

**File:** `src/Go/CompanionApp/GoMainPage.cs`, line 139

The companion app itself uses `FontWeight.Semibold` — verify this is the correct casing. If `FontWeight` is an enum, check the actual member name. If it doesn't exist, this would fail at runtime.

```csharp
// Line 139 — verify this resolves. If Semibold doesn't exist:
.FontWeight(FontWeight.Semibold)  // ← may need to be .FontWeight(FontWeight.SemiBold)
                                   // or use the numeric value
```

### 3.4 Compilation Error Forwarding (Already Implemented — Verify)

The server already sends `CompilationError` messages (GoDevServer.cs line 342-352). The companion already handles them (GoClient.cs line 188-191). **What's missing:**

1. **Rich diagnostic info** — currently only sends the message string. Should send `FilePath`, `Line`, `Column` from the actual diagnostic:

```csharp
// In GoDevServer.CompileAndPushDelta(), line 343-349:
var errorMsg = GoProtocol.EncodeJson(GoMessageType.CompilationError, new CompilationErrorMessage
{
    Errors = result.Errors.Select(e => 
    {
        // Parse the formatted string "file(line,col): ID: message"
        // OR better: change CompilationResult to carry Diagnostic objects
        return new CompilationDiagnostic
        {
            Message = e,
            Severity = "Error",
            // TODO: Propagate structured diagnostic from DeltaCompiler
        };
    }).ToList()
});
```

**Better approach:** Change `CompilationResult.Errors` from `List<string>` to carry structured diagnostics:

```csharp
// Add to CompilationResult
public sealed class CompilationResult
{
    public bool Success { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<StructuredDiagnostic> Diagnostics { get; init; } = []; // NEW
    public byte[]? Pe { get; init; }
    public byte[]? Pdb { get; init; }
    public byte[]? MetadataDelta { get; init; }
    public byte[]? ILDelta { get; init; }
    public byte[]? PdbDelta { get; init; }
}

public record StructuredDiagnostic(
    string Id, string Message, string FilePath, 
    int Line, int Column, string Severity);
```

### 3.5 QR Scan Auto-Connect Fix

**File:** `src/Go/CompanionApp/GoMainPage.cs`, line 228-234

**Problem:** QR scan fills the URL but doesn't auto-connect. The user must tap "Connect" after scanning.

**Fix:** After QR scan sets `ServerUrl`, automatically trigger connection:

```csharp
async void OnScanQrTapped()
{
    try
    {
        var tcs = new TaskCompletionSource<string?>();
        var scannerPage = new QrScannerPage(tcs);

        if (!ModalPresenter.Present(scannerPage))
        {
            SetState(s => s.ErrorMessage = "Cannot open scanner on this platform");
            return;
        }

        var result = await tcs.Task;

        if (!string.IsNullOrEmpty(result))
        {
            SetState(s =>
            {
                s.ServerUrl = result;
                s.Status = "QR code scanned — connecting...";
                s.ErrorMessage = null;
            });
            
            // *** FIX: Auto-connect after successful QR scan ***
            await Task.Delay(300); // Brief delay for UI update
            OnConnectTapped();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[QR] Error: {ex}");
        SetState(s => s.ErrorMessage = $"Scanner error: {ex.Message}");
    }
}
```

---

## 4. Quality Metrics

### 4.1 Core Pass/Fail Criteria

| Metric | Target | How Measured |
|--------|--------|-------------|
| **App A (CounterPro) completion** | All 10 steps pass (A0–A9) | Accessibility tree assertions after each delta |
| **App B (TodoList) completion** | All 9 steps pass (B0–B8) | Accessibility tree + collection count assertions |
| **App C (Dashboard) completion** | Steps C0–C4, C7–C10 pass; C5–C6 pass after fix 3.2 | Grid layout assertions + restart recovery |
| **No companion app crashes** | 0 crashes across all 30 steps | `IsCompanionAlive()` check after each delta |
| **Error recovery** | Error banner appears on compile error, clears on fix | Steps A7→A8 and B5→B6 specifically test this |
| **RestartRequired handling** | Companion shows message, user can restart and continue | Step C7 tests this |
| **Baseline chain integrity** | Delta #10 applies successfully after 9 prior deltas | Step C10 tests this |
| **Debounce correctness** | Only 1 delta for 3 rapid saves | Step B8 — server stdout shows exactly 1 `"OK (delta"` |

### 4.2 Performance Targets

| Metric | Target | How Measured |
|--------|--------|-------------|
| **Initial compile → assembly loaded** | < 5 seconds | Server stdout timestamp → companion "Loaded" status |
| **File save → UI updated** | < 3 seconds | File write timestamp → screenshot showing new content |
| **Debounce window** | 300ms (current) | 3 saves in 500ms → only 1 delta |
| **Full E2E suite runtime** | < 10 minutes | CI job wall clock |

### 4.3 Regression Detection

Each test step produces:
1. **Screenshot** — `test/e2e/results/{app}/{step}.png` — visual diffing
2. **Accessibility tree snapshot** — `test/e2e/results/{app}/{step}.tree.json` — structural comparison
3. **Server stdout log** — `test/e2e/results/{app}/server.log` — compilation timing, delta sizes
4. **Companion console log** — `test/e2e/results/{app}/companion.log` — delta apply success/failure

**"All Green" definition:** All 3 apps complete without crashes, all expected UI elements present, no unexpected error banners, delta #10 applies cleanly, compile errors are shown and recovered from correctly.

### 4.4 Tracked Regressions / Known-Bad States

| State | Expected Behavior | Currently |
|-------|------------------|-----------|
| New method added (C5) | Hot reload applies delta with new method | ❌ `ComputeSemanticEdits` misses it — "no changes" |
| New constructor added | Hot reload applies delta | ❌ Not tracked |
| `VStack(0, ...)` | Compiles (or gives clear error) | ❌ Ambiguous overload, cryptic CS0121 |
| `FontWeight.SemiBold` | Clear hint message | ❌ Raw CS0117, no guidance |
| `using System;` missing | `Action`, `Math` resolve | ❌ CS0246 with no hint |
| QR scan | Auto-connects | ❌ Fills URL but requires manual tap |
| 10+ sequential deltas | All apply cleanly | ❓ Untested — baseline chain may degrade |
| Compile error → fix → continue | Error clears, UI resumes | ❓ Partially works but untested end-to-end |

---

## 5. Implementation Order

### Phase 1: Server Hardening (Estimated: 3 days)
1. **3.3.1** — Add implicit usings for single-file mode
2. **3.2** — Expand `ComputeSemanticEdits` for new methods + constructors
3. **3.1** — Error recovery guard (already mostly correct, add explicit protection)
4. **3.3.2** — Friendly error messages for common mistakes
5. **3.5** — QR auto-connect

### Phase 2: Test App Authoring (Estimated: 2 days)
1. Write all step files for App A (CounterPro) — 10 `.cs` files
2. Write all step files for App B (TodoList) — 9 `.cs` files  
3. Write all step files for App C (Dashboard) — 11 `.cs` files
4. Write `expected-{N}.json` assertion files for each step

### Phase 3: Test Harness (Estimated: 3 days)
1. `ServerProcess` — start/stop server, write edits, monitor stdout
2. `CompanionDriver` — launch/connect/screenshot/tree via maui-devflow
3. `E2ETestRunner` — orchestrate apps, assert, report
4. CI workflow YAML

### Phase 4: Validation (Estimated: 2 days)
1. Run full suite locally on iOS simulator
2. Fix any discovered bugs
3. Run 3× to confirm stability
4. Set up CI

**Total estimated effort: 10 engineering days**

---

## Appendix A: File Inventory

```
test/e2e/
├── apps/
│   ├── counter-pro/
│   │   ├── step-0.cs through step-9.cs
│   │   └── expected-0.json through expected-9.json
│   ├── todo-list/
│   │   ├── step-0.cs through step-8.cs
│   │   └── expected-0.json through expected-8.json
│   └── dashboard/
│       ├── step-0.cs through step-10.cs
│       └── expected-0.json through expected-10.json
├── CometGoE2E/
│   ├── CometGoE2E.csproj
│   ├── ServerProcess.cs
│   ├── CompanionDriver.cs
│   ├── E2ETestRunner.cs
│   └── AssertionHelpers.cs
├── results/           (gitignored, populated by test runs)
└── README.md

src/Go/Server/
├── DeltaCompiler.cs   (fixes 3.1, 3.2, 3.3)
└── GoDevServer.cs     (fix 3.4)

src/Go/CompanionApp/
└── GoMainPage.cs      (fix 3.5)
```

## Appendix B: Key AutomationIds for Test Automation

| AutomationId | Control | Location |
|-------------|---------|----------|
| `ScanQrButton` | Button | GoMainPage connect screen |
| `ServerUrlField` | TextField | GoMainPage connect screen |
| `ConnectButton` | Button | GoMainPage connect screen |
| `StatusLabel` | Text | GoMainPage connect screen + error state |
| `DisconnectButton` | Button | GoMainPage user view overlay |

**Note:** User-authored views from the test apps don't have AutomationIds. Assertions will use accessibility tree text matching (e.g., finding "Count: 5" in the tree).
