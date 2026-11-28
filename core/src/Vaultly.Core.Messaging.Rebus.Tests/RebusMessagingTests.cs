using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NUnit.Framework;

using Rebus.Config;
using Rebus.Handlers;

using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

using Vaultly.Core.Persistence;

namespace Vaultly.Core.Messaging.Rebus.Tests;

[TestFixture]
[NonParallelizable]
public sealed class RebusMessagingTests
{
    private const string NotesQueue = "notes";
    private const string ListenerQueue = "listener";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management-alpine").Build();
    private readonly List<ServiceProvider> _services = [];

    [OneTimeSetUp]
    public async Task StartContainers()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());
        await using var db = NotesDb(_postgres.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task StopContainers()
    {
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    [SetUp]
    public void Reset()
    {
        Listener.Received.Clear();
        PublishNoteWritten.Fail = false;
        FailingWriteNote.Attempts = 0;
    }

    [TearDown]
    public async Task StopServices()
    {
        foreach (var provider in _services)
        {
            foreach (var hostedService in provider.GetServices<IHostedService>())
            {
                await hostedService.StopAsync(CancellationToken.None);
            }

            await provider.DisposeAsync();
        }

        _services.Clear();
    }

    [Test]
    public async Task CommittedUnitOfWork_PublishesItsEvents()
    {
        await StartListener();
        var notes = await StartNotesService();
        var id = Guid.NewGuid();

        await WriteNoteThroughUnitOfWork(notes, id);

        Assert.That(await Listener.WaitFor(id), Is.EqualTo(new NoteWritten(id, NoteKind.Reminder)));
    }

    [Test]
    public async Task FailedUnitOfWork_PublishesNothing_AndSavesNothing()
    {
        await StartListener();
        var notes = await StartNotesService();
        var id = Guid.NewGuid();
        PublishNoteWritten.Fail = true;

        Assert.ThrowsAsync<InvalidOperationException>(() => WriteNoteThroughUnitOfWork(notes, id));

        Assert.That(await Listener.WaitFor(id, TimeSpan.FromSeconds(3)), Is.Null);
        Assert.That(await NoteExists(id), Is.False);
    }

    [Test]
    public async Task Handler_SavesItsWork_AndPublishesItsEvents()
    {
        await StartListener();
        var notes = await StartNotesService(services => services.AddRebusHandler<WriteNoteHandler>());
        var id = Guid.NewGuid();

        await notes.GetRequiredService<IMessageBus>().SendLocalAsync(new WriteNote(id, NoteKind.ShoppingList));

        Assert.That(await Listener.WaitFor(id), Is.EqualTo(new NoteWritten(id, NoteKind.ShoppingList)));
        Assert.That(await NoteExists(id), Is.True);
    }

    [Test]
    public async Task MessageThatKeepsFailing_GoesToTheErrorQueue()
    {
        var notes = await StartNotesService(services => services.AddRebusHandler<FailingWriteNote>(), queue: "failing");
        var errors = await StartService("failing.error", services => services.AddRebusHandler<ErrorQueueReader>());
        var id = Guid.NewGuid();

        await notes.GetRequiredService<IMessageBus>().SendLocalAsync(new WriteNote(id, NoteKind.Reminder));

        Assert.That(await ErrorQueueReader.WaitFor(id), Is.True);
        Assert.That(FailingWriteNote.Attempts, Is.EqualTo(RebusMessaging.MaxDeliveryAttempts));
        GC.KeepAlive(errors);
    }

    [Test]
    public void Enums_TravelAsSnakeCaseNames()
    {
        var json = JsonSerializer.Serialize(new NoteWritten(Guid.Empty, NoteKind.ShoppingList), RebusMessaging.JsonOptions);

        Assert.That(json, Does.Contain("\"kind\":\"shopping_list\""));
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NoteWritten>($"{{\"noteId\":\"{Guid.Empty}\",\"kind\":1}}", RebusMessaging.JsonOptions));
    }

    private async Task WriteNoteThroughUnitOfWork(IServiceProvider notes, Guid id)
    {
        await using var scope = notes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<NotesDbContext>().Notes.Add(Note.Write(id, NoteKind.Reminder));
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    private async Task<bool> NoteExists(Guid id)
    {
        await using var db = NotesDb(_postgres.GetConnectionString());
        return await db.Notes.AnyAsync(note => note.Id == id);
    }

    private async Task StartListener()
    {
        var listener = await StartService(ListenerQueue, services => services.AddRebusHandler<Listener>(), typeof(NoteWritten));
        await Task.Delay(500);
        GC.KeepAlive(listener);
    }

    private Task<ServiceProvider> StartNotesService(Action<IServiceCollection>? handlers = null, string queue = NotesQueue)
        => StartService(queue, services =>
        {
            services.AddDbContext<NotesDbContext>(options => options.UsePostgres(_postgres.GetConnectionString()));
            services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(NotesDbContext).Assembly));
            services.AddScoped<IUnitOfWork, OutboxUnitOfWork<NotesDbContext>>();
            handlers?.Invoke(services);
        });

    private async Task<ServiceProvider> StartService(string queue, Action<IServiceCollection> configure, params Type[] subscriptions)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{RebusMessaging.RabbitMqConnectionName}"] = _rabbitMq.GetConnectionString(),
                ["ConnectionStrings:NotesDb"] = _postgres.GetConnectionString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRebusMessaging(configuration, new MessagingEndpoint(queue, "NotesDb") { Subscriptions = subscriptions });
        configure(services);

        var provider = services.BuildServiceProvider();
        _services.Add(provider);
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        return provider;
    }

    private static NotesDbContext NotesDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<NotesDbContext>();
        options.UsePostgres(connectionString);
        return new NotesDbContext(options.Options);
    }

    private sealed class Listener : IHandleMessages<NoteWritten>
    {
        public static ConcurrentDictionary<Guid, NoteWritten> Received { get; } = new();

        public Task Handle(NoteWritten message)
        {
            Received[message.NoteId] = message;
            return Task.CompletedTask;
        }

        public static async Task<NoteWritten?> WaitFor(Guid id, TimeSpan? timeout = null)
        {
            var until = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(20));
            while (DateTime.UtcNow < until)
            {
                if (Received.TryGetValue(id, out var message))
                {
                    return message;
                }

                await Task.Delay(100);
            }

            return null;
        }
    }

    private sealed class WriteNoteHandler(NotesDbContext db, IUnitOfWork unitOfWork) : IHandleMessages<WriteNote>
    {
        public async Task Handle(WriteNote message)
        {
            db.Notes.Add(Note.Write(message.NoteId, message.Kind));
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
    }

    private sealed class FailingWriteNote : IHandleMessages<WriteNote>
    {
        public static int Attempts;

        public Task Handle(WriteNote message)
        {
            Interlocked.Increment(ref Attempts);
            throw new InvalidOperationException("Always fails.");
        }
    }

    private sealed class ErrorQueueReader : IHandleMessages<WriteNote>
    {
        private static readonly ConcurrentDictionary<Guid, bool> Seen = new();

        public Task Handle(WriteNote message)
        {
            Seen[message.NoteId] = true;
            return Task.CompletedTask;
        }

        public static async Task<bool> WaitFor(Guid id)
        {
            var until = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < until && !Seen.ContainsKey(id))
            {
                await Task.Delay(100);
            }

            return Seen.ContainsKey(id);
        }
    }
}
