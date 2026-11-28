using System.Text.Json;
using System.Text.Json.Serialization;

using Vaultly.Core;

namespace Vaultly.Accounts.Api.Http;

/// <summary>
/// JSON settings of the Accounts API: every enum is a <see cref="WireNames"/> string and every number a JSON number,
/// never a string, so its OpenAPI contract (from which the web SPA generates its types) says exactly that.
/// </summary>
public static class ApiJson
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(WireNames.Policy, allowIntegerValues: false));
        options.NumberHandling = JsonNumberHandling.Strict;
    }
}
