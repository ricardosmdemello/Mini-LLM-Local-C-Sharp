using MiniLlmLocal.Core.Models;
using Spectre.Console;

namespace MiniLlmLocal.Console.Menus;

/// <summary>Menu para listar modelos locais e baixar modelos do catálogo.</summary>
public static class ModelsMenu
{
    public static async Task RunAsync(LocalModelStore store)
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Modelos[/]")
                    .AddChoices(
                        "Listar modelos baixados",
                        "Baixar modelo do catálogo",
                        "Voltar"));

            switch (choice)
            {
                case "Listar modelos baixados":
                    ShowLocalModels(store);
                    break;
                case "Baixar modelo do catálogo":
                    await DownloadFromCatalogAsync(store);
                    break;
                default:
                    return;
            }
        }
    }

    private static void ShowLocalModels(LocalModelStore store)
    {
        var locals = store.List();
        if (locals.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Nenhum modelo baixado ainda.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Arquivo");
        table.AddColumn(new TableColumn("Tamanho").RightAligned());
        foreach (var m in locals)
            table.AddRow(Markup.Escape(m.FileName), m.SizeDisplay);
        AnsiConsole.Write(table);
    }

    private static async Task DownloadFromCatalogAsync(LocalModelStore store)
    {
        var model = AnsiConsole.Prompt(
            new SelectionPrompt<ModelInfo>()
                .Title("Escolha um modelo para baixar:")
                .PageSize(10)
                .UseConverter(m =>
                {
                    var status = store.IsDownloaded(m) ? "[green](já baixado)[/]" : $"[grey]{m.ApproxSizeDisplay}[/]";
                    return $"{Markup.Escape(m.Name)} {status}";
                })
                .AddChoices(ModelCatalog.All));

        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(model.Description)}[/]");
        AnsiConsole.MarkupLine($"[grey]Licença: {Markup.Escape(model.License)}[/]");

        if (store.IsDownloaded(model))
        {
            AnsiConsole.MarkupLine("[green]Este modelo já está no disco.[/]");
            return;
        }

        if (!AnsiConsole.Confirm($"Baixar [bold]{Markup.Escape(model.Name)}[/] (~{model.ApproxSizeDisplay})?"))
            return;

        var downloader = new ModelDownloader();
        try
        {
            await AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new DownloadedColumn(),
                    new TransferSpeedColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask($"Baixando {Markup.Escape(model.FileName)}", maxValue: model.ApproxSizeBytes);
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        if (p.TotalBytes is > 0)
                            task.MaxValue = p.TotalBytes.Value;
                        task.Value = p.BytesReceived;
                    });
                    await downloader.DownloadAsync(model, store.Directory, progress);
                    task.Value = task.MaxValue;
                });

            AnsiConsole.MarkupLine($"[green]✓ Modelo salvo em[/] [blue]{Markup.Escape(store.Directory)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Falha no download:[/] {Markup.Escape(ex.Message)}");
        }
    }
}
