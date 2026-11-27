using Vaultly.Core.Messaging;

namespace Vaultly.Identity.Application.Features.Users.GetUsers;

/// <summary>
/// One page of users for the admin console, optionally filtered by email or name.
/// </summary>
/// <param name="Page">1-based; values below 1 mean the first page.</param>
/// <param name="PageSize">Clamped to 1–<see cref="GetUsersHandler.MaxPageSize"/>.</param>
public sealed record GetUsersQuery(string? Search, int Page, int PageSize) : IQuery<UserPageDto>;
