using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Vaultly.Identity.Api.Configuration;

/// <summary>
/// Adds helpers for layering dotenv configuration into the ASP.NET Core configuration pipeline.
/// </summary>
public static class DotEnvConfigurationExtensions
{
    /// <summary>
    /// Inserts an optional <c>.env</c> file before environment variables so dotenv values override JSON defaults.
    /// </summary>
    public static ConfigurationManager AddOptionalDotEnvFile(this ConfigurationManager configuration, string path)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var dotEnvSource = new DotEnvConfigurationSource(path, optional: true);
        var insertIndex = FindEnvironmentVariablesSourceIndex(configuration.Sources);

        if (insertIndex >= 0)
        {
            configuration.Sources.Insert(insertIndex, dotEnvSource);
            return configuration;
        }

        configuration.Sources.Add(dotEnvSource);
        return configuration;
    }

    /// <summary>
    /// Finds the default environment variable source so dotenv values can be layered beneath it.
    /// </summary>
    private static int FindEnvironmentVariablesSourceIndex(IList<IConfigurationSource> sources)
    {
        for (var index = 0; index < sources.Count; index++)
        {
            if (sources[index] is EnvironmentVariablesConfigurationSource { Prefix: null })
            {
                return index;
            }
        }

        return -1;
    }
}
