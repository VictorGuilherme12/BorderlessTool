using MonitorTool;

ConsoleUI.SetupConsole();

string gameStatusLine;
if (GameWindowDetector.TryGetSingleGame(out var game))
    gameStatusLine = $"🎮 Jogo detectado: {game!.WindowTitle}";
else
    gameStatusLine = "🎮 Jogo detectado: (nenhum)";


while (true)
{
    Console.WriteLine(gameStatusLine);
    Console.WriteLine(new string('-', 60));

    var monitors = MonitorUtils.EnumerateAllMonitors();
    ConsoleUI.PrintMonitorInfo(monitors);

    int option = ConsoleUI.DisplayMainMenu();

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