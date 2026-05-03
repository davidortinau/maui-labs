// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Maui.Go.CompanionApp.Hud;

/// <summary>
/// Bridges <see cref="GoClient"/> events onto <see cref="GoHudState"/>
/// and exposes user-initiated commands the overlay can raise (e.g. exit
/// to the connect screen).
///
/// Lifetime: one controller instance per <see cref="GoHudState"/>.
/// Bind/Unbind a <see cref="GoClient"/> over the lifetime of the app —
/// the same controller survives soft-exit/reconnect cycles.
/// </summary>
public sealed class GoHudController
{
	readonly GoHudState _state;
	GoClient? _client;

	public GoHudController(GoHudState state)
	{
		_state = state ?? throw new ArgumentNullException(nameof(state));
	}

	public GoHudState State => _state;

	/// <summary>
	/// Raised when the user taps the "Exit to Connect Screen" action.
	/// The host (GoMainPage) handles by clearing UserView/IsConnected.
	/// </summary>
	public event Action? RequestExit;

	/// <summary>
	/// Subscribe to a fresh GoClient. Idempotent — unbinds any previous one first.
	/// </summary>
	public void Bind(GoClient client, string serverUrl)
	{
		Unbind();
		_client = client ?? throw new ArgumentNullException(nameof(client));

		_state.ServerUrl = serverUrl;
		_state.Status = client.IsConnected ? GoHudConnectionStatus.Connected : GoHudConnectionStatus.Connecting;
		_state.StatusText = client.IsConnected ? "Connected" : "Connecting...";

		client.Connected += OnConnected;
		client.Disconnected += OnDisconnected;
		client.StatusChanged += OnStatusChanged;
		client.DeltaApplied += OnDeltaApplied;
		client.ErrorReceived += OnErrorReceived;
		client.RestartRequired += OnRestartRequired;
		client.AssemblyLoaded += OnAssemblyLoaded;
	}

	public void Unbind()
	{
		if (_client is null) return;

		_client.Connected -= OnConnected;
		_client.Disconnected -= OnDisconnected;
		_client.StatusChanged -= OnStatusChanged;
		_client.DeltaApplied -= OnDeltaApplied;
		_client.ErrorReceived -= OnErrorReceived;
		_client.RestartRequired -= OnRestartRequired;
		_client.AssemblyLoaded -= OnAssemblyLoaded;
		_client = null;
	}

	internal void RaiseRequestExit() => RequestExit?.Invoke();

	void OnConnected()
	{
		_state.Status = GoHudConnectionStatus.Connected;
		_state.StatusText = "Connected";
		_state.LastError = null;
	}

	void OnDisconnected()
	{
		// Treat as transient — auto-reconnect loop in GoClient will retry.
		_state.Status = GoHudConnectionStatus.Connecting;
		_state.StatusText = "Disconnected";
	}

	void OnStatusChanged(string message)
	{
		_state.StatusText = message;

		// Project name announcement — "Project: Foo"
		const string projPrefix = "Project: ";
		if (message.StartsWith(projPrefix, StringComparison.Ordinal))
		{
			_state.ProjectName = message[projPrefix.Length..];
		}

		// Reconnect / connecting transitions — only if currently in a non-failed state.
		if (_state.Status != GoHudConnectionStatus.Failed &&
			_state.Status != GoHudConnectionStatus.RestartRequired)
		{
			if (message.StartsWith("Reconnecting", StringComparison.OrdinalIgnoreCase) ||
				message.StartsWith("Connecting", StringComparison.OrdinalIgnoreCase) ||
				message.StartsWith("Connection lost", StringComparison.OrdinalIgnoreCase))
			{
				_state.Status = GoHudConnectionStatus.Connecting;
			}
		}
	}

	void OnDeltaApplied(int sequence)
	{
		_state.LastDeltaSequence = sequence;
		_state.AppliedCount += 1;
		_state.LastError = null;
		// Always advance to DeltaApplied; the overlay auto-fades back to Connected.
		_state.Status = GoHudConnectionStatus.DeltaApplied;
		_state.StatusText = $"Delta #{sequence} applied";
	}

	void OnErrorReceived(string error)
	{
		_state.LastError = error;
		_state.FailedCount += 1;
		_state.Status = GoHudConnectionStatus.Failed;
		// Keep StatusText showing the high-level state; LastError carries the detail.
		_state.StatusText = "Error";
	}

	void OnRestartRequired(string reason)
	{
		_state.LastError = reason;
		_state.Status = GoHudConnectionStatus.RestartRequired;
		_state.StatusText = "Restart required";
	}

	void OnAssemblyLoaded(System.Reflection.Assembly assembly)
	{
		// First successful load clears error history but doesn't bump applied count.
		_state.LastError = null;
		_state.Status = GoHudConnectionStatus.Connected;
		_state.StatusText = $"Loaded: {assembly.GetName().Name}";
	}
}
