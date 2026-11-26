using Vaultly.Identity.Domain.Enums;

namespace Vaultly.Identity.Application.Models;

/// <summary>
/// The user signed in through an active SSO session, as the admin console sees them.
/// </summary>
public sealed record ConsoleUserDto(Guid Id, string Email, string Name, UserRole Role);

/// <summary>
/// One user in the admin console's user list.
/// </summary>
/// <param name="SignInMethods">Providers linked to the account, e.g. <c>google</c>.</param>
public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string Name,
    UserRole Role,
    bool EmailVerified,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> SignInMethods);

/// <summary>
/// One page of users, newest first.
/// </summary>
/// <param name="Total">Number of users matching the search across all pages.</param>
public sealed record UserPageDto(IReadOnlyList<UserSummaryDto> Items, int Total, int Page, int PageSize);
