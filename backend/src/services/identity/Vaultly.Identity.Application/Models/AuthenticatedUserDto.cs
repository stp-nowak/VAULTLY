namespace Vaultly.Identity.Application.Models;

public sealed record AuthenticatedUserDto(Guid Id, string Email, string Name);