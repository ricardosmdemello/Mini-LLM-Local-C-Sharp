namespace MiniLlmLocal.Core.Models;

/// <summary>Progresso de um download em andamento.</summary>
public sealed record DownloadProgress(long BytesReceived, long? TotalBytes)
{
    /// <summary>Fração concluída (0..1), ou null se o tamanho total é desconhecido.</summary>
    public double? Fraction =>
        TotalBytes is > 0 ? Math.Clamp((double)BytesReceived / TotalBytes.Value, 0, 1) : null;
}

/// <summary>
/// Baixa arquivos GGUF do Hugging Face para um diretório local, com relato de
/// progresso e download seguro (grava em ".part" e renomeia ao final).
/// </summary>
public sealed class ModelDownloader
{
    private readonly HttpClient _http;

    public ModelDownloader(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
    }

    /// <summary>
    /// Baixa <paramref name="model"/> para <paramref name="targetDirectory"/>.
    /// Se o arquivo já existir, retorna o caminho sem baixar de novo.
    /// </summary>
    /// <returns>Caminho completo do arquivo .gguf baixado.</returns>
    public async Task<string> DownloadAsync(
        ModelInfo model,
        string targetDirectory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(targetDirectory);
        var finalPath = Path.Combine(targetDirectory, model.FileName);
        if (File.Exists(finalPath))
            return finalPath;

        var partPath = finalPath + ".part";

        using var response = await _http.GetAsync(
            model.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? model.ApproxSizeBytes;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using (var destination = File.Create(partPath))
        {
            var buffer = new byte[81920];
            long received = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                received += read;
                progress?.Report(new DownloadProgress(received, total));
            }
        }

        File.Move(partPath, finalPath, overwrite: true);
        return finalPath;
    }
}
