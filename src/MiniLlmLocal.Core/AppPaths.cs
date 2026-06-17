namespace MiniLlmLocal.Core;

/// <summary>
/// Resolve os diretórios padrão do projeto (models/ e data/). Procura a raiz
/// do repositório subindo a partir do diretório do executável até achar o
/// arquivo .sln; se não achar, usa o diretório de trabalho atual.
/// </summary>
public static class AppPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string ModelsDirectory => Path.Combine(RepoRoot, "models");

    public static string DataDirectory => Path.Combine(RepoRoot, "data");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.EnumerateFiles("*.sln").Any() || dir.EnumerateFiles("*.slnx").Any())
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}
