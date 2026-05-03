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

		// Hit testing model:
		//   * Passthrough is left ENABLED so taps outside any HUD element fall to the app.
		//   * EnableDrawableTouchHandling makes the platform overlay consult our
		//     IWindowOverlayElement.Contains() to decide whether to capture a tap.
		//
		// When collapsed: only GoHudPill.Contains returns true on its rect.
		//   App taps elsewhere flow through to the user's UI.
		// When expanded: GoHudPanel.Contains returns true for the entire scrim
		//   (== dirtyRect), capturing every tap and routing via OnTapped.
		//
		// This avoids toggling DisableUITouchEventPassthrough on every expand/collapse,
		// which on iOS leaves the gesture recognizer in a state where the pill
		// stops responding to taps after the first round-trip.
		DisableUITouchEventPassthrough = false;
		EnableDrawableTouchHandling = true;
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

		// Always invalidate on any property change so the canvas redraws.
		Invalidate();
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
