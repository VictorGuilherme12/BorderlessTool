using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BorderlessTool.Core;

/// <summary>
/// Represents a detected game window candidate, containing the window handle,
/// process information, and window title.
/// </summary>
/// <param name="Hwnd">The Win32 window handle (HWND) of the game's main window.</param>
/// <param name="ProcessId">The operating system process ID.</param>
/// <param name="ProcessName">The name of the process (without extension), e.g. "Dishonored".</param>
/// <param name="ProcessPath">The full path to the process executable, or null if inaccessible.</param>
/// <param name="WindowTitle">The title bar text of the main window.</param>
public sealed record GameWindowCandidate(
    nint Hwnd,
    int ProcessId,
    string ProcessName,
    string? ProcessPath,
    string WindowTitle
);

/// <summary>
/// Detects running game processes by scanning all active processes and filtering
/// based on graphics API usage, window presence, path heuristics, and a blacklist.
/// </summary>
public static class GameDetector
{
    /// <summary>
    /// Set of graphics-related DLL names that indicate a process is using a GPU API.
    /// A process must load at least one of these to be considered a game candidate.
    /// </summary>
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

    /// <summary>
    /// Set of process names that are known to load graphics DLLs but are not games.
    /// Processes in this list are excluded from detection regardless of their loaded modules.
    /// </summary>
    private static readonly HashSet<string> BlacklistedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        // Windows system processes
        "explorer", "dwm", "csrss", "svchost",
        "ApplicationFrameHost", "ShellExperienceHost",
        "SearchHost", "StartMenuExperienceHost",
        "SystemSettings", "TextInputHost", "WidgetService",
        "WindowsTerminal", "cmd", "powershell", "pwsh",

        // Development tools
        "devenv", "Code", "msbuild",

        // Browsers
        "chrome", "msedge", "firefox", "opera", "brave",

        // Common applications
        "Discord", "Spotify", "Steam", "steamwebhelper",
        "EpicGamesLauncher", "ShareX", "obs64", "obs32",

        // GPU tools and overlays
        "NVIDIA Overlay", "NVIDIA Share", "nvcontainer",
        "nvoawrapperdll", "nvsphelper64", "nvidia_share",
        "RadeonSoftware", "AMDRSServ", "aaborern",
        "GameBar", "GameBarPresenceWriter",
        "MSIAfterburner", "RTSS", "RivaTunerStatisticsServer",

        // This application itself
        "BorderlessTool"
    };

    /// <summary>
    /// Attempts to find a single game candidate among all running processes.
    /// <para>
    /// If multiple candidates are found, preference is given to the window
    /// currently in the foreground. If no foreground match exists, the first
    /// candidate in the list is returned.
    /// </para>
    /// </summary>
    /// <param name="game">
    /// When this method returns <c>true</c>, contains the detected game candidate;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if at least one game was detected; otherwise, <c>false</c>.</returns>
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

        // Prefer whichever candidate is currently in the foreground
        nint fg = GetForegroundWindow();
        foreach (var c in candidates)
        {
            if (c.Hwnd == fg)
            {
                game = c;
                return true;
            }
        }

        // Fall back to the first candidate if none is in the foreground
        game = candidates[0];
        return true;
    }

    /// <summary>
    /// Scans all running processes and returns a list of game window candidates.
    /// <para>
    /// A process is considered a game candidate if it meets all of the following criteria:
    /// <list type="bullet">
    ///   <item>Not in the <see cref="BlacklistedProcesses"/> list.</item>
    ///   <item>Has a visible main window (MainWindowHandle != 0).</item>
    ///   <item>Does not reside in a system or Windows path.</item>
    ///   <item>Does not reside in a known GPU overlay/tool path.</item>
    ///   <item>Has at least one graphics DLL loaded (see <see cref="GraphicsDlls"/>).</item>
    ///   <item>Has a non-empty window title.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <returns>A read-only list of detected game window candidates.</returns>
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
                // Skip processes that throw access denied or other exceptions
            }
            finally
            {
                proc.Dispose();
            }
        }

        return results;
    }

    /// <summary>
    /// Checks whether the given process has at least one graphics-related DLL loaded.
    /// This is used to distinguish games from regular GUI applications.
    /// </summary>
    /// <param name="proc">The process to inspect.</param>
    /// <returns><c>true</c> if a graphics DLL is found in the process modules; otherwise, <c>false</c>.</returns>
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

    /// <summary>
    /// Determines whether the given executable path belongs to a Windows system directory.
    /// Processes in system paths are excluded from game detection.
    /// </summary>
    /// <param name="path">The full path to the executable, or null.</param>
    /// <returns><c>true</c> if the path is a system path or null; otherwise, <c>false</c>.</returns>
    private static bool IsSystemPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        return path.Contains(@"\Windows\", StringComparison.OrdinalIgnoreCase)
            || path.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the given executable path belongs to a known GPU tool
    /// or overlay vendor directory (NVIDIA, AMD, RivaTuner, MSI Afterburner, Xbox).
    /// </summary>
    /// <param name="path">The full path to the executable, or null.</param>
    /// <returns><c>true</c> if the path matches a known overlay vendor; otherwise, <c>false</c>.</returns>
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

    /// <summary>
    /// Returns the window handle of the currently active foreground window.
    /// Used to prioritize the game the user is currently interacting with.
    /// </summary>
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}