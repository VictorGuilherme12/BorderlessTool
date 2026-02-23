using MonitorTool;

ConsoleUI.SetupConsole();

GameWindowCandidate? detectedGame = null;
object lockObj = new();

// Timer em background que detecta jogos a cada 2 segundos
var gameDetectionTimer = new Timer(_ =>
{
    if (GameWindowDetector.TryGetSingleGame(out var game))
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

    var monitors = MonitorUtils.EnumerateAllMonitors();
    ConsoleUI.PrintMonitorInfo(monitors);

    int option = ConsoleUI.DisplayMainMenu();

    if (option == 5)
    {
        lock (lockObj)
        {
            if (detectedGame == null)
            {
                Console.WriteLine($"Nenhuma opcao disponivel");
                ConsoleUI.WaitAndClear();
                continue;
            }

            int gameOption = ConsoleUI.GameOptions();
            if (gameOption == 1)
            {
                Console.WriteLine("\n⚠️ Função ainda não implementada.");
            }
            
        }

        ConsoleUI.WaitAndClear();
        continue;
    }

    if (option == 4)
        break;

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
        1 => MonitorUtils.ChangeResolution(targetDevice, 3840, 2160),
        2 => MonitorUtils.ChangeResolution(targetDevice, 1920, 1080),
        3 => MonitorUtils.SetPrimaryMonitor(targetDevice),
        _ => MonitorStatus.Failed
    };

    ConsoleUI.ShowOperationResult(status);
    ConsoleUI.WaitAndClear();
}

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

    }
}