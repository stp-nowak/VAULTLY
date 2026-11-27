namespace Vaultly.Identity.ApiContracts;

/// <summary>Body of <c>GET /sessions</c>: the caller's sign-in sessions.</summary>
public sealed record SessionsResponse(IReadOnlyList<SessionResponse> Sessions);

/// <summary>One session; <c>IsCurrent</c> marks the session of the access token that made the call.</summary>
public sealed record SessionResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUsedAt,
    string? UserAgent,
    string? IpAddress,
    bool IsRevoked,
    bool IsCurrent);
