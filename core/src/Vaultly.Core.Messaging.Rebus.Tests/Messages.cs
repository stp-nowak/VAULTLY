using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Vaultly.Core.Messaging.Rebus.Tests;

public enum NoteKind
{
    ShoppingList,
    Reminder
}

/// <summary>An integration event, as a service would publish it from its PublicTypes.</summary>
public sealed record NoteWritten(Guid NoteId, NoteKind Kind);

/// <summary>A command another service sends to the notes service.</summary>
public sealed record WriteNote(Guid NoteId, NoteKind Kind);

public sealed class Note : AggregateRoot
{
    public Guid Id { get; private set; }

    public NoteKind Kind { get; private set; }

    public static Note Write(Guid id, NoteKind kind)
    {
        var note = new Note { Id = id, Kind = kind };
        note.RaiseDomainEvent(new NoteWrittenDomainEvent(id, kind));
        return note;
    }
}

public sealed record NoteWrittenDomainEvent(Guid NoteId, NoteKind Kind) : DomainEvent;

public sealed class PublishNoteWritten(IMessageBus bus) : INotificationHandler<NoteWrittenDomainEvent>
{
    public static bool Fail { get; set; }

    public async Task Handle(NoteWrittenDomainEvent notification, CancellationToken cancellationToken)
    {
        await bus.PublishAsync(new NoteWritten(notification.NoteId, notification.Kind));
        if (Fail)
        {
            throw new InvalidOperationException("The unit of work fails after publishing.");
        }
    }
}

public sealed class NotesDbContext(DbContextOptions<NotesDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.Entity<Note>().Property(note => note.Kind).HasConversion<string>();
    }
}
