using BorderlessTool.Monitors;
using BorderlessTool.UI;
using BorderlessTool.Core;

ConsoleUI.SetupConsole();

GameWindowCandidate? detectedGame = null;
object lockObj = new();

Console.WriteLine("Versão nova rodando!");

// Timer em background que detecta jogos a cada 2 segundos
var gameDetectionTimer = new Timer(_ =>
{
    if (GameDetector.TryGetSingleGame(out var game))
    {
        lock (lockObj)
        {
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
            if (detectedGame != null)
            {
                detectedGame = null;
                UpdateGameStatusLine(null);
            }
        }
    }
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

while (true)
{
    Console.Clear();

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

    // ✅ 5 = SAIR
    if (option == 5)
        break;

    // 🎮 Gerenciar jogo
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

    // Opções 1-3 (Monitores)
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

// 🔚 Encerramento limpo
gameDetectionTimer.Dispose();

static void UpdateGameStatusLine(string? gameTitle)
{
    try
    {
        int savedTop = Console.CursorTop;
        int savedLeft = Console.CursorLeft;

        Console.SetCursorPosition(0, 0);

        if (gameTitle != null)
            Console.Write($"🎮 Jogo detectado: {gameTitle}".PadRight(Console.WindowWidth - 1));
        else
            Console.Write("🎮 Jogo detectado: (nenhum)".PadRight(Console.WindowWidth - 1));

        Console.SetCursorPosition(savedLeft, savedTop);
    }
    catch
    {
        // Ignora erros de cursor (ex: redimensionamento da janela)
    }
}