using MediatR;

namespace Vaultly.Core.Messaging;

/// <summary>
/// Marks a request that changes state (write side of CQRS) and returns no value.
/// Commands go through aggregates and repositories so domain invariants are always enforced.
/// </summary>
public interface ICommand : IRequest;

/// <summary>
/// Marks a request that changes state (write side of CQRS) and returns a result,
/// e.g. identifiers or tokens produced by the state change.
/// </summary>
/// <typeparam name="TResponse">Result produced by the command.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;
