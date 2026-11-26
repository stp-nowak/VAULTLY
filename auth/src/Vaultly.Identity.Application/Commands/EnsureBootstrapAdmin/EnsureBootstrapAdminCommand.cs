using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Commands.EnsureBootstrapAdmin;

/// <summary>
/// Run at startup: while there is no admin, makes the configured bootstrap user one (see
/// <see cref="Options.BootstrapAdminOptions"/>).
/// </summary>
public sealed record EnsureBootstrapAdminCommand : ICommand;
