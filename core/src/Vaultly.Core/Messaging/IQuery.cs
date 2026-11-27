using MediatR;

namespace Vaultly.Core.Messaging;

/// <summary>
/// Marks a side-effect free request (read side of CQRS). Queries never load aggregates
/// for mutation and never call <c>IUnitOfWork.SaveChangesAsync</c>; they read DTOs from read services.
/// </summary>
/// <typeparam name="TResponse">Read model returned by the query.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;
