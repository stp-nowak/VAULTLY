using Vaultly.Core.Messaging;

namespace Vaultly.Identity.Application.Features.Users.EnsureBootstrapAdmin;

/// <summary>
/// Run at startup: while there is no admin, makes the configured bootstrap user one (see
/// <see cref="Options.BootstrapAdminOptions"/>).
/// </summary>
public sealed record EnsureBootstrapAdminCommand : ICommand;
