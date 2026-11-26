using System.Text.Json;

using NUnit.Framework;

using Vaultly.Identity.Api;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Domain.Enums;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class ApiJsonTests
{
    private static readonly JsonSerializerOptions Options = ApiJson.CreateOptions();

    [TestCase(AuthorizationRequestStatus.Pending, "pending")]
    [TestCase(AuthorizationRequestStatus.Expired, "expired")]
    [TestCase(AuthorizationRequestStatus.Completed, "completed")]
    public void AuthorizationRequestStatus_IsWrittenAsTheNameTheLoginUiExpects(AuthorizationRequestStatus status, string expected)
    {
        using var document = Serialize(new AuthorizationRequestDto(Guid.Empty, "Vaultly", status, [], PasswordSignIn: true));

        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("status").GetString(), Is.EqualTo(expected));
            Assert.That(document.RootElement.GetProperty("passwordSignIn").GetBoolean(), Is.True);
        });
    }

    [TestCase(UserRole.User, "user")]
    [TestCase(UserRole.Admin, "admin")]
    public void UserRole_IsWrittenAsTheNameTheLoginUiExpects(UserRole role, string expected)
    {
        using var document = Serialize(new ConsoleUserDto(Guid.Empty, "ada@example.com", "Ada", role));

        Assert.That(document.RootElement.GetProperty("role").GetString(), Is.EqualTo(expected));
    }

    [TestCase(ApiError.InvalidCredentials, "invalid_credentials")]
    [TestCase(ApiError.SignInMethodDisabled, "sign_in_method_disabled")]
    [TestCase(ApiError.CsrfHeaderMissing, "csrf_header_missing")]
    public void ApiError_IsWrittenInSnakeCase(ApiError error, string expected)
    {
        using var document = Serialize(new ApiErrorResponse(error));

        Assert.That(document.RootElement.GetProperty("error").GetString(), Is.EqualTo(expected));
    }

    [Test]
    public void AssignRoleRequest_ReadsTheRoleName()
    {
        var request = JsonSerializer.Deserialize<AdminEndpoints.AssignRoleRequest>("""{"role":"admin"}""", Options);

        Assert.That(request!.Role, Is.EqualTo(UserRole.Admin));
    }

    [TestCase("""{"role":"owner"}""")]
    [TestCase("""{"role":1}""")]
    public void AssignRoleRequest_RejectsAnythingButARoleName(string json)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AdminEndpoints.AssignRoleRequest>(json, Options));

    private static JsonDocument Serialize<T>(T value) => JsonDocument.Parse(JsonSerializer.Serialize(value, Options));
}
