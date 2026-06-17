using System.Text.Json;

namespace MiniLlmLocal.Core.Data;

/// <summary>Lê datasets de fine-tuning no formato JSONL (um JSON por linha).</summary>
public static class DatasetReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Lê e parseia um arquivo .jsonl do disco.</summary>
    public static IReadOnlyList<TrainingExample> ReadJsonl(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Dataset não encontrado.", path);

        return ParseJsonl(File.ReadLines(path));
    }

    /// <summary>
    /// Parseia linhas JSONL. Linhas em branco são ignoradas; linhas inválidas
    /// lançam <see cref="JsonException"/> com o número da linha.
    /// </summary>
    public static IReadOnlyList<TrainingExample> ParseJsonl(IEnumerable<string> lines)
    {
        var result = new List<TrainingExample>();
        var lineNumber = 0;

        foreach (var line in lines)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var example = JsonSerializer.Deserialize<TrainingExample>(line, Options);
                if (example is not null)
                    result.Add(example);
            }
            catch (JsonException ex)
            {
                throw new JsonException($"JSON inválido na linha {lineNumber}: {ex.Message}", ex);
            }
        }

        return result;
    }
}
