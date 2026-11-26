using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Queries.GetSignInSettings;

/// <summary>
/// Every sign-in method with whether it is configured and whether admins allow it.
/// </summary>
public sealed record GetSignInSettingsQuery : IQuery<IReadOnlyList<SignInMethodDto>>;
