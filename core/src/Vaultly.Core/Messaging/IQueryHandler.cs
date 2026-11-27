using MediatR;

namespace Vaultly.Core.Messaging;

/// <summary>
/// Handles a query and returns a read model.
/// </summary>
/// <typeparam name="TQuery">Query handled by the implementation.</typeparam>
/// <typeparam name="TResponse">Read model returned by the query.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
