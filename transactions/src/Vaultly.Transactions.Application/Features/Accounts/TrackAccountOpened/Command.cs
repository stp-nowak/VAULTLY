using Vaultly.Core.Messaging;

namespace Vaultly.Transactions.Application.Features.Accounts.TrackAccountOpened;

/// <summary>Accounts opened an account; Transactions keeps what it needs to know of it.</summary>
public sealed record TrackAccountOpenedCommand(Guid AccountId, Guid OwnerId, string Currency, int Version) : ICommand;
