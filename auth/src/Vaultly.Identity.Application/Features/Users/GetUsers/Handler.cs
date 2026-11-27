using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Features.Users.GetUsers;

public sealed class GetUsersHandler(IUserReadService readService) : IQueryHandler<GetUsersQuery, UserPageDto>
{
    public const int MaxPageSize = 100;

    private readonly IUserReadService _readService = readService;

    public Task<UserPageDto> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        => _readService.GetPageAsync(
            NormalizeSearch(request.Search),
            Math.Max(1, request.Page),
            Math.Clamp(request.PageSize, 1, MaxPageSize),
            cancellationToken);

    private static string? NormalizeSearch(string? search)
    {
        var trimmed = search?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed[..Math.Min(trimmed.Length, EmailAddress.MaxLength)];
    }
}
