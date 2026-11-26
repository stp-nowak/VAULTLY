using System.Text.Json.Serialization;

namespace Vaultly.Identity.Application.Models;

/// <summary>Where a sign-in for an authorization request stands, as the login UI sees it.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AuthorizationRequestStatus>))]
public enum AuthorizationRequestStatus
{
    /// <summary>The user can still sign in.</summary>
    [JsonStringEnumMemberName("pending")]
    Pending,

    [JsonStringEnumMemberName("expired")]
    Expired,

    /// <summary>The user already signed in; the link cannot be used again.</summary>
    [JsonStringEnumMemberName("completed")]
    Completed,
}
