using Rebus.Bus;

namespace Vaultly.Core.Messaging.Rebus;

internal sealed class RebusMessageBus(IBus bus) : IMessageBus
{
    public Task PublishAsync(object @event) => bus.Publish(@event);

    public Task SendAsync(object command) => bus.Send(command);

    public Task SendLocalAsync(object message) => bus.SendLocal(message);

    public Task ReplyAsync(object message) => bus.Reply(message);
}
