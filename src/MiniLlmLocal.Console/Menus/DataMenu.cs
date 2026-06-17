using MiniLlmLocal.Core;
using MiniLlmLocal.Core.Data;
using Spectre.Console;

namespace MiniLlmLocal.Console.Menus;

/// <summary>Inspeciona os datasets de treino (.jsonl) presentes em data/.</summary>
public static class DataMenu
{
    public static void Run()
    {
        if (!Directory.Exists(AppPaths.DataDirectory))
        {
            AnsiConsole.MarkupLine($"[yellow]Diretório de dados não existe:[/] {Markup.Escape(AppPaths.DataDirectory)}");
            return;
        }

        var files = Directory
            .EnumerateFiles(AppPaths.DataDirectory, "*.jsonl", SearchOption.AllDirectories)
            .ToList();

        if (files.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Nenhum dataset .jsonl encontrado em data/.[/]");
            return;
        }

        var file = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Escolha um dataset:")
                .UseConverter(f => Markup.Escape(Path.GetRelativePath(AppPaths.DataDirectory, f)))
                .AddChoices(files));

        IReadOnlyList<TrainingExample> examples;
        try
        {
            examples = DatasetReader.ReadJsonl(file);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erro ao ler o dataset:[/] {Markup.Escape(ex.Message)}");
            return;
        }

        AnsiConsole.MarkupLine($"[green]{examples.Count}[/] exemplos carregados.");

        const int preview = 10;
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("#");
        table.AddColumn("Instruction / Input");
        table.AddColumn("Output");

        foreach (var (ex, i) in examples.Take(preview).Select((e, i) => (e, i)))
        {
            table.AddRow(
                (i + 1).ToString(),
                Markup.Escape(Truncate(ex.Instruction, 120)) + (string.IsNullOrWhiteSpace(ex.Input) ? "" : $"\n[grey]{Markup.Escape(Truncate(ex.Input, 120))}[/]"),
                Markup.Escape(Truncate(ex.Output, 160)));
        }

        AnsiConsole.Write(table);
        if (examples.Count > preview)
            AnsiConsole.MarkupLine($"[grey]... e mais {examples.Count - preview} exemplos.[/]");
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}
