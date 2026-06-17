namespace MiniLlmLocal.Core.Models;

/// <summary>
/// Catálogo curado de modelos pequenos quantizados (Q4_K_M) que rodam bem em
/// GPU consumer ou mesmo em CPU. Todas as URLs apontam para mirrors GGUF
/// públicos no Hugging Face — sem necessidade de token para os repositórios
/// listados (os pesos originais de Gemma/Llama são "gated" no repo oficial,
/// mas estes mirrors GGUF são abertos).
/// </summary>
public static class ModelCatalog
{
    public static IReadOnlyList<ModelInfo> All { get; } =
    [
        new ModelInfo(
            Id: "gemma-2-2b-it",
            Name: "Gemma 2 2B Instruct",
            Family: ModelFamily.Gemma,
            Description: "Modelo do Google, 2B parâmetros. Ótimo equilíbrio qualidade/tamanho.",
            DownloadUrl: "https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf",
            FileName: "gemma-2-2b-it-Q4_K_M.gguf",
            ApproxSizeBytes: 1_710_000_000,
            License: "Gemma Terms of Use"),

        new ModelInfo(
            Id: "llama-3.2-1b-it",
            Name: "Llama 3.2 1B Instruct",
            Family: ModelFamily.Llama,
            Description: "O menor da família Llama 3.2. Roda até em máquinas modestas.",
            DownloadUrl: "https://huggingface.co/bartowski/Llama-3.2-1B-Instruct-GGUF/resolve/main/Llama-3.2-1B-Instruct-Q4_K_M.gguf",
            FileName: "Llama-3.2-1B-Instruct-Q4_K_M.gguf",
            ApproxSizeBytes: 808_000_000,
            License: "Llama 3.2 Community License"),

        new ModelInfo(
            Id: "llama-3.2-3b-it",
            Name: "Llama 3.2 3B Instruct",
            Family: ModelFamily.Llama,
            Description: "Mais capaz que o 1B; ainda cabe em GPUs de 6–8 GB.",
            DownloadUrl: "https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf",
            FileName: "Llama-3.2-3B-Instruct-Q4_K_M.gguf",
            ApproxSizeBytes: 2_020_000_000,
            License: "Llama 3.2 Community License"),

        new ModelInfo(
            Id: "phi-3-mini-4k-it",
            Name: "Phi-3 Mini 4K Instruct",
            Family: ModelFamily.Phi,
            Description: "Modelo da Microsoft, 3.8B. Forte em raciocínio para o tamanho.",
            DownloadUrl: "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf",
            FileName: "Phi-3-mini-4k-instruct-q4.gguf",
            ApproxSizeBytes: 2_390_000_000,
            License: "MIT")
    ];

    /// <summary>Busca um modelo pelo <see cref="ModelInfo.Id"/> (case-insensitive).</summary>
    public static ModelInfo? FindById(string id) =>
        All.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
}
