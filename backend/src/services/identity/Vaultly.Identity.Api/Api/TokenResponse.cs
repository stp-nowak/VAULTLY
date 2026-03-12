using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Api;

public sealed record TokenResponse(
    string AccessToken,
    int ExpiresIn,
    Guid SessionId,
    AuthenticatedUserDto User);