using System.Runtime.CompilerServices;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace MiniLlmLocal.Core.Chat;

/// <summary>
/// Motor de chat baseado em LLamaSharp (llama.cpp). Carrega um modelo GGUF e
/// mantém uma <see cref="ChatSession"/> interativa.
/// </summary>
public sealed class LlamaChatEngine : IChatEngine
{
    private readonly LLamaWeights _weights;
    private readonly LLamaContext _context;
    private readonly ChatEngineOptions _options;
    private ChatSession _session;

    private LlamaChatEngine(LLamaWeights weights, LLamaContext context, ChatEngineOptions options)
    {
        _weights = weights;
        _context = context;
        _options = options;
        _session = CreateSession();
    }

    /// <summary>Carrega o modelo do disco e cria o motor. Operação pesada (segundos).</summary>
    public static LlamaChatEngine Load(ChatEngineOptions options)
    {
        if (!File.Exists(options.ModelPath))
            throw new FileNotFoundException("Arquivo de modelo GGUF não encontrado.", options.ModelPath);

        var parameters = new ModelParams(options.ModelPath)
        {
            ContextSize = options.ContextSize,
            GpuLayerCount = options.GpuLayerCount
        };

        var weights = LLamaWeights.LoadFromFile(parameters);
        var context = weights.CreateContext(parameters);
        return new LlamaChatEngine(weights, context, options);
    }

    private ChatSession CreateSession()
    {
        var executor = new InteractiveExecutor(_context);
        var history = new ChatHistory();
        if (!string.IsNullOrWhiteSpace(_options.SystemPrompt))
            history.AddMessage(AuthorRole.System, _options.SystemPrompt);
        return new ChatSession(executor, history);
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var inferenceParams = new InferenceParams
        {
            MaxTokens = _options.MaxTokens,
            AntiPrompts = _options.AntiPrompts.ToList(),
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = _options.Temperature,
                TopP = _options.TopP
            }
        };

        await foreach (var chunk in _session.ChatAsync(
            new ChatHistory.Message(AuthorRole.User, userMessage),
            inferenceParams,
            cancellationToken))
        {
            yield return chunk;
        }
    }

    public void Reset() => _session = CreateSession();

    public void Dispose()
    {
        _context.Dispose();
        _weights.Dispose();
    }
}
