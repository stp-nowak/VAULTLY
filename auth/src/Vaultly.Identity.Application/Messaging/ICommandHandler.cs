using MediatR;

namespace Vaultly.Identity.Application.Messaging;

/// <summary>
/// Handles a command without a result. Wrapping MediatR keeps handlers explicit about
/// being on the write side and lets pipeline behaviors target commands only.
/// </summary>
/// <typeparam name="TCommand">Command handled by the implementation.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand;

/// <summary>
/// Handles a command that returns a result.
/// </summary>
/// <typeparam name="TCommand">Command handled by the implementation.</typeparam>
/// <typeparam name="TResponse">Result produced by the command.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
