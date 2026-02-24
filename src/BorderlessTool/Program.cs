using BorderlessTool.Monitors;
using BorderlessTool.UI;
using BorderlessTool.Core;

// Configure console encoding to support Unicode characters and emoji
ConsoleUI.SetupConsole();

// Holds the currently detected game window candidate, updated by the background timer
GameWindowCandidate? detectedGame = null;

// Lock object used to synchronize access to detectedGame between the timer thread and the main loop
object lockObj = new();

Console.WriteLine("Versão nova rodando!");

// Background timer that polls for a running game every 2 seconds.
// If a new game is detected (or the previously detected game closes), the status line is updated.
var gameDetectionTimer = new Timer(_ =>
{
    if (GameDetector.TryGetSingleGame(out var game))
    {
        lock (lockObj)
        {
            // Only update if the detected game has changed (avoids unnecessary redraws)
            if (detectedGame?.WindowTitle != game!.WindowTitle)
            {
                detectedGame = game;
                UpdateGameStatusLine(game.WindowTitle);
            }
        }
    }
    else
    {
        lock (lockObj)
        {
            // Clear the detected game if it is no longer running
            if (detectedGame != null)
            {
                detectedGame = null;
                UpdateGameStatusLine(null);
            }
        }
    }
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

// -------------------------------------------------------------------------
// Main application loop
// -------------------------------------------------------------------------
while (true)
{
    Console.Clear();

    // Display the currently detected game at the top of the screen
    lock (lockObj)
    {
        if (detectedGame != null)
            Console.WriteLine($"🎮 Jogo detectado: {detectedGame!.WindowTitle}");
        else
            Console.WriteLine("🎮 Jogo detectado: (nenhum)");
    }

    Console.WriteLine(new string('-', 60));

    var monitors = MonitorManager.EnumerateAllMonitors();
    ConsoleUI.PrintMonitorInfo(monitors);

    int option = ConsoleUI.DisplayMainMenu();

    // Option 5: Exit the application
    if (option == 5)
        break;

    // Option 4: Manage the detected game window
    if (option == 4)
    {
        lock (lockObj)
        {
            if (detectedGame == null)
            {
                Console.WriteLine("\nNenhuma opção disponível.");
                ConsoleUI.WaitAndClear();
                continue;
            }

            int gameOption = ConsoleUI.GameOptions();

            if (gameOption == 1)
            {
                // Apply borderless windowed mode to the detected game
                IntPtr hwnd = WindowManager.GetHwnd();
                if (hwnd != IntPtr.Zero)
                {
                    WindowManager.ApplyBorderless(hwnd);
                }
            }
        }

        ConsoleUI.WaitAndClear();
        continue;
    }

    // Options 1-3: Monitor configuration (requires at least one monitor)
    if (option < 1 || option > 3 || monitors.Count == 0)
    {
        Console.WriteLine("\nOpção inválida ou nenhum monitor encontrado.");
        ConsoleUI.WaitAndClear();
        continue;
    }

    int monitorNum = ConsoleUI.SelectMonitor(monitors);
    if (monitorNum == -1)
    {
        Console.WriteLine("\nSeleção de monitor inválida!");
        ConsoleUI.WaitAndClear();
        continue;
    }

    string targetDevice = monitors[monitorNum - 1].DeviceName;

    // Dispatch the selected monitor operation and display the result
    MonitorStatus status = option switch
    {
        1 => MonitorManager.ChangeResolution(targetDevice, 3840, 2160),
        2 => MonitorManager.ChangeResolution(targetDevice, 1920, 1080),
        3 => MonitorManager.SetPrimaryMonitor(targetDevice),
        _ => MonitorStatus.Failed
    };

    ConsoleUI.ShowOperationResult(status);
    ConsoleUI.WaitAndClear();
}

// -------------------------------------------------------------------------
// Shutdown
// -------------------------------------------------------------------------

// Dispose the background timer to stop game detection polling before exiting
gameDetectionTimer.Dispose();

// -------------------------------------------------------------------------
// Local functions
// -------------------------------------------------------------------------

/// <summary>
/// Updates the game status line at the top of the console (row 0) in-place,
/// without disrupting the current cursor position or the rest of the UI.
/// Called from the background timer thread whenever the detected game changes.
/// </summary>
/// <param name="gameTitle">
/// The title of the newly detected game, or <c>null</c> if no game is running.
/// </param>
static void UpdateGameStatusLine(string? gameTitle)
{
    try
    {
        // Save the current cursor position so we can restore it after writing to row 0
        int savedTop = Console.CursorTop;
        int savedLeft = Console.CursorLeft;

        Console.SetCursorPosition(0, 0);

        if (gameTitle != null)
            Console.Write($"🎮 Jogo detectado: {gameTitle}".PadRight(Console.WindowWidth - 1));
        else
            Console.Write("🎮 Jogo detectado: (nenhum)".PadRight(Console.WindowWidth - 1));

        // Restore cursor to where it was before the update
        Console.SetCursorPosition(savedLeft, savedTop);
    }
    catch
    {
        // Suppress cursor errors caused by console window resizing
    }
}