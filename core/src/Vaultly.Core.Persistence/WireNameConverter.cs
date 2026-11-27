using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Vaultly.Core.Persistence;

/// <summary>Stores an enum as its <see cref="WireNames"/> spelling, so columns never hold numbers.</summary>
public sealed class WireNameConverter<TEnum>() : ValueConverter<TEnum, string>(
    value => WireNames.Of(value),
    value => WireNames.Parse<TEnum>(value))
    where TEnum : struct, Enum;
