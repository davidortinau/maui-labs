// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Go.CompanionApp.Hud;

/// <summary>
/// Expanded HUD panel — modal-feeling bottom sheet with a key-value status grid
/// and a vertical list of action buttons. Hit-test rects are exposed to
/// <see cref="GoHudOverlay"/> for tap routing.
/// </summary>
internal sealed class GoHudPanel : IWindowOverlayElement
{
	const float PanelHeight = 380f;
	const float CornerRadius = 20f;
	const float HPadding = 20f;
	const float VPadding = 20f;
	const float HeaderHeight = 44f;
	const float CloseButtonSize = 36f;
	const float StatusRowHeight = 26f;
	const float ButtonHeight = 48f;
	const float ButtonGap = 10f;
	const float SectionGap = 16f;
	const float DefaultSafeBottom = 34f;

	readonly GoHudState _state;

	RectF _scrimRect;
	RectF _panelRect;
	RectF _closeButtonRect;
	readonly List<(string label, RectF rect, Action<GoHudPanel> handler)> _buttonHits = new();

	internal RectF ScrimRect => _scrimRect;
	internal RectF PanelRect => _panelRect;
	internal RectF CloseButtonRect => _closeButtonRect;

	/// <summary>
	/// Action raised when the user taps "Exit to Connect Screen".
	/// Wired by <see cref="GoHudOverlay"/>.
	/// </summary>
	internal event Action? ExitRequested;

	public GoHudPanel(GoHudState state)
	{
		_state = state ?? throw new ArgumentNullException(nameof(state));
	}

	public bool Contains(Point point)
		=> _state.IsExpanded && _scrimRect.Contains(point);

	public bool IsButtonAtPoint(Point p, out Action<GoHudPanel>? handler)
	{
		handler = null;
		if (!_state.IsExpanded) return false;
		foreach (var (_, rect, h) in _buttonHits)
		{
			if (rect.Contains(p)) { handler = h; return true; }
		}
		return false;
	}

	public bool IsCloseTapped(Point p)
		=> _state.IsExpanded && _closeButtonRect.Contains(p);

	public bool IsScrimTapped(Point p)
		=> _state.IsExpanded && _scrimRect.Contains(p) && !_panelRect.Contains(p);

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		if (!_state.IsExpanded) return;

		_scrimRect = dirtyRect;

		float safeBottom = GetSafeBottom();
		float panelTop = dirtyRect.Height - PanelHeight - safeBottom;
		_panelRect = new RectF(0, panelTop, dirtyRect.Width, PanelHeight + safeBottom);

		canvas.SaveState();
		try
		{
			// Scrim — semi-transparent darken
			canvas.FillColor = Color.FromRgba(0, 0, 0, 0x66);
			canvas.FillRectangle(_scrimRect);

			// Panel background — rounded top corners, square bottom (extends into safe area)
			canvas.FillColor = Color.FromRgba(0x1E, 0x1E, 0x1E, 0xF2);
			var path = new PathF();
			path.MoveTo(0, panelTop + CornerRadius);
			path.QuadTo(0, panelTop, CornerRadius, panelTop);
			path.LineTo(dirtyRect.Width - CornerRadius, panelTop);
			path.QuadTo(dirtyRect.Width, panelTop, dirtyRect.Width, panelTop + CornerRadius);
			path.LineTo(dirtyRect.Width, dirtyRect.Height);
			path.LineTo(0, dirtyRect.Height);
			path.Close();
			canvas.FillPath(path);

			// Header — title + close button
			float headerY = panelTop + VPadding;
			canvas.FontColor = Colors.White;
			canvas.FontSize = 18f;
			canvas.DrawString(
				"Comet Go",
				new RectF(HPadding, headerY, dirtyRect.Width - HPadding * 2 - CloseButtonSize - 8, HeaderHeight),
				HorizontalAlignment.Left, VerticalAlignment.Center);

			_closeButtonRect = new RectF(
				dirtyRect.Width - HPadding - CloseButtonSize,
				headerY + (HeaderHeight - CloseButtonSize) / 2,
				CloseButtonSize,
				CloseButtonSize);
			canvas.FillColor = Color.FromRgba(0xFF, 0xFF, 0xFF, 0x14);
			canvas.FillRoundedRectangle(_closeButtonRect, CloseButtonSize / 2);
			canvas.FontColor = Colors.White;
			canvas.FontSize = 18f;
			canvas.DrawString("✕", _closeButtonRect, HorizontalAlignment.Center, VerticalAlignment.Center);

			// Divider
			float dividerY = headerY + HeaderHeight + 6;
			canvas.StrokeColor = Color.FromRgba(0xFF, 0xFF, 0xFF, 0x22);
			canvas.StrokeSize = 1f;
			canvas.DrawLine(HPadding, dividerY, dirtyRect.Width - HPadding, dividerY);

			// Status grid
			float rowY = dividerY + SectionGap;
			rowY = DrawRow(canvas, "Status", StatusDisplay(_state), rowY, dirtyRect.Width);
			rowY = DrawRow(canvas, "Project", _state.ProjectName ?? "—", rowY, dirtyRect.Width);
			rowY = DrawRow(canvas, "Server", _state.ServerUrl, rowY, dirtyRect.Width);
			rowY = DrawRow(canvas, "Deltas", $"{_state.AppliedCount} applied · {_state.FailedCount} failed", rowY, dirtyRect.Width);

			// Last error / status text (if any)
			if (!string.IsNullOrEmpty(_state.LastError))
			{
				rowY += 4;
				canvas.FontColor = Color.FromRgb(0xFF, 0x9B, 0x9B);
				canvas.FontSize = 12f;
				var errRect = new RectF(HPadding, rowY, dirtyRect.Width - HPadding * 2, 36);
				canvas.DrawString(Truncate(_state.LastError, 200), errRect, HorizontalAlignment.Left, VerticalAlignment.Top);
				rowY += 36;
			}

			// Buttons section (anchored to bottom so layout is predictable)
			_buttonHits.Clear();
			float buttonsBottom = dirtyRect.Height - safeBottom - VPadding;
			// Reserve space for two buttons; second is a placeholder.
			float btn2Y = buttonsBottom - ButtonHeight;
			float btn1Y = btn2Y - ButtonGap - ButtonHeight;

			DrawPrimaryButton(
				canvas,
				new RectF(HPadding, btn1Y, dirtyRect.Width - HPadding * 2, ButtonHeight),
				"⟵  Exit to Connect Screen",
				enabled: true,
				p => p.ExitRequested?.Invoke());

			DrawSecondaryButton(
				canvas,
				new RectF(HPadding, btn2Y, dirtyRect.Width - HPadding * 2, ButtonHeight),
				"More tools coming soon");
		}
		finally
		{
			canvas.RestoreState();
		}
	}

	float DrawRow(ICanvas canvas, string label, string value, float y, float width)
	{
		canvas.FontColor = Color.FromRgb(0xAA, 0xAA, 0xAA);
		canvas.FontSize = 13f;
		canvas.DrawString(label, new RectF(HPadding, y, 88, StatusRowHeight), HorizontalAlignment.Left, VerticalAlignment.Center);

		canvas.FontColor = Colors.White;
		canvas.FontSize = 14f;
		canvas.DrawString(
			Truncate(value, 60),
			new RectF(HPadding + 90, y, width - HPadding * 2 - 90, StatusRowHeight),
			HorizontalAlignment.Left, VerticalAlignment.Center);
		return y + StatusRowHeight;
	}

	void DrawPrimaryButton(ICanvas canvas, RectF rect, string label, bool enabled, Action<GoHudPanel> handler)
	{
		canvas.FillColor = enabled
			? Color.FromRgba(0xFF, 0x95, 0x00, 0xFF)
			: Color.FromRgba(0xFF, 0x95, 0x00, 0x55);
		canvas.FillRoundedRectangle(rect, 12f);
		canvas.FontColor = Colors.White;
		canvas.FontSize = 16f;
		canvas.DrawString(label, rect, HorizontalAlignment.Center, VerticalAlignment.Center);
		_buttonHits.Add((label, rect, handler));
	}

	void DrawSecondaryButton(ICanvas canvas, RectF rect, string label)
	{
		// Disabled placeholder; not added to hit list.
		canvas.FillColor = Color.FromRgba(0xFF, 0xFF, 0xFF, 0x10);
		canvas.FillRoundedRectangle(rect, 12f);
		canvas.StrokeColor = Color.FromRgba(0xFF, 0xFF, 0xFF, 0x22);
		canvas.StrokeSize = 1f;
		canvas.DrawRoundedRectangle(rect, 12f);
		canvas.FontColor = Color.FromRgb(0x88, 0x88, 0x88);
		canvas.FontSize = 14f;
		canvas.DrawString(label, rect, HorizontalAlignment.Center, VerticalAlignment.Center);
	}

	static string StatusDisplay(GoHudState s) => s.Status switch
	{
		GoHudConnectionStatus.Disconnected => "Disconnected",
		GoHudConnectionStatus.Connecting => "Connecting…",
		GoHudConnectionStatus.Connected => "● Connected",
		GoHudConnectionStatus.DeltaApplied => $"↻ Reload #{s.LastDeltaSequence} applied",
		GoHudConnectionStatus.Failed => "⚠ Reload failed",
		GoHudConnectionStatus.RestartRequired => "⟳ Restart required",
		_ => "—",
	};

	static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

	static float GetSafeBottom()
	{
#if IOS || MACCATALYST
		try
		{
			var app = UIKit.UIApplication.SharedApplication;
			if (app?.ConnectedScenes is not null)
			{
				foreach (var scene in app.ConnectedScenes)
				{
					if (scene is UIKit.UIWindowScene ws)
					{
						foreach (var w in ws.Windows)
						{
							if (w.IsKeyWindow)
								return Math.Max((float)w.SafeAreaInsets.Bottom, 12f);
						}
					}
				}
			}
		}
		catch { }
#endif
		return DefaultSafeBottom;
	}
}
