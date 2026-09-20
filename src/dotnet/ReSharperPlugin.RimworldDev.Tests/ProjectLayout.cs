using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

namespace ReSharperPlugin.RimworldDev.Tests;

/// <summary>How the solution under test is laid out. It changes what the plugin sees, so most suites want both.</summary>
public enum ProjectLayout
{
    /// <summary>Everything in one C# project, which is what the SDK's test bases build by default.</summary>
    CSharpProject,

    /// <summary>
    /// What a mod looks like: the Defs in a project of their own with no references, RimWorld referenced by a C#
    /// project beside it. Some values are validated here and not in a C# project; see boundary 8 in
    /// docs/testing-plan.md.
    /// </summary>
    XmlProject
}

/// <summary>A fixture built for one layout. The layout-aware test bases implement it; tests don't need to.</summary>
public interface IProjectLayoutFixture
{
    ProjectLayout Layout { get; }
}

/// <summary>
/// On a fixture, builds one of it per layout listed, so every test in the class runs once in each:
///
///     [ProjectLayouts(ProjectLayout.XmlProject, ProjectLayout.CSharpProject)]
///
/// On a test method, narrows that one test to the layouts listed and skips it in the others.
///
/// It also resets ScopeHelper around every test it applies to, which is why the bases don't need a SetUp for it.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ProjectLayoutsAttribute(params ProjectLayout[] layouts) : NUnitAttribute, IFixtureBuilder2, ITestAction
{
    public IReadOnlyList<ProjectLayout> Layouts { get; } = layouts;

    public IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo) => BuildFrom(typeInfo, MatchEverything.Instance);

    public IEnumerable<TestSuite> BuildFrom(ITypeInfo typeInfo, IPreFilter filter) =>
        layouts.Select(layout =>
            new NUnitTestFixtureBuilder().BuildFrom(typeInfo, filter, new TestFixtureParameters(layout)));

    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test)
    {
        if (test.Fixture is IProjectLayoutFixture fixture && !Layouts.Contains(fixture.Layout))
            Assert.Ignore($"Only runs in {string.Join(", ", Layouts)}");

        // ScopeHelper caches the RimWorld scope in statics; without this the next test reuses a scope from a solution
        // that no longer exists. The discovery switch stops it from finding a real RimWorld install on this machine.
        ScopeHelper.Reset();
        ScopeHelper.SkipAssemblyDiscovery = true;
    }

    public void AfterTest(ITest test) => ScopeHelper.Reset();

    /// <summary>NUnit's own empty filter is internal, and this is all of it that gets used.</summary>
    private class MatchEverything : IPreFilter
    {
        public static readonly IPreFilter Instance = new MatchEverything();

        public bool IsMatch(Type type) => true;
        public bool IsMatch(Type type, MethodInfo method) => true;
    }
}

/// <summary>
/// Everything <see cref="ProjectLayout.XmlProject"/> takes, so that a test base wiring it up is five one-line
/// overrides and nothing else. A base for an SDK test class that doesn't have one yet is a copy of those five.
/// </summary>
public static class ProjectLayoutSupport
{
    private const string XmlProjectName = "RimworldXmlProject";
    private const string CSharpProjectName = "ModAssembly";
    private const string CSharpProjectFile = "ModAssembly.cs";

    /// <summary>What the two projects are called, given what the SDK named them. Call it from the base's constructor.</summary>
    public static (string ProjectName, string SecondProjectName) ProjectNames(
        ProjectLayout layout, string projectName, string secondProjectName) =>
        layout == ProjectLayout.XmlProject
            ? (XmlProjectName, CSharpProjectName)
            : (projectName, secondProjectName);

    /// <summary>A solution built by a fixture in the other layout has the wrong projects in it to reuse.</summary>
    public static bool CanReuse(bool baseResult, ISolution solution, string projectName) =>
        baseResult && solution.GetAllProjects().Any(project => project.Name == projectName);

    /// <summary>The game's assemblies, unless the fixture is one that checks life without them.</summary>
    public static IEnumerable<string> ReferencedAssemblies(IEnumerable<string> baseAssemblies, bool referenceRimworld) =>
        referenceRimworld ? baseAssemblies.Concat(RimworldReferenceAssemblies()) : baseAssemblies;

    /// <summary>
    /// Every DLL from the Krafs package's ref/net472 folder except the framework ones, which the test platform
    /// already provides. The folder path is baked into this assembly by the csproj from NuGet's restore.
    /// </summary>
    private static IEnumerable<string> RimworldReferenceAssemblies()
    {
        var refDir = typeof(ProjectLayoutSupport).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RimworldRefDir").Value;

        return Directory.GetFiles(refDir, "*.dll")
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                return !name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) &&
                       !name.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                       !name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) &&
                       !name.StartsWith("Mono.", StringComparison.OrdinalIgnoreCase);
            });
    }

    /// <summary>Only the XML project loses its references; that is what makes it behave like a mod's Defs folder.</summary>
    public static ICollection<KeyValuePair<TargetFrameworkId, IEnumerable<string>>> Libraries(
        ProjectLayout layout, string projectName, string mainProjectName,
        ICollection<KeyValuePair<TargetFrameworkId, IEnumerable<string>>> libraries) =>
        layout == ProjectLayout.XmlProject && projectName == mainProjectName
            ? libraries
                .Select(pair =>
                    new KeyValuePair<TargetFrameworkId, IEnumerable<string>>(pair.Key, EmptyList<string>.Instance))
                .ToList()
            : libraries;

    /// <summary>
    /// Builds the solution the way the layout wants it: one project, or the Defs in the XML project and the .cs files
    /// in the mod's assembly beside it. The C# project holds test/data/ModAssembly.cs, shared by every suite.
    /// </summary>
    public static void BuildSolution(ProjectLayout layout, string testFile, IEnumerable<string> otherFiles,
        string relativeTestDataPath, Action<string[]> singleProject, Action<string[], string[]> twoProjects)
    {
        var others = otherFiles.AsList();

        if (layout != ProjectLayout.XmlProject)
        {
            singleProject(new[] { testFile }.Concat(others).ToArray());
            return;
        }

        var depth = relativeTestDataPath.Split('/', '\\').Count(part => part.Length > 0);
        var sharedFile = string.Concat(Enumerable.Repeat(@"..\", depth)) + CSharpProjectFile;

        twoProjects(
            new[] { testFile }.Concat(others.Where(file => !file.EndsWith(".cs"))).ToArray(),
            others.Where(file => file.EndsWith(".cs")).Concat(new[] { sharedFile }).ToArray());
    }
}
