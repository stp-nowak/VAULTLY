using System.Reflection;

using NUnit.Framework;

using Vaultly.Core.Testing;

namespace Vaultly.Accounts.Application.Tests;

[TestFixture]
public sealed class CqrsConventionTests : CqrsConventions
{
    protected override Assembly ApplicationAssembly => typeof(DependencyInjection).Assembly;

    protected override string ApplicationSourceDirectory => SiblingProject("Vaultly.Accounts.Application");
}
