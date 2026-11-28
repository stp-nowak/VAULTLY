using System.Collections.Concurrent;

using Vaultly.Core.Messaging;

namespace Vaultly.Core.Testing;

/// <summary>
/// An <see cref="IMessageBus"/> for API tests that run with <c>Messaging:Enabled = false</c>: keeps every message
/// instead of sending it.
/// </summary>
public sealed class RecordingMessageBus : IMessageBus
{
    private readonly ConcurrentQueue<object> _messages = new();

    public IReadOnlyCollection<object> Messages => _messages;

    public IEnumerable<TMessage> OfType<TMessage>() => _messages.OfType<TMessage>();

    public Task PublishAsync(object @event) => Record(@event);

    public Task SendAsync(object command) => Record(command);

    public Task SendLocalAsync(object message) => Record(message);

    public Task ReplyAsync(object message) => Record(message);

    private Task Record(object message)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }
}
