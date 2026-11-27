using MediatR;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace Vaultly.Core.Persistence.Tests;

[TestFixture]
public sealed class UnitOfWorkTests
{
    [Test]
    public async Task SaveChangesAsync_SavesThenPublishesEachDomainEventOnce()
    {
        await using var dbContext = CreateDbContext();
        var publisher = new RecordingPublisher(dbContext);
        var unitOfWork = new UnitOfWork<TestDbContext>(dbContext, publisher);
        var note = Note.Write("first");
        dbContext.Notes.Add(note);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(publisher.Published, Has.Count.EqualTo(1));
            Assert.That(publisher.Published[0], Is.TypeOf<NoteWritten>().With.Property(nameof(NoteWritten.NoteId)).EqualTo(note.Id));
            Assert.That(publisher.SavedWhenPublished, Is.True);
            Assert.That(note.DomainEvents, Is.Empty);
        });
    }

    private static TestDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private sealed record NoteWritten(Guid NoteId) : DomainEvent;

    private sealed class Note : AggregateRoot
    {
        public Guid Id { get; private set; }
        public string Text { get; private set; } = string.Empty;

        public static Note Write(string text)
        {
            var note = new Note { Id = Guid.NewGuid(), Text = text };
            note.RaiseDomainEvent(new NoteWritten(note.Id));
            return note;
        }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<Note> Notes => Set<Note>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<DomainEvent>();
            modelBuilder.Entity<Note>().Ignore(note => note.DomainEvents);
        }
    }

    private sealed class RecordingPublisher(TestDbContext dbContext) : IPublisher
    {
        public List<object> Published { get; } = [];
        public bool SavedWhenPublished { get; private set; }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            SavedWhenPublished = !dbContext.ChangeTracker.HasChanges() && dbContext.Notes.AsNoTracking().Any();
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Publish((object)notification, cancellationToken);
    }
}
