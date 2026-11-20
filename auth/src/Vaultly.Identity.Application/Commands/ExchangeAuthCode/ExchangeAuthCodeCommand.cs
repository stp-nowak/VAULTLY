using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Commands.ExchangeAuthCode;

/// <summary>
/// Command to exchange an auth code for tokens.
/// </summary>
public sealed record ExchangeAuthCodeCommand(string Code) : ICommand<ExchangeResult>;
