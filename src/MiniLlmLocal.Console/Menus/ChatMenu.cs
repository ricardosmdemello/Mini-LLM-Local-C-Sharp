using MiniLlmLocal.Core.Chat;
using MiniLlmLocal.Core.Models;
using Spectre.Console;

namespace MiniLlmLocal.Console.Menus;

/// <summary>Carrega um modelo local e conduz uma conversa interativa com streaming.</summary>
public static class ChatMenu
{
    public static async Task RunAsync(LocalModelStore store)
    {
        var locals = store.List();
        if (locals.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Nenhum modelo baixado. Use o menu 'Modelos' primeiro.[/]");
            return;
        }

        var model = AnsiConsole.Prompt(
            new SelectionPrompt<LocalModel>()
                .Title("Escolha o modelo para a conversa:")
                .UseConverter(m => $"{Markup.Escape(m.FileName)} [grey]({m.SizeDisplay})[/]")
                .AddChoices(locals));

        var systemPrompt = AnsiConsole.Prompt(
            new TextPrompt<string>("[grey]Prompt de sistema (Enter p/ padrão):[/]")
                .AllowEmpty());

        var options = new ChatEngineOptions
        {
            ModelPath = model.FullPath,
            SystemPrompt = string.IsNullOrWhiteSpace(systemPrompt)
                ? new ChatEngineOptions { ModelPath = model.FullPath }.SystemPrompt
                : systemPrompt
        };

        IChatEngine? engine = null;
        try
        {
            await AnsiConsole.Status()
                .StartAsync("Carregando modelo... (pode levar alguns segundos)", _ =>
                {
                    engine = LlamaChatEngine.Load(options);
                    return Task.CompletedTask;
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Não foi possível carregar o modelo:[/] {Markup.Escape(ex.Message)}");
            engine?.Dispose();
            return;
        }

        using (engine)
        {
            AnsiConsole.MarkupLine("[green]Modelo pronto![/] Comandos: [yellow]/sair[/] para voltar, [yellow]/reset[/] para limpar o histórico.");
            AnsiConsole.WriteLine();

            while (true)
            {
                var input = AnsiConsole.Prompt(new TextPrompt<string>("[bold blue]Você:[/]").AllowEmpty());

                if (string.IsNullOrWhiteSpace(input))
                    continue;
                if (input.Trim().Equals("/sair", StringComparison.OrdinalIgnoreCase))
                    break;
                if (input.Trim().Equals("/reset", StringComparison.OrdinalIgnoreCase))
                {
                    engine!.Reset();
                    AnsiConsole.MarkupLine("[grey]Histórico limpo.[/]");
                    continue;
                }

                AnsiConsole.Markup("[bold springgreen3]IA:[/] ");
                try
                {
                    await foreach (var chunk in engine!.StreamReplyAsync(input))
                        AnsiConsole.Write(chunk);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"\n[red]Erro na geração:[/] {Markup.Escape(ex.Message)}");
                }
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();
            }
        }
    }
}
