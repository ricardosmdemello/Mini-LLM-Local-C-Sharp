namespace MiniLlmLocal.Core.Chat;

/// <summary>
/// Abstração de um motor de chat local. Implementações encapsulam o modelo
/// carregado e mantêm o histórico da conversa.
/// </summary>
public interface IChatEngine : IDisposable
{
    /// <summary>
    /// Envia a mensagem do usuário e devolve a resposta do assistente em
    /// streaming, token a token (ou pedaço a pedaço).
    /// </summary>
    IAsyncEnumerable<string> StreamReplyAsync(string userMessage, CancellationToken cancellationToken = default);

    /// <summary>Limpa o histórico da conversa, mantendo o prompt de sistema.</summary>
    void Reset();
}
