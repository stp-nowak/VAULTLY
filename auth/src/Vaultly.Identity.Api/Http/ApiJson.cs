using System.Text.Json;
using System.Text.Json.Serialization;

using Vaultly.Core;

namespace Vaultly.Identity.Api.Http;

/// <summary>JSON settings of the Identity API: every enum is a <see cref="WireNames"/> string, never a number.</summary>
public static class ApiJson
{
    public static void Configure(JsonSerializerOptions options)
        => options.Converters.Add(new JsonStringEnumConverter(WireNames.Policy, allowIntegerValues: false));

    /// <summary>The API's settings on top of the web defaults (camelCase properties), for tests.</summary>
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Configure(options);
        return options;
    }
}
