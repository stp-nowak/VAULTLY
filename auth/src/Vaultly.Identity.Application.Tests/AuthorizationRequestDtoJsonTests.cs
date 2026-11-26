using System.Text.Json;

using NUnit.Framework;

using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class AuthorizationRequestDtoJsonTests
{
    [TestCase(AuthorizationRequestStatus.Pending, "pending")]
    [TestCase(AuthorizationRequestStatus.Expired, "expired")]
    [TestCase(AuthorizationRequestStatus.Completed, "completed")]
    public void Status_IsWrittenAsTheLowercaseNameTheLoginUiExpects(AuthorizationRequestStatus status, string expected)
    {
        var json = JsonSerializer.Serialize(
            new AuthorizationRequestDto(Guid.Empty, "Vaultly", status, [], PasswordSignIn: true),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var document = JsonDocument.Parse(json);
        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("status").GetString(), Is.EqualTo(expected));
            Assert.That(document.RootElement.GetProperty("passwordSignIn").GetBoolean(), Is.True);
        });
    }
}
