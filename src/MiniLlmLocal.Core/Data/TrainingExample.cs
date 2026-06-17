using System.Text.Json.Serialization;

namespace MiniLlmLocal.Core.Data;

/// <summary>
/// Um exemplo de treino no formato de instrução (estilo Alpaca). É o mesmo
/// formato consumido pelo script de fine-tuning em Python (training/).
/// </summary>
public sealed record TrainingExample
{
    [JsonPropertyName("instruction")]
    public string Instruction { get; init; } = string.Empty;

    /// <summary>Entrada/contexto opcional (ex.: a mensagem do cliente).</summary>
    [JsonPropertyName("input")]
    public string Input { get; init; } = string.Empty;

    [JsonPropertyName("output")]
    public string Output { get; init; } = string.Empty;
}
