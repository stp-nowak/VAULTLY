using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Security;

namespace Vaultly.Identity.Application.Features.Users.GetConsoleUser;

public sealed class GetConsoleUserHandler(IUserReadService readService, ITokenHasher tokenHasher, TimeProvider clock)
    : IQueryHandler<GetConsoleUserQuery, ConsoleUserDto?>
{
    private readonly IUserReadService _readService = readService;
    private readonly ITokenHasher _tokenHasher = tokenHasher;
    private readonly TimeProvider _clock = clock;

    public async Task<ConsoleUserDto?> Handle(GetConsoleUserQuery request, CancellationToken cancellationToken)
        => string.IsNullOrEmpty(request.SsoToken)
            ? null
            : await _readService.GetBySsoTokenHashAsync(_tokenHasher.HashToken(request.SsoToken), _clock.GetUtcNow(), cancellationToken);
}
