namespace MiniLlmLocal.Core.Models;

/// <summary>Um modelo .gguf presente no disco.</summary>
public sealed record LocalModel(string FileName, string FullPath, long SizeBytes)
{
    public string SizeDisplay => ModelInfo.FormatBytes(SizeBytes);
}

/// <summary>Gerencia os modelos GGUF baixados em um diretório local.</summary>
public sealed class LocalModelStore
{
    public string Directory { get; }

    public LocalModelStore(string directory)
    {
        Directory = directory;
    }

    /// <summary>Lista os arquivos .gguf presentes no diretório (ordenados por nome).</summary>
    public IReadOnlyList<LocalModel> List()
    {
        if (!System.IO.Directory.Exists(Directory))
            return [];

        return System.IO.Directory
            .EnumerateFiles(Directory, "*.gguf", SearchOption.TopDirectoryOnly)
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new LocalModel(info.Name, info.FullName, info.Length);
            })
            .OrderBy(m => m.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>True se o modelo do catálogo já foi baixado.</summary>
    public bool IsDownloaded(ModelInfo model) =>
        File.Exists(Path.Combine(Directory, model.FileName));
}
