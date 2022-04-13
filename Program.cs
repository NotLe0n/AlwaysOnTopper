using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using SimpleTrayIcon;

namespace AlwaysOnTopper;

public static class Program
{
	private const int MenuId = 31337;
	private const string DefaultMenuItemName = "Always on top";
	private const int OffsetFromBottom = 1;

	private static string menuItemName;
	private static readonly Dictionary<string, string> languages = new()
	{
		{ "ru", "Поверх всех окон" },
		{ "de", "Immer im Vordergrund" }
	};

	private static readonly Dictionary<IntPtr, IntPtr> windowHandles = new();

	[STAThread]
	private static void Main()
	{
		var mutex = new Mutex(true, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, out bool createdNew);
		if (!createdNew)
			return;

		SetupTrayIcon(mutex, out TrayMenu trayIcon);

		try {
			CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
			if (!languages.TryGetValue(currentCulture.TwoLetterISOLanguageName, out menuItemName!)) {
				menuItemName = DefaultMenuItemName;
			}

			User32.SetWinEventHook(User32.EVENT_OBJECT_FOCUS, User32.EVENT_OBJECT_FOCUS, IntPtr.Zero, WinEventObjectFocus, 0, 0, User32.WINEVENT_OUTOFCONTEXT);

			while (User32.GetMessage(out var message, IntPtr.Zero, 0, 0) > 0) {
				User32.TranslateMessage(ref message);
				User32.DispatchMessage(ref message);
			}
		}
		catch (Exception ex) {
			try {
				ShowError($"{ex.GetType()}: {ex.Message}{ex.StackTrace}");
				File.WriteAllText("exception.txt", $"{ex.GetType()}: {ex.Message}{ex.StackTrace}");
			}
			catch { }
		}
		finally {
			Exit(mutex);
			trayIcon.Dispose();
		}
	}

	private static void SetupTrayIcon(Mutex mutex, out TrayMenu trayIcon)
	{
		trayIcon = new TrayMenu(SystemIcons.WinLogo, "Always on Topper Options");
		var exitButton = new TrayMenuItem() { Content = "Exit" };
		exitButton.Click += (sender, args) =>
		{
			Exit(mutex);
			Environment.Exit(0);
		};

		var processes = Process.GetProcesses();
		for (int i = 0; i < processes.Length; i++) {
			// if the process doesn't have a window
			if (processes[i].MainWindowHandle == IntPtr.Zero) {
				continue;
			}

			var processButton = new TrayMenuItem() { Content = processes[i].ProcessName, IsChecked = IsTopmost(processes[i].MainWindowHandle) };
			processButton.Click += (sender, args) =>
			{
				var btn = (sender as TrayMenuItem);
				if (btn is null)
					return;

				var proc = Process.GetProcessesByName(btn.Content)[0];
				if (User32.SetWindowPos(proc.MainWindowHandle, IsTopmost(proc.MainWindowHandle) ? User32.HWND_NOTOPMOST : User32.HWND_TOPMOST, 0, 0, 0, 0, User32.SetWindowPosFlags.IgnoreMove | User32.SetWindowPosFlags.IgnoreResize)) {
					btn.IsChecked = !btn.IsChecked;
				}
			};

			trayIcon.Items.Add(processButton);
		}

		trayIcon.Items.Add(new TrayMenuSeparator());
		trayIcon.Items.Add(exitButton);
		trayIcon.Show();
	}

	private static void Exit(Mutex mutex)
	{
		mutex.ReleaseMutex();
		foreach (var hwnd in windowHandles.Values) {
			UpdateAlwaysOnTopToMenu(hwnd, remove: true);
		}
	}

	private static bool IsTopmost(IntPtr hwnd)
	{
		var info = new User32.WINDOWINFO(true);
		User32.GetWindowInfo(hwnd, ref info);
		return (info.dwExStyle & User32.WS_EX_TOPMOST) != 0;
	}

	private static void UpdateAlwaysOnTopToMenu(IntPtr windowHwnd, bool remove = false)
	{
		IntPtr sysMenu;
		int count;

		sysMenu = User32.GetSystemMenu(windowHwnd, false);
		if ((count = User32.GetMenuItemCount(sysMenu)) < 0) // Check if menu already modified
		{
			sysMenu = User32.GetSystemMenu(windowHwnd, true);

			if ((count = User32.GetMenuItemCount(sysMenu)) < 0) {
				sysMenu = User32.GetSystemMenu(windowHwnd, false);

				if ((count = User32.GetMenuItemCount(sysMenu)) < 0)
					return;
			}
		}

		// Calculate target position
		uint position = (uint)Math.Max(0, count - OffsetFromBottom);

		// Check if it's already our menu item
		var item = new User32.MENUITEMINFO(User32.MIIM.STATE | User32.MIIM.FTYPE | User32.MIIM.ID | User32.MIIM.STRING);
		item.dwTypeData = new string(' ', 64);
		item.cch = (uint)item.dwTypeData.Length;

		if (!User32.GetMenuItemInfo(sysMenu, (uint)Math.Max(0, (int)position - 1), true, item))
			return;

		// Need to add new menu item?
		bool newItem = item.dwTypeData != menuItemName && item.wID != MenuId;
		uint state = IsTopmost(windowHwnd) ? User32.MFS_CHECKED : User32.MFS_UNCHECKED;

		// Need to update menu item?
		bool updateItem = !newItem && (
				(state & (User32.MFS_CHECKED | User32.MFS_UNCHECKED))
				!= (item.fState & (User32.MFS_CHECKED | User32.MFS_UNCHECKED))
			);

		if (remove) {
			if (!newItem) {
				// If menu item exists
				//RemoveMenu(sysMenu, (uint)Math.Max(0, (int)position - 1), true);
				User32.GetSystemMenu(windowHwnd, true); // Reset menu
			}
		}
		else if (newItem || updateItem) {
			item = new User32.MENUITEMINFO(User32.MIIM.STATE | User32.MIIM.FTYPE | User32.MIIM.ID | User32.MIIM.STRING);
			item.fType = User32.MFT_STRING;
			item.dwTypeData = menuItemName;
			item.cch = (uint)item.dwTypeData.Length;
			item.fState = state;
			item.wID = MenuId;

			if (newItem) {
				User32.InsertMenuItem(sysMenu, position, true, item); // Add menu item
			}
			else if (updateItem) {
				User32.SetMenuItemInfo(sysMenu, (uint)Math.Max(0, (int)position - 1), true, item);  // Update menu item
			}
		}

		if (remove) // Deattach hook?
		{
			foreach (var handle in windowHandles) {
				if (handle.Value == windowHwnd) {
					var hook = handle.Value;

					User32.UnhookWinEvent(hook);
					windowHandles.Remove(hook);
				}
			}
		}
		else {
			// Attach hook to target window
			HookWindow(windowHwnd);
		}
	}

	private static void HookWindow(IntPtr windowHwnd)
	{
		// return if it already exists
		if (windowHandles.ContainsValue(windowHwnd))
			return;

		// Attach hook to target window
		var hhook = User32.SetWinEventHook(User32.EVENT_OBJECT_INVOKED, User32.EVENT_OBJECT_INVOKED, windowHwnd, WinEventObjectInvoked, 0, 0, User32.WINEVENT_OUTOFCONTEXT);
		if (hhook != IntPtr.Zero) {
			windowHandles[hhook] = windowHwnd; // add to windowHandles
		}
	}

	private static void WinEventObjectInvoked(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
	{
		if (idChild != MenuId)
			return;

		if (!windowHandles.TryGetValue(hWinEventHook, out IntPtr windowHwnd))
			return;

		if (User32.GetForegroundWindow() != windowHwnd)
			return;

		User32.SetWindowPos(windowHwnd, IsTopmost(windowHwnd) ? User32.HWND_NOTOPMOST : User32.HWND_TOPMOST, 0, 0, 0, 0, User32.SetWindowPosFlags.IgnoreMove | User32.SetWindowPosFlags.IgnoreResize);
		UpdateAlwaysOnTopToMenu(windowHwnd); // Update menu
	}

	private static void WinEventObjectFocus(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
	{
		UpdateAlwaysOnTopToMenu(User32.GetForegroundWindow());
	}

	private static User32.MessageBoxResult ShowError(string message)
	{
		return (User32.MessageBoxResult)User32.MessageBox(IntPtr.Zero, message, "AlwaysOnTopper error", (uint)User32.MessageBoxButton.OK | (uint)User32.MessageBoxIcon.Error);
	}
}
