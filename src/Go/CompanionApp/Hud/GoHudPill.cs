// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Go.CompanionApp.Hud;

/// <summary>
/// Compact status pill drawn in the top-leading corner of the window.
/// Color reflects <see cref="GoHudState.Status"/>; tapping the pill expands
/// the full HUD panel.
/// </summary>
internal sealed class GoHudPill : IWindowOverlayElement
{
	const float PillHeight = 32f;
	const float PillCornerRadius = 16f;
	const float DotRadius = 5f;
	const float HorizontalPadding = 12f;
	const float DotTextGap = 8f;
	const float MinPillWidth = 96f;
	const float MaxPillWidth = 260f;
	const float TextSize = 13f;

	// Default safe-area fallbacks; replaced on iOS/MacCatalyst by SafeAreaInsets read at draw-time.
	const float DefaultSafeTop = 50f;
	const float DefaultSafeLeading = 16f;

	readonly GoHudState _state;

	RectF _pillRect;

	public GoHudPill(GoHudState state)
	{
		_state = state ?? throw new ArgumentNullException(nameof(state));
	}

	/// <summary>
	/// Last computed hit-rect for the pill. Used by <see cref="GoHudOverlay"/>
	/// for tap routing.
	/// </summary>
	public RectF HitRect => _pillRect;

	/// <summary>
	/// Whether the pill is currently visible. Disconnected status hides the pill entirely.
	/// </summary>
	public bool IsVisible => _state.Status != GoHudConnectionStatus.Disconnected;

	public bool Contains(Point point) => IsVisible && _pillRect.Contains(point);

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		if (!IsVisible) return;

		var (safeTop, safeLeading) = GetSafeArea();

		var (text, fillColor, strokeColor, dotColor, textColor) = StyleForStatus(_state);

		canvas.SaveState();
		try
		{
			// Measure text to size the pill
			canvas.FontSize = TextSize;
			var measured = canvas.GetStringSize(text, Microsoft.Maui.Graphics.Font.Default, TextSize);
			// Pad measured width so glyph metrics differences (and Mac Catalyst's
			// stricter measurement) don't cause text to wrap inside the pill.
			float measuredText = measured.Width + 4f;
			float pillWidth = Math.Min(MaxPillWidth,
				Math.Max(MinPillWidth,
					HorizontalPadding + DotRadius * 2 + DotTextGap + measuredText + HorizontalPadding));

			float pillX = safeLeading;
			float pillY = safeTop;
			_pillRect = new RectF(pillX, pillY, pillWidth, PillHeight);

			// Pill background
			canvas.FillColor = fillColor;
			canvas.FillRoundedRectangle(_pillRect, PillCornerRadius);

			// Stroke
			canvas.StrokeColor = strokeColor;
			canvas.StrokeSize = 1f;
			canvas.DrawRoundedRectangle(_pillRect, PillCornerRadius);

			// Status dot
			float dotCenterX = pillX + HorizontalPadding + DotRadius;
			float dotCenterY = pillY + PillHeight / 2f;
			canvas.FillColor = dotColor;
			canvas.FillCircle(dotCenterX, dotCenterY, DotRadius);

			// Text
			float textX = dotCenterX + DotRadius + DotTextGap;
			float textWidth = pillWidth - (textX - pillX) - HorizontalPadding;
			canvas.FontColor = textColor;
			canvas.DrawString(
				text,
				new RectF(textX, pillY, textWidth, PillHeight),
				HorizontalAlignment.Left,
				VerticalAlignment.Center);
		}
		finally
		{
			canvas.RestoreState();
		}
	}

	static (string text, Color fill, Color stroke, Color dot, Color textColor) StyleForStatus(GoHudState s)
	{
		// All colors picked for legibility on top of arbitrary user app content.
		// Filled with high alpha so the user's UI remains identifiable behind/around.
		var darkPanel = Color.FromRgba(0x1E, 0x1E, 0x1E, 0xE6);
		var white = Colors.White;

		return s.Status switch
		{
			GoHudConnectionStatus.Connecting => (
				text: "Connecting…",
				fill: darkPanel,
				stroke: Color.FromRgb(0x66, 0x66, 0x66),
				dot: Color.FromRgb(0xAA, 0xAA, 0xAA),
				textColor: white),

			GoHudConnectionStatus.Connected => (
				text: ProjectOrConnected(s),
				fill: darkPanel,
				stroke: Color.FromRgb(0x2E, 0xA0, 0x4F),
				dot: Color.FromRgb(0x4A, 0xD6, 0x6D),
				textColor: white),

			GoHudConnectionStatus.DeltaApplied => (
				text: $"↻ Reload #{s.LastDeltaSequence}",
				fill: Color.FromRgba(0xD9, 0x73, 0x06, 0xE6),
				stroke: Color.FromRgb(0xFF, 0x95, 0x00),
				dot: white,
				textColor: white),

			GoHudConnectionStatus.Failed => (
				text: "⚠ " + Truncate(s.LastError ?? "Reload failed", 28),
				fill: Color.FromRgba(0xC9, 0x2A, 0x2A, 0xE6),
				stroke: Color.FromRgb(0xFF, 0x4D, 0x4D),
				dot: white,
				textColor: white),

			GoHudConnectionStatus.RestartRequired => (
				text: "⟳ Restart required",
				fill: Color.FromRgba(0xCC, 0x9A, 0x06, 0xE6),
				stroke: Color.FromRgb(0xFF, 0xC1, 0x07),
				dot: white,
				textColor: white),

			_ => (
				text: string.Empty,
				fill: Colors.Transparent,
				stroke: Colors.Transparent,
				dot: Colors.Transparent,
				textColor: Colors.Transparent),
		};
	}

	static string ProjectOrConnected(GoHudState s)
		=> string.IsNullOrEmpty(s.ProjectName) ? "Connected" : Truncate(s.ProjectName, 24);

	static string Truncate(string s, int max)
		=> s.Length <= max ? s : s[..(max - 1)] + "…";

	(float top, float leading) GetSafeArea()
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
							{
								var top = (float)w.SafeAreaInsets.Top;
								var leading = (float)w.SafeAreaInsets.Left;
								return (Math.Max(top, 8f), Math.Max(leading, 12f));
							}
						}
					}
				}
			}
		}
		catch { }
#endif
		return (DefaultSafeTop, DefaultSafeLeading);
	}
}
