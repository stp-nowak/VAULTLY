using System.Reflection;
using System.Runtime.CompilerServices;

using MediatR;

using NUnit.Framework;

using Vaultly.Core.Messaging;

namespace Vaultly.Core.Testing;

/// <summary>
/// Guards the CQRS conventions of a service's application layer so they do not erode over time. A service's
/// application tests derive a <c>[TestFixture]</c> from it and name their application project.
/// </summary>
public abstract class CqrsConventions
{
    /// <summary>The service's <c>Application</c> assembly.</summary>
    protected abstract Assembly ApplicationAssembly { get; }

    /// <summary>Folder of the <c>Application</c> project's sources; see <see cref="SiblingProject"/>.</summary>
    protected abstract string ApplicationSourceDirectory { get; }

    /// <summary>A project next to the calling test file's project, e.g. <c>Vaultly.Accounts.Application</c>.</summary>
    protected static string SiblingProject(string projectName, [CallerFilePath] string testFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "..", projectName));

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
    public void CommandsAndQueries_LiveInAFeatureUseCase_AndAreNamedAfterIt()
    {
        var misplaced = ConcreteTypes()
            .Where(type => IsCommand(type) || IsQuery(type))
            .Where(type => !InFeatureUseCase(type) || type.Name != UseCaseName(type) + (IsCommand(type) ? "Command" : "Query"))
            .Select(type => type.FullName)
            .ToList();

        Assert.That(misplaced, Is.Empty, "Use Features/<Area>/<UseCase>/ with a <UseCase>Command or <UseCase>Query.");
    }

    [Test]
    public void UseCaseFolders_HoldAHandlerAndEitherACommandOrAQuery()
    {
        var featuresDirectory = Path.Combine(ApplicationSourceDirectory, "Features");
        Assert.That(Directory.Exists(featuresDirectory), Is.True, $"No Features folder in {ApplicationSourceDirectory}.");

        var violations = Directory.GetDirectories(featuresDirectory)
            .SelectMany(Directory.GetDirectories)
            .Where(useCase =>
            {
                var hasCommand = File.Exists(Path.Combine(useCase, "Command.cs"));
                var hasQuery = File.Exists(Path.Combine(useCase, "Query.cs"));
                return hasCommand == hasQuery || !File.Exists(Path.Combine(useCase, "Handler.cs"));
            })
            .Select(useCase => Path.GetRelativePath(featuresDirectory, useCase))
            .ToList();

        Assert.That(violations, Is.Empty, "Each use case folder holds Handler.cs and exactly one of Command.cs and Query.cs.");
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

        Assert.That(violations, Is.Empty, "Query handlers read through read services, never aggregate repositories.");
    }

    private IEnumerable<Type> ConcreteTypes()
        => ApplicationAssembly.GetTypes().Where(type => type is { IsAbstract: false, IsInterface: false });

    private static bool IsCommand(Type type)
        => typeof(ICommand).IsAssignableFrom(type) || ImplementsOpenGeneric(type, typeof(ICommand<>));

    private static bool IsQuery(Type type) => ImplementsOpenGeneric(type, typeof(IQuery<>));

    private static bool InFeatureUseCase(Type type)
        => type.Namespace?.Split('.') is [.., "Features", _, _];

    private static string UseCaseName(Type type) => type.Namespace!.Split('.')[^1];

    private static bool IsWriteSideDependency(Type type)
        => type == typeof(IUnitOfWork) || type is { IsInterface: true, Name: var name } && name.EndsWith("Repository", StringComparison.Ordinal);

    private static bool ImplementsOpenGeneric(Type type, Type openGeneric)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);
}
