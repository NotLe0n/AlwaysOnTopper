using System;
using System.Runtime.InteropServices;

namespace AlwaysOnTopper;

internal static class User32
{
	internal const uint EVENT_OBJECT_INVOKED = 0x8013;
	internal const uint EVENT_OBJECT_FOCUS = 0x8005;
	internal const uint WINEVENT_OUTOFCONTEXT = 0;
	internal const uint MFT_STRING = 0x00000000;
	internal const uint MFS_CHECKED = 0x00000008;
	internal const uint MFS_UNCHECKED = 0x00000000;
	internal const int HWND_NOTOPMOST = -2;
	internal const int HWND_TOPMOST = -1;
	internal const int WS_EX_TOPMOST = 0x00000008;

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

	[StructLayout(LayoutKind.Sequential)]
	internal struct POINT
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MSG
	{
		public readonly IntPtr hwnd;
		public readonly uint message;
		public readonly UIntPtr wParam;
		public readonly IntPtr lParam;
		public readonly int time;
		public readonly POINT pt;
	}

	[DllImport("user32.dll")]
	internal static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll")]
	internal static extern bool TranslateMessage([In] ref MSG lpMsg);

	[DllImport("user32.dll")]
	internal static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern bool GetMenuItemInfo(IntPtr hMenu, uint uItem, bool fByPosition, [In, Out] MENUITEMINFO lpmii);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern bool InsertMenuItem(IntPtr hMenu, uint uItem, bool fByPosition, [In] MENUITEMINFO lpmii);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern bool SetMenuItemInfo(IntPtr hMenu, uint uItem, bool fByPosition, [In] MENUITEMINFO lpmii);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern bool RemoveMenu(IntPtr hMenu, uint uItem, bool fByPosition);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern int GetMenuItemCount(IntPtr hMenu);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
	   hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
	   uint idThread, uint dwFlags);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

	internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
		IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

	[StructLayout(LayoutKind.Sequential)]
	internal struct WINDOWINFO
	{
		public uint cbSize;
		public RECT rcWindow;
		public RECT rcClient;
		public uint dwStyle;
		public uint dwExStyle;
		public uint dwWindowStatus;
		public uint cxWindowBorders;
		public uint cyWindowBorders;
		public ushort atomWindowType;
		public ushort wCreatorVersion;

		public WINDOWINFO(bool? filler) : this() // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
		{
			cbSize = (uint)(Marshal.SizeOf(typeof(WINDOWINFO)));
		}

	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct RECT
	{
		public int Left, Top, Right, Bottom;
	}

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter,
		int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

	[Flags]
	internal enum SetWindowPosFlags : uint
	{
		/// <summary>If the calling thread and the thread that owns the window are attached to different input queues, 
		/// the system posts the request to the thread that owns the window. This prevents the calling thread from 
		/// blocking its execution while other threads process the request.</summary>
		/// <remarks>SWP_ASYNCWINDOWPOS</remarks>
		AsynchronousWindowPosition = 0x4000,
		/// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
		/// <remarks>SWP_DEFERERASE</remarks>
		DeferErase = 0x2000,
		/// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
		/// <remarks>SWP_DRAWFRAME</remarks>
		DrawFrame = 0x0020,
		/// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to 
		/// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE 
		/// is sent only when the window's size is being changed.</summary>
		/// <remarks>SWP_FRAMECHANGED</remarks>
		FrameChanged = 0x0020,
		/// <summary>Hides the window.</summary>
		/// <remarks>SWP_HIDEWINDOW</remarks>
		HideWindow = 0x0080,
		/// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the 
		/// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter 
		/// parameter).</summary>
		/// <remarks>SWP_NOACTIVATE</remarks>
		DoNotActivate = 0x0010,
		/// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid 
		/// contents of the client area are saved and copied back into the client area after the window is sized or 
		/// repositioned.</summary>
		/// <remarks>SWP_NOCOPYBITS</remarks>
		DoNotCopyBits = 0x0100,
		/// <summary>Retains the current position (ignores X and Y parameters).</summary>
		/// <remarks>SWP_NOMOVE</remarks>
		IgnoreMove = 0x0002,
		/// <summary>Does not change the owner window's position in the Z order.</summary>
		/// <remarks>SWP_NOOWNERZORDER</remarks>
		DoNotChangeOwnerZOrder = 0x0200,
		/// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to 
		/// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent 
		/// window uncovered as a result of the window being moved. When this flag is set, the application must 
		/// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
		/// <remarks>SWP_NOREDRAW</remarks>
		DoNotRedraw = 0x0008,
		/// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
		/// <remarks>SWP_NOREPOSITION</remarks>
		DoNotReposition = 0x0200,
		/// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
		/// <remarks>SWP_NOSENDCHANGING</remarks>
		DoNotSendChangingEvent = 0x0400,
		/// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
		/// <remarks>SWP_NOSIZE</remarks>
		IgnoreResize = 0x0001,
		/// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
		/// <remarks>SWP_NOZORDER</remarks>
		IgnoreZOrder = 0x0004,
		/// <summary>Displays the window.</summary>
		/// <remarks>SWP_SHOWWINDOW</remarks>
		ShowWindow = 0x0040,
	}

	[DllImport("user32.dll")]
	internal static extern IntPtr GetForegroundWindow();

	[Flags]
	internal enum MIIM
	{
		BITMAP = 0x00000080,
		CHECKMARKS = 0x00000008,
		DATA = 0x00000020,
		FTYPE = 0x00000100,
		ID = 0x00000002,
		STATE = 0x00000001,
		STRING = 0x00000040,
		SUBMENU = 0x00000004,
		TYPE = 0x00000010
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	internal class MENUITEMINFO
	{
		public int cbSize = Marshal.SizeOf(typeof(MENUITEMINFO));
		public MIIM fMask;
		public uint fType;
		public uint fState;
		public uint wID;
		public IntPtr hSubMenu;
		public IntPtr hbmpChecked;
		public IntPtr hbmpUnchecked;
		public IntPtr dwItemData;
		public string dwTypeData = null;
		public uint cch; // length of dwTypeData
		public IntPtr hbmpItem;

		public MENUITEMINFO() { }
		public MENUITEMINFO(MIIM pfMask)
		{
			fMask = pfMask;
		}
	}

	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	internal static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

	[Flags]
	internal enum MessageBoxButton : uint
	{
		AbortRetryIgnore = 0x00000002U,
		CancelTryContinue = 0x00000006U,
		Help = 0x00004000U,
		OK = 0x00000000U,
		OKCancel = 0x00000001U,
		RetryCancel = 0x00000005U,
		YesNo = 0x00000004U,
		YesNoCancel = 0x00000003U,
	}

	[Flags]
	internal enum MessageBoxIcon : uint
	{
		Exclamation = 0x00000030U,
		Warning = 0x00000030U,
		Information = 0x00000040U,
		Asteriks = 0x00000040U,
		Question = 0x00000020U,
		Stop = 0x00000010U,
		Error = 0x00000010U,
		Hand = 0x00000010U,
	}

	internal enum MessageBoxResult : int
	{
		Abort = 3,
		Cancel = 2,
		Continue = 11,
		Ignore = 5,
		No = 7,
		OK = 1,
		Retry = 4,
		TryAgain = 10,
		Yes = 6,
	}
}