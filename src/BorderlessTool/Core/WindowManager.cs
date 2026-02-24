using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using BorderlessTool.Monitors;

namespace BorderlessTool.Core;

/// <summary>
/// Provides functionality to detect game windows and apply borderless windowed mode
/// by manipulating Win32 window styles and position.
/// </summary>
public static class WindowManager
{
    /// <summary>
    /// Represents a rectangle defined by its left, top, right, and bottom edges.
    /// Used internally by Win32 APIs such as <c>GetWindowRect</c> and <c>GetMonitorInfo</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// Contains information about a display monitor, including its bounding rectangle
    /// and working area. Passed by reference to <c>GetMonitorInfo</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        /// <summary>Size of the structure in bytes. Must be set before calling GetMonitorInfo.</summary>
        public uint cbSize;

        /// <summary>The full bounding rectangle of the monitor, in virtual screen coordinates.</summary>
        public RECT rcMonitor;

        /// <summary>The working area rectangle (excludes taskbar and toolbars).</summary>
        public RECT rcWork;

        /// <summary>Flags for the monitor. 1 = primary monitor.</summary>
        public uint dwFlags;
    }

    // -------------------------------------------------------------------------
    // Window style indices (used with GetWindowLongPtr / SetWindowLongPtr)
    // -------------------------------------------------------------------------

    /// <summary>Index to retrieve or set the window style (GWL_STYLE).</summary>
    private const int GWL_STYLE = -16;

    /// <summary>Index to retrieve or set the extended window style (GWL_EXSTYLE).</summary>
    private const int GWL_EXSTYLE = -20;

    // -------------------------------------------------------------------------
    // Window style flags (WS_*)
    // These are bit flags stored in the style integer returned by GetWindowLongPtr.
    // Use bitwise AND (&) to test, bitwise OR (|) to set, bitwise AND NOT (&= ~) to clear.
    // -------------------------------------------------------------------------

    /// <summary>The window has a thin border.</summary>
    private const uint WS_BORDER = 0x00800000;

    /// <summary>The window has a title bar (implies WS_BORDER). Used to detect windowed mode.</summary>
    private const uint WS_CAPTION = 0x00C00000;

    /// <summary>The window has a sizing border (resizable). Indicates a standard windowed game.</summary>
    private const uint WS_THICKFRAME = 0x00040000;

    /// <summary>The window has a minimize button.</summary>
    private const uint WS_MINIMIZEBOX = 0x00020000;

    /// <summary>The window has a maximize button.</summary>
    private const uint WS_MAXIMIZEBOX = 0x00010000;

    /// <summary>The window has a system menu (right-click on title bar).</summary>
    private const uint WS_SYSMENU = 0x00080000;

    /// <summary>
    /// The window is a pop-up window. Typically used by borderless or fullscreen windows.
    /// Games that natively support borderless windowed often use this style.
    /// </summary>
    private const uint WS_POPUP = 0x80000000;

    /// <summary>The window is visible.</summary>
    private const uint WS_VISIBLE = 0x10000000;

    // -------------------------------------------------------------------------
    // SetWindowPos flags (SWP_*)
    // These control the behavior of the SetWindowPos call.
    // -------------------------------------------------------------------------

    /// <summary>Retains current Z-order. The hWndInsertAfter parameter is ignored.</summary>
    private const uint SWP_NOZORDER = 0x0004;

    /// <summary>Does not activate the window after repositioning.</summary>
    private const uint SWP_NOACTIVATE = 0x0010;

    /// <summary>
    /// Sends a WM_NCCALCSIZE message to the window, forcing it to recalculate
    /// its client area. Required after changing the window style via SetWindowLongPtr.
    /// </summary>
    private const uint SWP_FRAMECHANGED = 0x0020;

    /// <summary>Displays the window after repositioning.</summary>
    private const uint SWP_SHOWWINDOW = 0x0040;

    // -------------------------------------------------------------------------
    // MonitorFromWindow flags
    // -------------------------------------------------------------------------

    /// <summary>
    /// If the window does not intersect any monitor, returns the handle
    /// of the nearest monitor to the window.
    /// </summary>
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    // -------------------------------------------------------------------------
    // Win32 P/Invoke declarations
    // -------------------------------------------------------------------------

    /// <summary>Retrieves information about the specified window's style or other attributes.</summary>
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    /// <summary>Changes an attribute of the specified window, such as its style flags.</summary>
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    /// <summary>Changes the size, position, and Z-order of a window.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    /// <summary>Returns a handle to the monitor that contains the specified window.</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    /// <summary>Retrieves information about a display monitor into a <see cref="MONITORINFO"/> struct.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    /// <summary>Retrieves the bounding rectangle of a window in screen coordinates.</summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Detects the running game using <see cref="GameDetector"/> and returns its window handle (HWND).
    /// Also prints the current window style flags to the console for diagnostic purposes.
    /// </summary>
    /// <returns>
    /// The window handle of the detected game, or <see cref="IntPtr.Zero"/> if no game was found.
    /// </returns>
    public static IntPtr GetHwnd()
    {
        bool found = GameDetector.TryGetSingleGame(out GameWindowCandidate? game);

        if (found)
        {
            IntPtr hwnd = new IntPtr(game!.Hwnd);
            uint style = (uint)GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();

            Console.WriteLine($"Estilo bruto (hex): 0x{style:X8}");
            Console.WriteLine($"É popup:      {(style & WS_POPUP) != 0}");
            Console.WriteLine($"É visível:    {(style & WS_VISIBLE) != 0}");
            Console.WriteLine($"Tem borda:    {(style & WS_BORDER) != 0}");
            Console.WriteLine($"Tem título:   {(style & WS_CAPTION) != 0}");
            Console.WriteLine($"Redimension:  {(style & WS_THICKFRAME) != 0}");
            return hwnd;
        }

        Console.WriteLine("Nenhum jogo encontrado.");
        return IntPtr.Zero;
    }

    /// <summary>
    /// Applies borderless windowed mode to the specified window.
    /// <para>
    /// If the window has a standard border or title bar, those style flags are removed
    /// and the <see cref="WS_POPUP"/> flag is applied via <c>SetWindowLongPtr</c>.
    /// In both cases (with or without existing border), the window is then repositioned
    /// and resized to fill the monitor it currently occupies using <c>SetWindowPos</c>.
    /// </para>
    /// <para>
    /// Note: Some older games (e.g. Unreal Engine 3 titles) manage their own window styles
    /// and may revert changes applied by this method.
    /// </para>
    /// </summary>
    /// <param name="hwnd">The window handle of the target game window.</param>
    public static void ApplyBorderless(IntPtr hwnd)
    {
        uint style = (uint)GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();

        bool haveBorder = (style & WS_CAPTION) != 0 || (style & WS_THICKFRAME) != 0;

        uint newStyle = style;
        if (haveBorder)
        {
            // Remove all decorative border flags and replace with popup style
            newStyle &= ~WS_CAPTION;
            newStyle &= ~WS_THICKFRAME;
            newStyle &= ~WS_BORDER;
            newStyle &= ~WS_SYSMENU;
            newStyle &= ~WS_MINIMIZEBOX;
            newStyle &= ~WS_MAXIMIZEBOX;
            newStyle |= WS_POPUP;

            SetWindowLongPtr(hwnd, GWL_STYLE, new IntPtr(newStyle));
            Console.WriteLine("Borda removida.");
        }
        else
        {
            Console.WriteLine("Janela já é borderless, só vai reposicionar.");
        }

        // Resolve the monitor the window currently resides on
        IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

        var mi = new MONITORINFO();
        mi.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();
        GetMonitorInfo(hMonitor, ref mi);

        // Calculate position and size from the monitor's full bounding rectangle
        int x = mi.rcMonitor.Left;
        int y = mi.rcMonitor.Top;
        int width = mi.rcMonitor.Right - mi.rcMonitor.Left;
        int height = mi.rcMonitor.Bottom - mi.rcMonitor.Top;

        // Reposition and resize the window to fill the monitor
        // SWP_FRAMECHANGED forces the window to recalculate its frame after the style change
        SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height,
            SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);

        Console.WriteLine($"X: {x}, Y: {y}, Width: {width}, Height: {height}");
    }
}