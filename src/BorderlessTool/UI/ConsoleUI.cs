using System;
using System.Collections.Generic;
using BorderlessTool.Monitors;

namespace BorderlessTool.UI;

/// <summary>
/// Provides all console-based user interface rendering and input handling for the application.
/// Responsible for displaying menus, monitor information, and operation results.
/// </summary>
public static class ConsoleUI
{
    /// <summary>
    /// Configures the console to use Unicode encoding for both input and output.
    /// Required to correctly render emoji and special characters in menu prompts.
    /// Should be called once at application startup before any output is written.
    /// </summary>
    public static void SetupConsole()
    {
        Console.InputEncoding = System.Text.Encoding.Unicode;
        Console.OutputEncoding = System.Text.Encoding.Unicode;
    }

    /// <summary>
    /// Prints a formatted list of all detected monitors, including their device name,
    /// resolution, and whether each is the primary display.
    /// </summary>
    /// <param name="monitors">The list of monitors to display, as returned by <see cref="MonitorManager.EnumerateAllMonitors"/>.</param>
    public static void PrintMonitorInfo(IReadOnlyList<MonitorInfo> monitors)
    {
        Console.WriteLine("🖥️  Informações dos Monitores Atuais:");
        for (int i = 0; i < monitors.Count; i++)
        {
            var m = monitors[i];
            Console.Write($"  {i + 1}. Device: {m.FriendlyName}, Resolução: {m.Width}x{m.Height}");
            if (m.IsPrimary)
                Console.Write(" (⭐ Primário)");
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Displays the main application menu and reads the user's choice.
    /// </summary>
    /// <returns>
    /// The integer option selected by the user, or <see cref="int.MinValue"/> if the input was invalid.
    /// </returns>
    public static int DisplayMainMenu()
    {
        Console.WriteLine("Escolha uma opção:");
        Console.WriteLine("1. Mudar para 4K (3840x2160)");
        Console.WriteLine("2. Mudar para Full HD (1920x1080)");
        Console.WriteLine("3. Definir como monitor primário");
        Console.WriteLine("4. Gerenciar jogo detectado");
        Console.WriteLine("5. Sair");
        Console.Write("Opção: ");
        return ReadInt();
    }

    /// <summary>
    /// Displays the game management submenu and reads the user's choice.
    /// Shown after a game window has been detected.
    /// </summary>
    /// <returns>
    /// The integer option selected by the user, or <see cref="int.MinValue"/> if the input was invalid.
    /// </returns>
    public static int GameOptions()
    {
        Console.WriteLine("\nEscolha uma ação para o jogo:");
        Console.WriteLine("1. Aplicar Borderless windowed");
        Console.WriteLine("Opção: ");
        return ReadInt();
    }

    /// <summary>
    /// Prompts the user to select a monitor from the provided list by number.
    /// </summary>
    /// <param name="monitors">The list of available monitors to choose from.</param>
    /// <returns>
    /// The 1-based index of the selected monitor, or -1 if the input was out of range or invalid.
    /// </returns>
    public static int SelectMonitor(IReadOnlyList<MonitorInfo> monitors)
    {
        Console.Write($"\nEscolha o monitor para aplicar a alteração (1-{monitors.Count}): ");
        int monitorNum = ReadInt();
        if (monitorNum <= 0 || monitorNum > monitors.Count)
            return -1;
        return monitorNum;
    }

    /// <summary>
    /// Displays a user-facing message corresponding to the result of a monitor operation.
    /// Each <see cref="MonitorStatus"/> value maps to a distinct message with an appropriate emoji indicator.
    /// </summary>
    /// <param name="status">The result status returned by a <see cref="MonitorManager"/> operation.</param>
    public static void ShowOperationResult(MonitorStatus status)
    {
        switch (status)
        {
            case MonitorStatus.Success:
                Console.WriteLine("\n✅ Operação realizada com sucesso!");
                break;
            case MonitorStatus.Failed:
                Console.WriteLine("\n❌ Falha ao executar a operação.");
                break;
            case MonitorStatus.BadMode:
                Console.WriteLine("\n⚠️ O modo de vídeo não é suportado por este monitor.");
                break;
            case MonitorStatus.RestartRequired:
                Console.WriteLine("\n🔄 É necessário reiniciar o computador para aplicar as alterações.");
                break;
            case MonitorStatus.MonitorNotFound:
                Console.WriteLine("\n🔍 Monitor não encontrado.");
                break;
        }
    }

    /// <summary>
    /// Pauses execution until the user presses ENTER, then clears the console.
    /// Used between menu interactions to let the user read the output before returning to the menu.
    /// </summary>
    public static void WaitAndClear()
    {
        Console.WriteLine("\nPressione ENTER para continuar...");
        Console.ReadLine();
        Console.Clear();
    }

    /// <summary>
    /// Reads a line from the console and attempts to parse it as an integer.
    /// </summary>
    /// <returns>
    /// The parsed integer value, or <see cref="int.MinValue"/> if the input could not be parsed.
    /// </returns>
    private static int ReadInt()
    {
        var s = Console.ReadLine();
        return int.TryParse(s, out int v) ? v : int.MinValue;
    }
}