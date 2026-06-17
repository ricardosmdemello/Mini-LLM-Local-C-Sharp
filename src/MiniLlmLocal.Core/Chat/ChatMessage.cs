namespace MiniLlmLocal.Core.Chat;

/// <summary>Papel de quem emitiu a mensagem em uma conversa.</summary>
public enum ChatRole
{
    System,
    User,
    Assistant
}

/// <summary>Uma única mensagem de chat (papel + conteúdo).</summary>
public sealed record ChatMessage(ChatRole Role, string Content);
