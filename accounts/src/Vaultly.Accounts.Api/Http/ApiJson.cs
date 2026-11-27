using System.Text.Json;
using System.Text.Json.Serialization;

using Vaultly.Core;

namespace Vaultly.Accounts.Api.Http;

/// <summary>JSON settings of the Accounts API: every enum is a <see cref="WireNames"/> string, never a number.</summary>
public static class ApiJson
{
    public static void Configure(JsonSerializerOptions options)
        => options.Converters.Add(new JsonStringEnumConverter(WireNames.Policy, allowIntegerValues: false));
}
