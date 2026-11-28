namespace Vaultly.Core.Messaging;

/// <summary>
/// Messages to other services. Events are published to every service subscribed to them; commands are sent to the
/// one service that owns them. Called while a unit of work is open, a message leaves only if that work commits.
/// </summary>
public interface IMessageBus
{
    Task PublishAsync(object @event);

    Task SendAsync(object command);

    /// <summary>Sends a message to this service's own queue, e.g. to start a saga.</summary>
    Task SendLocalAsync(object message);

    /// <summary>Answers the message being handled, to the service that sent it.</summary>
    Task ReplyAsync(object message);
}
