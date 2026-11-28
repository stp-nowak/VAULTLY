using Vaultly.Core.Messaging;

namespace Vaultly.Transactions.Application.Features.Accounts.TrackAccountClosed;

/// <summary>Accounts closed an account; nothing new may be recorded on it.</summary>
public sealed record TrackAccountClosedCommand(Guid AccountId, Guid OwnerId, int Version) : ICommand;
