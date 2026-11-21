using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Queries.GetAuthorizationRequest;

/// <summary>
/// What the login UI shows for an authorization request; <c>null</c> when the id is unknown.
/// </summary>
public sealed record GetAuthorizationRequestQuery(Guid Id) : IQuery<AuthorizationRequestDto?>;
