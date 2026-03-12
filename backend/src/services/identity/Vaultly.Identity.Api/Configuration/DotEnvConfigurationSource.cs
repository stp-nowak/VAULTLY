using System.Text;

using Microsoft.Extensions.Configuration;

namespace Vaultly.Identity.Api.Configuration;

/// <summary>
/// Represents a configuration source that reads key-value pairs from a dotenv file.
/// </summary>
public sealed class DotEnvConfigurationSource(string path, bool optional = true) : IConfigurationSource
{
    /// <summary>
    /// Builds a provider that loads the configured dotenv file.
    /// </summary>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new DotEnvConfigurationProvider(path, optional);
    }
}

/// <summary>
/// Loads dotenv entries into the standard .NET configuration key space.
/// </summary>
public sealed class DotEnvConfigurationProvider(string path, bool optional) : ConfigurationProvider
{
    private const string ExportPrefix = "export ";

    /// <summary>
    /// Loads and normalizes dotenv values from disk.
    /// </summary>
    public override void Load()
    {
        if (!File.Exists(path))
        {
            if (optional)
            {
                Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            throw new FileNotFoundException($"The dotenv configuration file '{path}' was not found.", path);
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var lines = File.ReadAllLines(path);

        for (var lineNumber = 0; lineNumber < lines.Length; lineNumber++)
        {
            ParseLine(lines[lineNumber], lineNumber + 1, data);
        }

        Data = data;
    }

    /// <summary>
    /// Parses a single dotenv line and adds it to the configuration dictionary.
    /// </summary>
    private static void ParseLine(string line, int lineNumber, IDictionary<string, string?> data)
    {
        var trimmedLine = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
        {
            return;
        }

        if (trimmedLine.StartsWith(ExportPrefix, StringComparison.Ordinal))
        {
            trimmedLine = trimmedLine[ExportPrefix.Length..].TrimStart();
        }

        var separatorIndex = trimmedLine.IndexOf('=');
        if (separatorIndex < 0)
        {
            throw CreateFormatException(lineNumber, "Expected a KEY=VALUE entry.");
        }

        var key = trimmedLine[..separatorIndex].Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw CreateFormatException(lineNumber, "Configuration keys cannot be empty.");
        }

        var value = ParseValue(trimmedLine[(separatorIndex + 1)..].Trim(), lineNumber);
        data[NormalizeKey(key)] = value;
    }

    /// <summary>
    /// Parses a dotenv value, including quoted values with common escape sequences.
    /// </summary>
    private static string ParseValue(string rawValue, int lineNumber)
    {
        if (rawValue.Length == 0)
        {
            return string.Empty;
        }

        if (rawValue[0] is '"' or '\'')
        {
            if (rawValue.Length < 2 || rawValue[^1] != rawValue[0])
            {
                throw CreateFormatException(lineNumber, "Quoted values must end with the same quote character.");
            }

            return UnescapeValue(rawValue[1..^1]);
        }

        return rawValue;
    }

    /// <summary>
    /// Converts dotenv keys into the hierarchical key format used by .NET configuration.
    /// </summary>
    private static string NormalizeKey(string key) =>
        key.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);

    /// <summary>
    /// Unescapes common sequences so multiline secrets can be expressed safely in dotenv files.
    /// </summary>
    private static string UnescapeValue(string value)
    {
        var builder = new StringBuilder(value.Length);
        var isEscaping = false;

        foreach (var character in value)
        {
            if (!isEscaping)
            {
                if (character == '\\')
                {
                    isEscaping = true;
                    continue;
                }

                builder.Append(character);
                continue;
            }

            builder.Append(character switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                '\\' => '\\',
                '"' => '"',
                '\'' => '\'',
                _ => character
            });
            isEscaping = false;
        }

        if (isEscaping)
        {
            builder.Append('\\');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Creates a consistent format exception for invalid dotenv input.
    /// </summary>
    private static FormatException CreateFormatException(int lineNumber, string reason) =>
        new($"Invalid .env entry at line {lineNumber}. {reason}");
}
