using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using BorderlessTool.Monitors;

namespace BorderlessTool.Core;


public static class WindowManager
{

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    // Índices
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;

    // Flags de estilo
    private const uint WS_BORDER = 0x00800000;
    private const uint WS_CAPTION = 0x00C00000;
    private const uint WS_THICKFRAME = 0x00040000;
    private const uint WS_MINIMIZEBOX = 0x00020000;
    private const uint WS_MAXIMIZEBOX = 0x00010000;
    private const uint WS_SYSMENU = 0x00080000;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;

    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

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

    public static void ApplyBorderless(IntPtr hwnd)
    {
        uint style = (uint)GetWindowLongPtr(hwnd, GWL_STYLE).ToInt64();

        bool itsPopUp = (style & WS_POPUP) != 0;
        bool haveBorder = (style & WS_CAPTION) != 0 || (style & WS_THICKFRAME) != 0;

        if (haveBorder)
        {
            uint newStyle = style;
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

        IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

        var mi = new MONITORINFO();
        mi.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();
        GetMonitorInfo(hMonitor, ref mi);

        int x = mi.rcMonitor.Left;
        int y = mi.rcMonitor.Top;
        int width = mi.rcMonitor.Right - mi.rcMonitor.Left;
        int height = mi.rcMonitor.Bottom - mi.rcMonitor.Top;

        SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_SHOWWINDOW);


        Console.WriteLine($"X: {x}, Y: {y}, Width: {width}, Height: {height}");
    }

}
