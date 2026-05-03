// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Threading;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Go.CompanionApp.Hud;

/// <summary>
/// Top-level cross-platform <see cref="WindowOverlay"/> that hosts the Comet Go
/// HUD pill and expanded panel. Subscribes to <see cref="GoHudState"/> and to the
/// underlying <see cref="GoHudController"/> for invalidate + delta auto-fade.
/// </summary>
public sealed class GoHudOverlay : WindowOverlay
{
	readonly GoHudController _controller;
	readonly GoHudState _state;
	readonly GoHudPill _pill;
	readonly GoHudPanel _panel;

	System.Threading.Timer? _deltaFadeTimer;

	public GoHudOverlay(IWindow window, GoHudController controller) : base(window)
	{
		_controller = controller ?? throw new ArgumentNullException(nameof(controller));
		_state = controller.State;
		_pill = new GoHudPill(_state);
		_panel = new GoHudPanel(_state);

		AddWindowElement(_pill);
		AddWindowElement(_panel);

		_panel.ExitRequested += OnPanelExitRequested;

		_state.PropertyChanged += OnStateChanged;
		Tapped += OnTapped;

		SetPassthrough();
	}

	void OnStateChanged(object? sender, PropertyChangedEventArgs e)
	{
		// Auto-fade: bounce DeltaApplied → Connected after 2.5s
		if (e.PropertyName == nameof(GoHudState.Status) &&
			_state.Status == GoHudConnectionStatus.DeltaApplied)
		{
			_deltaFadeTimer?.Dispose();
			_deltaFadeTimer = new System.Threading.Timer(_ =>
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (_state.Status == GoHudConnectionStatus.DeltaApplied)
					{
						_state.Status = GoHudConnectionStatus.Connected;
					}
				});
			}, null, TimeSpan.FromMilliseconds(2500), Timeout.InfiniteTimeSpan);
		}

		if (e.PropertyName == nameof(GoHudState.IsExpanded))
		{
			SetPassthrough();
		}

		// Always invalidate on any property change so the canvas redraws.
		Invalidate();
	}

	void SetPassthrough()
	{
		// When the panel is expanded we want to capture taps (scrim + buttons).
		// When only the pill is visible we still need to receive taps on the pill,
		// but the WindowOverlay framework handles that via Tapped events with passthrough on.
		// However, with passthrough enabled, the panel's scrim taps would also go to the app
		// behind it — which we don't want. So toggle based on expanded state.
		DisableUITouchEventPassthrough = _state.IsExpanded;
	}

	void OnTapped(object? sender, WindowOverlayTappedEventArgs e)
	{
		if (_state.IsExpanded)
		{
			// Close button
			if (_panel.IsCloseTapped(e.Point))
			{
				_state.IsExpanded = false;
				return;
			}

			// Action button
			if (_panel.IsButtonAtPoint(e.Point, out var handler))
			{
				handler?.Invoke(_panel);
				return;
			}

			// Tap outside panel (on scrim) → collapse
			if (_panel.IsScrimTapped(e.Point) && !_panel.PanelRect.Contains(e.Point))
			{
				_state.IsExpanded = false;
			}
			return;
		}

		// Collapsed mode: tap pill → expand
		if (_pill.IsVisible && _pill.HitRect.Contains(e.Point))
		{
			_state.IsExpanded = true;
		}
	}

	void OnPanelExitRequested()
	{
		// Collapse first, then raise on controller so the host page can react.
		_state.IsExpanded = false;
		_controller.RaiseRequestExit();
	}
}
