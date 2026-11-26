using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Queries.GetConsoleUser;

public sealed class GetConsoleUserHandler(IUserReadService readService, ITokenHasher tokenHasher, IClock clock)
    : IQueryHandler<GetConsoleUserQuery, ConsoleUserDto?>
{
    private readonly IUserReadService _readService = readService;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly IClock _clock = clock;

    public async Task<ConsoleUserDto?> Handle(GetConsoleUserQuery request, CancellationToken cancellationToken)
        => string.IsNullOrEmpty(request.SsoToken)
            ? null
            : await _readService.GetBySsoTokenHashAsync(_tokenHasher.HashToken(request.SsoToken), _clock.UtcNow, cancellationToken);
}
