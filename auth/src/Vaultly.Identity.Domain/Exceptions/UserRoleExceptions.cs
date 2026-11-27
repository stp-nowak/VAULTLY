using Vaultly.Core;

namespace Vaultly.Identity.Domain.Exceptions;

/// <summary>
/// Thrown when a change would leave the Identity service without any admin, so nobody could manage it any more.
/// </summary>
public sealed class LastAdminException() : DomainException("At least one admin must remain.");
