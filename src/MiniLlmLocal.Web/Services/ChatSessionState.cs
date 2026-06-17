using System.Runtime.CompilerServices;
using MiniLlmLocal.Core.Chat;

namespace MiniLlmLocal.Web.Services;

/// <summary>
/// Estado de uma conversa, com escopo de circuito (uma instância por aba/conexão
/// Blazor Server). Encapsula o <see cref="IChatEngine"/> carregado e serializa as
/// chamadas de inferência — o <c>LLamaContext</c> não é thread-safe.
/// </summary>
public sealed class ChatSessionState : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IChatEngine? _engine;

    public string? LoadedModelPath { get; private set; }
    public bool IsLoaded => _engine is not null;
    public bool IsBusy { get; private set; }
    public List<ChatMessage> Messages { get; } = [];

    /// <summary>Carrega um modelo GGUF (operação pesada — roda fora da thread de UI).</summary>
    public async Task LoadAsync(string modelPath, string? systemPrompt, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            _engine?.Dispose();
            _engine = null;
            Messages.Clear();

            var options = string.IsNullOrWhiteSpace(systemPrompt)
                ? new ChatEngineOptions { ModelPath = modelPath }
                : new ChatEngineOptions { ModelPath = modelPath, SystemPrompt = systemPrompt };

            _engine = await Task.Run(() => LlamaChatEngine.Load(options), ct);
            LoadedModelPath = modelPath;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Envia a mensagem do usuário e devolve a resposta em streaming. Mantém o
    /// histórico em <see cref="Messages"/> (a mensagem do usuário e a do assistente
    /// devem ser adicionadas pelo chamador da UI).
    /// </summary>
    public async IAsyncEnumerable<string> StreamReplyAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_engine is null)
            throw new InvalidOperationException("Nenhum modelo carregado.");

        await _gate.WaitAsync(ct);
        IsBusy = true;
        try
        {
            await foreach (var chunk in _engine.StreamReplyAsync(userMessage, ct))
                yield return chunk;
        }
        finally
        {
            IsBusy = false;
            _gate.Release();
        }
    }

    public void Reset()
    {
        _engine?.Reset();
        Messages.Clear();
    }

    public void Dispose()
    {
        _engine?.Dispose();
        _gate.Dispose();
    }
}
