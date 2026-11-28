using Rebus.Sagas;

using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Infrastructure.Postings;

public sealed class PostingSagaData : SagaData
{
    public Guid StepId { get; set; }

    public Guid TransactionId { get; set; }

    public Guid AccountId { get; set; }

    public Guid OwnerId { get; set; }

    public PostingStep Step { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "";

    public DateOnly BookedOn { get; set; }

    /// <summary>How many times <c>PostTransaction</c> was sent for this step.</summary>
    public int Attempts { get; set; }
}
