namespace Vaultly.Identity.Application.Models;

/// <summary>Where a sign-in for an authorization request stands, as the login UI sees it.</summary>
public enum AuthorizationRequestStatus
{
    /// <summary>The user can still sign in.</summary>
    Pending,

    Expired,

    /// <summary>The user already signed in; the link cannot be used again.</summary>
    Completed,
}
