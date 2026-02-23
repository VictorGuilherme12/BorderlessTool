using System;
using System.Collections.Generic;
using BorderlessTool.Monitors;

namespace BorderlessTool.UI;

public static class ConsoleUI
{
    public static void SetupConsole()
    {
        Console.InputEncoding = System.Text.Encoding.Unicode;
        Console.OutputEncoding = System.Text.Encoding.Unicode;
    }

    public static void PrintMonitorInfo(IReadOnlyList<MonitorInfo> monitors)
    {
        Console.WriteLine("🖥️  Informações dos Monitores Atuais:");
        for (int i = 0; i < monitors.Count; i++)
        {
            var m = monitors[i];
            Console.Write($"  {i + 1}. Device: {m.DeviceName}, Resolução: {m.Width}x{m.Height}");
            if (m.IsPrimary)
                Console.Write(" (⭐ Primário)");
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    public static int DisplayMainMenu()
    {
        Console.WriteLine("Escolha uma opção:");
        Console.WriteLine("1. Mudar para 4K (3840x2160)");
        Console.WriteLine("2. Mudar para Full HD (1920x1080)");
        Console.WriteLine("3. Definir como monitor primário");
        Console.WriteLine("4. Sair");
        Console.WriteLine("5. Gerenciar jogo detectado");
        Console.Write("Opção: ");

        return ReadInt();
    }

    public static int GameOptions()
    {
        Console.WriteLine("\nEscolha uma ação para o jogo:");
        Console.WriteLine("1. Aplicar Borderless windowed");
        Console.WriteLine("Opção: ");

        return ReadInt();
    }

    public static int SelectMonitor(IReadOnlyList<MonitorInfo> monitors)
    {
        Console.Write($"\nEscolha o monitor para aplicar a alteração (1-{monitors.Count}): ");
        int monitorNum = ReadInt();
        if (monitorNum <= 0 || monitorNum > monitors.Count)
            return -1;
        return monitorNum;
    }

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

    public static void WaitAndClear()
    {
        Console.WriteLine("\nPressione ENTER para continuar...");
        Console.ReadLine();
        Console.Clear();
    }

    private static int ReadInt()
    {
        var s = Console.ReadLine();
        return int.TryParse(s, out int v) ? v : int.MinValue;
    }
}