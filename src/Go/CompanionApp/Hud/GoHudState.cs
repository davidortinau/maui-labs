// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Microsoft.Maui.Go.CompanionApp.Hud;

/// <summary>
/// Connection / hot-reload status surfaced to the HUD overlay.
/// Transitions are driven by <see cref="GoHudController"/> from
/// <see cref="GoClient"/> events.
/// </summary>
public enum GoHudConnectionStatus
{
	Disconnected,
	Connecting,
	Connected,
	DeltaApplied,
	Failed,
	RestartRequired,
}

/// <summary>
/// Observable view-model for the Comet Go HUD overlay.
/// Subscribed by <see cref="GoHudOverlay"/> to invalidate the canvas
/// when any property changes.
/// </summary>
public sealed class GoHudState : INotifyPropertyChanged
{
	GoHudConnectionStatus _status = GoHudConnectionStatus.Disconnected;
	int _lastDeltaSequence;
	int _appliedCount;
	int _failedCount;
	string? _lastError;
	string? _projectName;
	string _serverUrl = string.Empty;
	string _statusText = string.Empty;
	bool _isExpanded;
	DateTime _statusAtUtc = DateTime.UtcNow;

	public GoHudConnectionStatus Status
	{
		get => _status;
		set { if (_status != value) { _status = value; _statusAtUtc = DateTime.UtcNow; Raise(); Raise(nameof(StatusAtUtc)); } }
	}

	public int LastDeltaSequence
	{
		get => _lastDeltaSequence;
		set { if (_lastDeltaSequence != value) { _lastDeltaSequence = value; Raise(); } }
	}

	public int AppliedCount
	{
		get => _appliedCount;
		set { if (_appliedCount != value) { _appliedCount = value; Raise(); } }
	}

	public int FailedCount
	{
		get => _failedCount;
		set { if (_failedCount != value) { _failedCount = value; Raise(); } }
	}

	public string? LastError
	{
		get => _lastError;
		set { if (_lastError != value) { _lastError = value; Raise(); } }
	}

	public string? ProjectName
	{
		get => _projectName;
		set { if (_projectName != value) { _projectName = value; Raise(); } }
	}

	public string ServerUrl
	{
		get => _serverUrl;
		set { if (_serverUrl != value) { _serverUrl = value ?? string.Empty; Raise(); } }
	}

	/// <summary>
	/// Free-form status text from <see cref="GoClient.StatusChanged"/>.
	/// Shown on the expanded panel; not used for the pill (which colors by Status).
	/// </summary>
	public string StatusText
	{
		get => _statusText;
		set { if (_statusText != value) { _statusText = value ?? string.Empty; Raise(); } }
	}

	public bool IsExpanded
	{
		get => _isExpanded;
		set { if (_isExpanded != value) { _isExpanded = value; Raise(); } }
	}

	public DateTime StatusAtUtc => _statusAtUtc;

	public event PropertyChangedEventHandler? PropertyChanged;

	void Raise([CallerMemberName] string? name = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name!));
}
