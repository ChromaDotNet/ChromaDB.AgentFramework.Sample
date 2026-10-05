using Microsoft.Extensions.AI;

namespace ChromaDB.AgentFramework.Sample.Tests;

/// <summary>
/// A chat model that answers "Noted." and records the messages the agent sends to it, context included.
/// </summary>
public sealed class RecordingChatClient : IChatClient
{
    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public ChatOptions? LastOptions { get; private set; }

    public string LastMessagesText => string.Join("\n", LastMessages.Select(message => message.Text));

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        LastMessages = messages.ToList();
        LastOptions = options;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Noted.")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var update in (await GetResponseAsync(messages, options, cancellationToken)).ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
