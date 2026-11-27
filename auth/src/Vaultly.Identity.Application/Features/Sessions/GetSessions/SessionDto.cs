namespace Vaultly.Identity.Application.Features.Sessions.GetSessions;

public sealed record SessionDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUsedAt,
    string? UserAgent,
    string? IpAddress,
    bool IsRevoked,
    bool IsCurrent);