namespace MiniLlmLocal.Core.Chat;

/// <summary>
/// Parâmetros de carga e de amostragem usados para iniciar uma sessão de chat
/// sobre um modelo GGUF via LLamaSharp.
/// </summary>
public sealed class ChatEngineOptions
{
    /// <summary>Caminho absoluto para o arquivo .gguf.</summary>
    public required string ModelPath { get; init; }

    /// <summary>
    /// Prompt de sistema que define a "persona" do assistente. O padrão é um
    /// agente de suporte técnico — ajuste conforme o domínio do fine-tuning.
    /// </summary>
    public string SystemPrompt { get; init; } =
        "Você é um assistente de suporte técnico. Responda de forma objetiva, " +
        "educada e em português do Brasil. Quando não souber, diga que não sabe.";

    /// <summary>Tamanho da janela de contexto (tokens). Limite pela VRAM/RAM disponível.</summary>
    public uint ContextSize { get; init; } = 4096;

    /// <summary>
    /// Número de camadas a descarregar na GPU. 0 = só CPU. Use um valor alto
    /// (ex.: 999) para colocar o modelo inteiro na GPU quando houver VRAM.
    /// </summary>
    public int GpuLayerCount { get; init; } = 0;

    /// <summary>Máximo de tokens gerados por resposta.</summary>
    public int MaxTokens { get; init; } = 512;

    /// <summary>Temperatura de amostragem (0 = determinístico, &gt;1 = mais criativo).</summary>
    public float Temperature { get; init; } = 0.7f;

    /// <summary>Top-p (nucleus sampling).</summary>
    public float TopP { get; init; } = 0.9f;

    /// <summary>
    /// Sequências que encerram a geração. Cobrem os marcadores de fim de turno
    /// das famílias Gemma, Llama 3 e Phi-3, além do clássico "User:".
    /// </summary>
    public IReadOnlyList<string> AntiPrompts { get; init; } = new[]
    {
        "User:",
        "Usuário:",
        "<end_of_turn>",   // Gemma
        "<|eot_id|>",      // Llama 3.x
        "<|end|>"          // Phi-3
    };
}
