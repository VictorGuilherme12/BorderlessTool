using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using BorderlessTool.Monitors;

namespace BorderlessTool.Core;


public static class WindowManager
{
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

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    public static void GetHwnd()
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
        }
        else
        {
            Console.WriteLine("Nenhum jogo encontrado.");
            
        }
      
    }

}
