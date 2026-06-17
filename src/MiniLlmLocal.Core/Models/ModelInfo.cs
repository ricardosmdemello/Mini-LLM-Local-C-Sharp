namespace MiniLlmLocal.Core.Models;

/// <summary>Família/arquitetura do modelo. Afeta o template de prompt.</summary>
public enum ModelFamily
{
    Gemma,
    Llama,
    Phi
}

/// <summary>
/// Metadados de um modelo GGUF disponível para download. As URLs apontam para
/// arquivos .gguf hospedados no Hugging Face.
/// </summary>
public sealed record ModelInfo(
    string Id,
    string Name,
    ModelFamily Family,
    string Description,
    string DownloadUrl,
    string FileName,
    long ApproxSizeBytes,
    string License)
{
    /// <summary>Tamanho aproximado formatado (ex.: "1,6 GB").</summary>
    public string ApproxSizeDisplay => FormatBytes(ApproxSizeBytes);

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0} {1}", size, units[unit]);
    }
}
