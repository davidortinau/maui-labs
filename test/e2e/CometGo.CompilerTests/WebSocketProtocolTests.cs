using System.Diagnostics;
using System.Net.WebSockets;
using Microsoft.Maui.Go;
using Xunit;

namespace CometGo.CompilerTests;

/// <summary>
/// Integration tests that start a real GoDevServer subprocess, connect via
/// ClientWebSocket, and verify the binary wire protocol end-to-end.
/// </summary>
[Collection("WebSocketProtocol")]
public sealed class WebSocketProtocolTests : IAsyncLifetime
{
	const string SimpleSource = """
		using System;
		public class App
		{
			public string Title => "Hello";
		}
		""";

	readonly string _tempDir;
	readonly string _csFilePath;

	// Assigned per-test via StartServerAsync
	Process? _serverProcess;
	int _port;

	public WebSocketProtocolTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "CometGoWsTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
		_csFilePath = Path.Combine(_tempDir, "App.cs");
		File.WriteAllText(_csFilePath, SimpleSource);
	}

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		if (_serverProcess is { HasExited: false } p)
		{
			try { p.Kill(entireProcessTree: true); } catch { }
			try { await p.WaitForExitAsync(new CancellationTokenSource(5000).Token); } catch { }
		}
		_serverProcess?.Dispose();

		try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
		catch { /* best-effort cleanup */ }
	}

	// ------------------------------------------------------------------
	// Tests
	// ------------------------------------------------------------------

	[Fact]
	public async Task Connect_ReceivesWelcomeAndInitialAssembly()
	{
		_port = 19876;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		await StartServerAsync(_port, cts.Token);

		using var ws = await ConnectAsync(_port, cts.Token);
		await SendHello(ws, cts.Token);

		// Expect Welcome
		var (welcomeType, welcomePayload) = await ReceiveFrame(ws, cts.Token);
		Assert.Equal(GoMessageType.Welcome, welcomeType);

		var welcome = GoProtocol.DecodeJson<WelcomeMessage>(welcomePayload.Span);
		Assert.Equal("App", welcome.ProjectName);
		Assert.Equal("App", welcome.AssemblyName);

		// Expect InitialAssembly
		var (initType, initPayload) = await ReceiveFrame(ws, cts.Token);
		Assert.Equal(GoMessageType.InitialAssembly, initType);

		var (asmName, pe, _) = GoProtocol.DecodeInitialAssembly(initPayload.Span);
		Assert.Equal("App", asmName);
		Assert.True(pe.Length > 0, "PE bytes should not be empty");
	}

	[Fact]
	public async Task FileChange_SendsDeltaToConnectedClient()
	{
		_port = 19877;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		await StartServerAsync(_port, cts.Token);
		using var ws = await ConnectAsync(_port, cts.Token);
		await SendHello(ws, cts.Token);
		await DrainInitialMessages(ws, cts.Token); // Welcome + InitialAssembly

		// Mutate the source file
		var modified = SimpleSource.Replace("\"Hello\"", "\"Updated\"");
		await WriteFileWithFlush(_csFilePath, modified);

		// Wait for Delta
		var (type, payload) = await ReceiveFrame(ws, cts.Token);
		Assert.Equal(GoMessageType.Delta, type);

		var delta = GoProtocol.DecodeDelta(payload.Span);
		Assert.True(delta.Sequence > 0);
		Assert.True(delta.MetadataDelta.Length > 0, "MetadataDelta should not be empty");
		Assert.True(delta.ILDelta.Length > 0, "ILDelta should not be empty");
	}

	[Fact]
	public async Task CompileError_SendsCompilationErrorMessage()
	{
		_port = 19878;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		await StartServerAsync(_port, cts.Token);
		using var ws = await ConnectAsync(_port, cts.Token);
		await SendHello(ws, cts.Token);
		await DrainInitialMessages(ws, cts.Token);

		// Write broken source
		await WriteFileWithFlush(_csFilePath, "public class App { BROKEN }");

		// Expect CompilationError
		var (type, payload) = await ReceiveFrame(ws, cts.Token);
		Assert.Equal(GoMessageType.CompilationError, type);

		var errorMsg = GoProtocol.DecodeJson<CompilationErrorMessage>(payload.Span);
		Assert.NotEmpty(errorMsg.Errors);
	}

	[Fact]
	public async Task CompileError_ThenFix_SendsDelta()
	{
		_port = 19879;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));

		await StartServerAsync(_port, cts.Token);
		using var ws = await ConnectAsync(_port, cts.Token);
		await SendHello(ws, cts.Token);
		await DrainInitialMessages(ws, cts.Token);

		// Break the file
		await WriteFileWithFlush(_csFilePath, "public class App { BROKEN }");

		var (errType, _) = await ReceiveFrame(ws, cts.Token);
		Assert.Equal(GoMessageType.CompilationError, errType);

		// Fix the file (change string so we get a delta, not "no changes")
		var fixedSource = SimpleSource.Replace("\"Hello\"", "\"Fixed\"");
		await WriteFileWithFlush(_csFilePath, fixedSource);

		var (fixType, fixPayload) = await ReceiveFrame(ws, cts.Token);
		Assert.Equal(GoMessageType.Delta, fixType);

		var delta = GoProtocol.DecodeDelta(fixPayload.Span);
		Assert.True(delta.Sequence > 0);
	}

	// ------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------

	async Task StartServerAsync(int port, CancellationToken ct)
	{
		// The server executable is copied to the test output dir via ProjectReference
		var serverExe = Path.Combine(AppContext.BaseDirectory, "Microsoft.Maui.Go.Server");
		if (!File.Exists(serverExe))
			throw new FileNotFoundException($"Server executable not found at {serverExe}");

		var psi = new ProcessStartInfo
		{
			FileName = serverExe,
			Arguments = $"\"{_csFilePath}\" --port {port} --no-qr",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		_serverProcess = Process.Start(psi)
			?? throw new InvalidOperationException("Failed to start server process");

		// Wait until server prints "Waiting for connections"
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		_serverProcess.OutputDataReceived += (_, e) =>
		{
			if (e.Data?.Contains("Waiting for connections") == true)
				ready.TrySetResult();
		};
		_serverProcess.ErrorDataReceived += (_, _) => { };

		_serverProcess.BeginOutputReadLine();
		_serverProcess.BeginErrorReadLine();

		using var readyReg = ct.Register(() => ready.TrySetCanceled(ct));

		await ready.Task;
	}

	static async Task<ClientWebSocket> ConnectAsync(int port, CancellationToken ct)
	{
		var uri = new Uri($"ws://localhost:{port}{GoProtocol.DefaultPath}");

		// Server may need a moment after "Waiting" before the listener is fully ready
		for (var attempt = 0; attempt < 10; attempt++)
		{
			var ws = new ClientWebSocket();
			try
			{
				await ws.ConnectAsync(uri, ct);
				return ws;
			}
			catch (WebSocketException) when (attempt < 9)
			{
				ws.Dispose();
				await Task.Delay(300, ct);
			}
		}

		throw new InvalidOperationException("Could not connect to server");
	}

	static async Task SendHello(ClientWebSocket ws, CancellationToken ct)
	{
		var helloFrame = GoProtocol.EncodeJson(GoMessageType.Hello, new HelloMessage
		{
			DeviceId = "test-device",
			DeviceName = "IntegrationTest",
			Platform = "MacCatalyst",
			RuntimeVersion = "10.0",
			SupportsMetadataUpdate = true,
		});

		await ws.SendAsync(helloFrame, WebSocketMessageType.Binary, true, ct);
	}

	/// <summary>
	/// Reads a single length-prefixed binary frame from the WebSocket.
	/// Handles multi-chunk reads for large frames.
	/// </summary>
	static async Task<(GoMessageType Type, ReadOnlyMemory<byte> Payload)> ReceiveFrame(
		ClientWebSocket ws, CancellationToken ct)
	{
		using var ms = new MemoryStream();
		var buffer = new byte[8192];
		WebSocketReceiveResult result;

		do
		{
			result = await ws.ReceiveAsync(buffer, ct);
			if (result.MessageType == WebSocketMessageType.Close)
				throw new InvalidOperationException("Server closed the connection");
			ms.Write(buffer, 0, result.Count);
		}
		while (!result.EndOfMessage);

		return GoProtocol.ParseFrame(ms.ToArray());
	}

	/// <summary>
	/// Consume the Welcome and InitialAssembly messages that always arrive after Hello.
	/// </summary>
	static async Task DrainInitialMessages(ClientWebSocket ws, CancellationToken ct)
	{
		// Welcome
		await ReceiveFrame(ws, ct);
		// InitialAssembly
		await ReceiveFrame(ws, ct);
	}

	/// <summary>
	/// Writes content to a file in a way that reliably triggers FileSystemWatcher.
	/// Writes to a temp file then renames, just like most text editors.
	/// </summary>
	static async Task WriteFileWithFlush(string path, string content)
	{
		// Small delay to ensure debounce timer from any prior event has elapsed
		await Task.Delay(500);

		var tempPath = path + ".tmp";
		await File.WriteAllTextAsync(tempPath, content);
		File.Move(tempPath, path, overwrite: true);
	}
}
