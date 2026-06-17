using MiniLlmLocal.Console.Menus;
using MiniLlmLocal.Core;
using MiniLlmLocal.Core.Models;
using Spectre.Console;

AnsiConsole.Write(new FigletText("Mini LLM Local").Color(Color.SpringGreen3));
AnsiConsole.MarkupLine("[grey]Chat local com modelos GGUF (Gemma / Llama / Phi) via LLamaSharp[/]");
AnsiConsole.MarkupLine($"[grey]Modelos:[/] [blue]{AppPaths.ModelsDirectory}[/]");
AnsiConsole.MarkupLine($"[grey]Dados:  [/] [blue]{AppPaths.DataDirectory}[/]");
AnsiConsole.WriteLine();

var store = new LocalModelStore(AppPaths.ModelsDirectory);

while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("O que deseja fazer?")
            .PageSize(6)
            .AddChoices(
                "💬  Conversar com a IA",
                "📦  Modelos (baixar / listar)",
                "📊  Ver dados de treino",
                "🚪  Sair"));

    switch (choice)
    {
        case "💬  Conversar com a IA":
            await ChatMenu.RunAsync(store);
            break;
        case "📦  Modelos (baixar / listar)":
            await ModelsMenu.RunAsync(store);
            break;
        case "📊  Ver dados de treino":
            DataMenu.Run();
            break;
        default:
            AnsiConsole.MarkupLine("[grey]Até logo![/]");
            return;
    }

    AnsiConsole.WriteLine();
}
