using Vaultly.Core.Messaging;

namespace Vaultly.Identity.Application.Features.SignInMethods.GetSignInSettings;

/// <summary>
/// Every sign-in method with whether it is configured and whether admins allow it.
/// </summary>
public sealed record GetSignInSettingsQuery : IQuery<IReadOnlyList<SignInMethodDto>>;
