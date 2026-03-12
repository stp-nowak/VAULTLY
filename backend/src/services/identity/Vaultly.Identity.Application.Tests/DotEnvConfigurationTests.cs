using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Vaultly.Identity.Api.Configuration;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class DotEnvConfigurationTests
{
    private string _temporaryDirectory = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "vaultly-dotenv-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    [Test]
    public void DotEnvConfigurationSource_WhenKeyExistsInJson_OverridesJsonValue()
    {
        var dotEnvPath = CreateDotEnvFile("Identity__Issuer=https://identity.from-dotenv");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Identity:Issuer"] = "https://identity.from-json" })
            .Add(new DotEnvConfigurationSource(dotEnvPath, optional: false))
            .Build();

        Assert.That(configuration["Identity:Issuer"], Is.EqualTo("https://identity.from-dotenv"));
    }

    [Test]
    public void AddOptionalDotEnvFile_WhenEnvironmentVariablesExist_KeepsEnvironmentVariablePriority()
    {
        const string key = "VaultlyDotEnvTests__Issuer";
        var dotEnvPath = CreateDotEnvFile($"{key}=from-dotenv");

        Environment.SetEnvironmentVariable(key, "from-environment");

        try
        {
            var configuration = new ConfigurationManager();
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["VaultlyDotEnvTests:Issuer"] = "from-json" });
            configuration.AddEnvironmentVariables();
            configuration.AddOptionalDotEnvFile(dotEnvPath);

            Assert.That(configuration["VaultlyDotEnvTests:Issuer"], Is.EqualTo("from-environment"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Test]
    public void DotEnvConfigurationSource_WhenValueContainsEscapedNewlines_UnescapesThem()
    {
        var dotEnvPath = CreateDotEnvFile("Identity__JwtPrivateKeyPem=\"line-one\\nline-two\"");

        var configuration = new ConfigurationBuilder()
            .Add(new DotEnvConfigurationSource(dotEnvPath, optional: false))
            .Build();

        Assert.That(configuration["Identity:JwtPrivateKeyPem"], Is.EqualTo("line-one\nline-two"));
    }

    [Test]
    public void DotEnvConfigurationSource_WhenLineIsInvalid_ThrowsFormatException()
    {
        var dotEnvPath = CreateDotEnvFile("INVALID_LINE");

        Assert.That(
            () => new ConfigurationBuilder()
                .Add(new DotEnvConfigurationSource(dotEnvPath, optional: false))
                .Build(),
            Throws.TypeOf<FormatException>()
                .With.Message.Contains("Invalid .env entry"));
    }

    private string CreateDotEnvFile(string content)
    {
        var path = Path.Combine(_temporaryDirectory, ".env.test");
        File.WriteAllText(path, content);
        return path;
    }
}
