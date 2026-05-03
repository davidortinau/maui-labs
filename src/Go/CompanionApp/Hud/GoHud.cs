// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Comet;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Hosting;

namespace Microsoft.Maui.Go.CompanionApp.Hud;

/// <summary>
/// Process-wide handle to the Comet Go HUD. Exposed as a static so that
/// <see cref="GoMainPage"/> can <see cref="GoHudController.Bind"/> a fresh
/// <see cref="GoClient"/> without going through DI.
/// </summary>
public static class GoHud
{
	public static GoHudState State { get; } = new GoHudState();
	public static GoHudController Controller { get; } = new GoHudController(State);
	public static GoHudOverlay? Overlay { get; private set; }

	internal static void SetOverlay(GoHudOverlay overlay) => Overlay = overlay;
}

/// <summary>
/// Polls for <see cref="CometApp.CurrentWindow"/> and attaches the
/// <see cref="GoHudOverlay"/> once it's available. <see cref="CometApp.CurrentWindow"/>
/// is only set inside <c>IApplication.CreateWindow</c>, which fires after
/// <c>MauiApp.Build()</c>, so polling is required.
/// </summary>
internal static class GoHudInitializer
{
	const int MaxAttempts = 50;
	const int IntervalMs = 100;
	static int s_started;

	public static void Start()
	{
		if (System.Threading.Interlocked.CompareExchange(ref s_started, 1, 0) != 0)
			return;
		_ = AttachAsync();
	}

	static async Task AttachAsync()
	{
		for (int i = 0; i < MaxAttempts; i++)
		{
			var window = CometApp.CurrentWindow;
			if (window is not null)
			{
				try
				{
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						if (GoHud.Overlay is not null) return;
						var overlay = new GoHudOverlay(window, GoHud.Controller);
						((IWindow)window).AddOverlay(overlay);
						GoHud.SetOverlay(overlay);
						GoHud.Controller.RequestExit += () => GoMainPage.RaiseExitRequested();
					});
					return;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[GoHud] AddOverlay failed: {ex.Message}");
					return;
				}
			}
			await Task.Delay(IntervalMs);
		}
		Console.WriteLine("[GoHud] Window did not appear within 5s; HUD not attached.");
	}
}
