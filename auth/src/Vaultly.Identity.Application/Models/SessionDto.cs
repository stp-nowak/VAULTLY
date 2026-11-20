namespace Vaultly.Identity.Application.Models;

public sealed record SessionDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUsedAt,
    string? UserAgent,
    string? IpAddress,
    bool IsRevoked,
    bool IsCurrent);