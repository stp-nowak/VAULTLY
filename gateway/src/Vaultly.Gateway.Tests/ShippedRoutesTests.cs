using System.Runtime.CompilerServices;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

namespace Vaultly.Gateway.Tests;

/// <summary>
/// The routes the gateway ships with (appsettings.json): a cluster id names the service and is the audience of its
/// internal tokens, so a typo here locks a service out.
/// </summary>
[TestFixture]
public sealed class ShippedRoutesTests
{
    private static readonly IConfiguration Settings = new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(GatewayProjectDirectory(), "appsettings.json"))
        .Build();

    [TestCase("accounts", "/accounts/{**catch-all}")]
    public void Service_IsRoutedByItsPathPrefix_ToItsOwnCluster(string service, string path)
    {
        var route = Settings.GetSection($"ReverseProxy:Routes:{service}");

        Assert.Multiple(() =>
        {
            Assert.That(route["ClusterId"], Is.EqualTo(service));
            Assert.That(route["Match:Path"], Is.EqualTo(path));
            Assert.That(route["AuthorizationPolicy"], Is.Null, "Domain services are for signed-in users.");
        });
    }

    [Test]
    public void EveryRoute_PointsAtAClusterWithADestination()
    {
        var missing = Settings.GetSection("ReverseProxy:Routes").GetChildren()
            .Select(route => route["ClusterId"])
            .Where(cluster => !Settings.GetSection($"ReverseProxy:Clusters:{cluster}:Destinations").GetChildren().Any())
            .ToList();

        Assert.That(missing, Is.Empty);
    }

    private static string GatewayProjectDirectory([CallerFilePath] string testFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "..", "Vaultly.Gateway"));
}
