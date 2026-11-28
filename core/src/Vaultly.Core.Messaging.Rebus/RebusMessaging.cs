using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Rebus.Config;
using Rebus.Config.Outbox;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Sagas.Idempotent;
using Rebus.Serialization.Json;

using Vaultly.Core.Persistence;

namespace Vaultly.Core.Messaging.Rebus;

public static class RebusMessaging
{
    public const string SectionName = "Messaging";
    public const string RabbitMqConnectionName = "RabbitMq";
    public const int MaxDeliveryAttempts = 5;

    /// <summary>Messages on the wire: enums as <c>snake_case</c> strings, like every other wire format.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(WireNames.Policy, allowIntegerValues: false) }
    };

    // The build writes each API's OpenAPI document by starting the app inside this tool; no broker is around then.
    private static bool IsWritingOpenApiDocument => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    /// <summary>
    /// Connects the service to RabbitMQ through Rebus and registers <see cref="IMessageBus"/>. Handlers
    /// (<c>IHandleMessages&lt;T&gt;</c>) are registered by the service. With <c>Messaging:Enabled = false</c> (API
    /// tests) nothing is registered and the test provides its own <see cref="IMessageBus"/>.
    /// </summary>
    public static IServiceCollection AddRebusMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        MessagingEndpoint endpoint)
    {
        if (!configuration.GetSection(SectionName).GetValue("Enabled", defaultValue: true) || IsWritingOpenApiDocument)
        {
            return services;
        }

        var rabbitMq = configuration.GetConnectionString(RabbitMqConnectionName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{RabbitMqConnectionName} is not set.");
        var database = PostgresOptions.ConnectionString(configuration.GetConnectionString(endpoint.DatabaseConnectionName));

        services.AddRebus(
            configure =>
            {
                configure
                    .Transport(transport => transport.UseRabbitMq(rabbitMq, endpoint.Queue))
                    .Routing(routing =>
                    {
                        var router = routing.TypeBased();
                        foreach (var (command, owner) in endpoint.CommandOwners)
                        {
                            router.Map(command, owner);
                        }
                    })
                    .Serialization(serialization => serialization.UseSystemTextJson(JsonOptions))
                    .Outbox(outbox => outbox.StoreInPostgreSql(database, "rebus_outbox"))
                    .Options(options =>
                    {
                        options.RetryStrategy(errorQueueName: $"{endpoint.Queue}.error", maxDeliveryAttempts: MaxDeliveryAttempts);
                        SendFromHandlersOnCommit(options);
                        if (endpoint.Sagas)
                        {
                            options.EnableIdempotentSagas();
                        }
                    });

                if (endpoint.Sagas)
                {
                    configure
                        .Sagas(sagas => sagas.StoreInPostgres(database, "rebus_sagas", "rebus_saga_index"))
                        .Timeouts(timeouts => timeouts.StoreInPostgres(database, "rebus_timeouts"));
                }

                return configure;
            },
            onCreated: async bus =>
            {
                foreach (var @event in endpoint.Subscriptions)
                {
                    await bus.Subscribe(@event);
                }
            });

        services.AddTransient<IMessageBus, RebusMessageBus>();
        return services;
    }

    // WORKAROUND, remove when https://github.com/rebus-org/Rebus.PostgreSql/issues/55 is fixed.
    // Rebus.PostgreSql's outbox step commits a handler's database transaction before it saves the handler's outgoing
    // messages into it, so on .NET 10 / Npgsql 10 every message a handler sends fails with "Transaction is already
    // completed". Without the step a handler's messages skip the outbox: Rebus sends them after the handler finishes
    // and before the incoming message is acknowledged, and a failed send redelivers the incoming message — at least
    // once, which is safe because handlers are idempotent. Sends outside handlers (API requests through
    // OutboxUnitOfWork) still go through the outbox. Once the issue is fixed: delete this method and its call, and let
    // OutboxUnitOfWork enlist handlers in the outbox too.
    private static void SendFromHandlersOnCommit(OptionsConfigurer options)
        => options.Decorate<IPipeline>(context => new PipelineStepRemover(context.Get<IPipeline>())
            .RemoveIncomingStep(step => step.GetType().Name == "OutboxIncomingStep"));
}
