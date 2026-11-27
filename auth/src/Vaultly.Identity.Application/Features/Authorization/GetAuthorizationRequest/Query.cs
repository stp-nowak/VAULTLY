using Vaultly.Core.Messaging;

namespace Vaultly.Identity.Application.Features.Authorization.GetAuthorizationRequest;

/// <summary>
/// What the login UI shows for an authorization request; <c>null</c> when the id is unknown.
/// </summary>
public sealed record GetAuthorizationRequestQuery(Guid Id) : IQuery<AuthorizationRequestDto?>;
