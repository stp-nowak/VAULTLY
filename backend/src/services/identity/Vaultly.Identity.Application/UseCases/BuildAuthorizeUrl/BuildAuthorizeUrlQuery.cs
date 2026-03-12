using MediatR;

using Microsoft.Extensions.Options;

using Serilog;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.UseCases.OAuth;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Application.UseCases.BuildAuthorizeUrl;

/// <summary>
/// Query to build an OAuth authorization URL.
/// </summary>
public sealed record BuildAuthorizeUrlQuery(string ProviderId, string? RedirectUri) : IRequest<string>;

/// <summary>
/// Handles building an OAuth authorization URL.
/// </summary>
public sealed class BuildAuthorizeUrlHandler(
    IOAuthStateRepository oauthStateRepository,
    IUnitOfWork unitOfWork,
    IEnumerable<IExternalOAuthProvider> providers,
    ITokenGenerator tokenGenerator,
    IClock clock,
    IOptions<IdentityOptions> identityOptions,
    ILogger logger) : IRequestHandler<BuildAuthorizeUrlQuery, string>
{
    private readonly IOAuthStateRepository _oauthStateRepository = oauthStateRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IReadOnlyDictionary<ProviderId, IExternalOAuthProvider> _providers = providers.ToDictionary(x => x.Id);
    private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
    private readonly IClock _clock = clock;
    private readonly IdentityOptions _identityOptions = identityOptions.Value;
    private readonly ILogger _logger = logger.ForContext<BuildAuthorizeUrlHandler>();

    /// <summary>
    /// Builds the authorization URL for the requested provider.
    /// </summary>
    public async Task<string> Handle(BuildAuthorizeUrlQuery request, CancellationToken cancellationToken)
    {
        var provider = OAuthFlowUtilities.GetProvider(_providers, new ProviderId(request.ProviderId));
        if (!provider.IsEnabled)
        {
            _logger.Warning("OAuth provider {ProviderId} is disabled", provider.Id.Value);
            throw new InvalidOperationException("OAuth provider is disabled.");
        }

        var now = _clock.UtcNow;
        var redirectUri = OAuthFlowUtilities.ValidateRedirectUri(request.RedirectUri, _identityOptions);

        var state = OAuthState.Create(
            new OAuthStateValue(_tokenGenerator.GenerateToken(32)),
            new CodeVerifier(_tokenGenerator.GenerateToken(64)),
            new Nonce(_tokenGenerator.GenerateToken(32)),
            now.AddMinutes(_identityOptions.OAuthStateTtlMinutes),
            redirectUri);

        _oauthStateRepository.Add(state);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var authorizeUrl = OAuthFlowUtilities.BuildUrl(provider.AuthorizationEndpoint, new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = provider.ClientId,
            ["redirect_uri"] = provider.RedirectUri,
            ["scope"] = string.Join(' ', provider.Scopes),
            ["state"] = state.State.Value,
            ["code_challenge"] = OAuthFlowUtilities.CreateCodeChallenge(state.CodeVerifier.Value),
            ["code_challenge_method"] = "S256",
            ["nonce"] = state.Nonce.Value
        });

        _logger.Information("Built OAuth authorize URL for provider {ProviderId}", provider.Id.Value);
        return authorizeUrl;
    }
}