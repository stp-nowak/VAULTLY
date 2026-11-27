using System.Reflection;

using MediatR;

using NUnit.Framework;

using Vaultly.Core;
using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Tests;

/// <summary>
/// Guards the CQRS conventions of the application layer so they do not erode over time.
/// </summary>
[TestFixture]
public sealed class CqrsConventionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;

    [Test]
    public void Requests_AreDeclaredAsCommandsOrQueries()
    {
        var plainRequests = ConcreteTypes()
            .Where(type => ImplementsOpenGeneric(type, typeof(IRequest<>)) || typeof(IRequest).IsAssignableFrom(type))
            .Where(type => !IsCommand(type) && !IsQuery(type))
            .Select(type => type.FullName)
            .ToList();

        Assert.That(plainRequests, Is.Empty, "Every MediatR request must implement ICommand or IQuery.");
    }

    [Test]
    public void Commands_LiveInCommandsNamespace_AndQueries_LiveInQueriesNamespace()
    {
        var misplaced = ConcreteTypes()
            .Where(type => (IsCommand(type) && !InNamespace(type, "Commands")) || (IsQuery(type) && !InNamespace(type, "Queries")))
            .Select(type => type.FullName)
            .ToList();

        Assert.That(misplaced, Is.Empty);
    }

    [Test]
    public void QueryHandlers_DoNotDependOnRepositoriesOrUnitOfWork()
    {
        var violations = ConcreteTypes()
            .Where(type => ImplementsOpenGeneric(type, typeof(IQueryHandler<,>)))
            .SelectMany(type => type.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters())
                .Where(parameter => IsWriteSideDependency(parameter.ParameterType))
                .Select(parameter => $"{type.Name} -> {parameter.ParameterType.Name}"))
            .ToList();

        Assert.That(violations, Is.Empty, "Query handlers must read through read services, not aggregates.");
    }

    private static IEnumerable<Type> ConcreteTypes()
        => ApplicationAssembly.GetTypes().Where(type => type is { IsAbstract: false, IsInterface: false });

    private static bool IsCommand(Type type)
        => typeof(ICommand).IsAssignableFrom(type) || ImplementsOpenGeneric(type, typeof(ICommand<>));

    private static bool IsQuery(Type type) => ImplementsOpenGeneric(type, typeof(IQuery<>));

    private static bool InNamespace(Type type, string segment)
        => type.Namespace?.Contains($".{segment}.", StringComparison.Ordinal) == true;

    private static bool IsWriteSideDependency(Type type)
        => type == typeof(IUnitOfWork)
           || type.Namespace == "Vaultly.Identity.Application.Interfaces.Repositories";

    private static bool ImplementsOpenGeneric(Type type, Type openGeneric)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
}
