using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BorderlessTool.Core;

public sealed record GameWindowCandidate(
    nint Hwnd,
    int ProcessId,
    string ProcessName,
    string? ProcessPath,
    string WindowTitle
);

public static class GameDetector
{
    // DLLs que indicam uso de GPU (DirectX / Vulkan)
    private static readonly HashSet<string> GraphicsDlls = new(StringComparer.OrdinalIgnoreCase)
    {
        "d3d9.dll",
        "d3d10.dll",
        "d3d11.dll",
        "d3d12.dll",
        "dxgi.dll",
        "vulkan-1.dll",
        "opengl32.dll"
    };

    // Processos que carregam DLLs gráficas mas não são jogos
    private static readonly HashSet<string> BlacklistedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        // Windows
        "explorer", "dwm", "csrss", "svchost",
        "ApplicationFrameHost", "ShellExperienceHost",
        "SearchHost", "StartMenuExperienceHost",
        "SystemSettings", "TextInputHost", "WidgetService",
        "WindowsTerminal", "cmd", "powershell", "pwsh",

        // Dev tools
        "devenv", "Code", "msbuild",

        // Browsers
        "chrome", "msedge", "firefox", "opera", "brave",

        // Apps
        "Discord", "Spotify", "Steam", "steamwebhelper",
        "EpicGamesLauncher", "ShareX", "obs64", "obs32",

        // GPU / Overlays
        "NVIDIA Overlay", "NVIDIA Share", "nvcontainer",
        "nvoawrapperdll", "nvsphelper64", "nvidia_share",
        "RadeonSoftware", "AMDRSServ", "aaborern",
        "GameBar", "GameBarPresenceWriter",
        "MSIAfterburner", "RTSS", "RivaTunerStatisticsServer",

        // O próprio app
        "BorderlessTool"
    };

    public static bool TryGetSingleGame(out GameWindowCandidate? game)
    {
        var candidates = FindGames();

        if (candidates.Count == 0)
        {
            game = null;
            return false;
        }

        if (candidates.Count == 1)
        {
            game = candidates[0];
            return true;
        }

        nint fg = GetForegroundWindow();
        foreach (var c in candidates)
        {
            if (c.Hwnd == fg)
            {
                game = c;
                return true;
            }
        }

        game = candidates[0];
        return true;
    }

    public static IReadOnlyList<GameWindowCandidate> FindGames()
    {
        var results = new List<GameWindowCandidate>();

        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                if (BlacklistedProcesses.Contains(proc.ProcessName))
                    continue;

                if (proc.MainWindowHandle == 0)
                    continue;

                string? exePath = null;
                try { exePath = proc.MainModule?.FileName; } catch { }

                if (IsSystemPath(exePath))
                    continue;

                // Filtrar overlays/GPU tools pelo path (pega qualquer NVIDIA/AMD/Rivatuner)
                if (IsOverlayPath(exePath))
                    continue;

                if (!HasGraphicsDll(proc))
                    continue;

                string title = proc.MainWindowTitle;
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                results.Add(new GameWindowCandidate(
                    Hwnd: proc.MainWindowHandle,
                    ProcessId: proc.Id,
                    ProcessName: proc.ProcessName,
                    ProcessPath: exePath,
                    WindowTitle: title
                ));
            }
            catch
            {
            }
            finally
            {
                proc.Dispose();
            }
        }

        return results;
    }

    private static bool HasGraphicsDll(Process proc)
    {
        try
        {
            foreach (ProcessModule module in proc.Modules)
            {
                if (GraphicsDlls.Contains(module.ModuleName))
                    return true;
            }
        }
        catch { }

        return false;
    }

    private static bool IsSystemPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        return path.Contains(@"\Windows\", StringComparison.OrdinalIgnoreCase)
            || path.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOverlayPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return path.Contains(@"\NVIDIA", StringComparison.OrdinalIgnoreCase)
            || path.Contains(@"\AMD", StringComparison.OrdinalIgnoreCase)
            || path.Contains(@"\RivaTuner", StringComparison.OrdinalIgnoreCase)
            || path.Contains(@"\MSI Afterburner", StringComparison.OrdinalIgnoreCase)
            || path.Contains(@"\Xbox", StringComparison.OrdinalIgnoreCase);
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}