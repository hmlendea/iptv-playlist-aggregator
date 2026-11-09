using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace IptvPlaylistAggregator.ArchitectureTests
{
    /// <summary>
    /// Verifies the project and solution conventions declared by the checked
    /// <c>.csproj</c> files and the solution file. These checks read the staged
    /// project metadata and never depend on the host file-system layout.
    /// </summary>
    [TestFixture]
    public sealed class ProjectFileConventionTests
    {
        private const string NullableEnabledValue = "enable";
        private const string ImplicitUsingsDisabledValue = "disable";

        private static IReadOnlyList<XElement> ProjectPropertyGroupValues(string projectName, string propertyName)
        {
            ArchitectureProjectFile project = ArchitectureSourceRepository.Projects
                .SingleOrDefault(candidate => string.Equals(candidate.Name, projectName, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"The project '{projectName}' was not staged for checking.");

            XDocument document = XDocument.Load(project.FullPath);

            return [.. document
                .Descendants("PropertyGroup")
                .SelectMany(group => group.Elements(propertyName))];
        }

        private static IReadOnlyList<string> PackageReferences(string projectName)
        {
            ArchitectureProjectFile project = ArchitectureSourceRepository.Projects
                .SingleOrDefault(candidate => string.Equals(candidate.Name, projectName, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"The project '{projectName}' was not staged for checking.");

            return [.. XDocument.Load(project.FullPath)
                .Descendants("PackageReference")
                .Select(reference => reference.Attribute("Include")?.Value ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))];
        }

        [Test]
        public void GivenEveryProject_WhenItsTargetFrameworkIsChecked_ThenNet10OrNewerIsRequired()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects)
            {
                IReadOnlyList<XElement> targetFrameworks = ProjectPropertyGroupValues(project.Name, "TargetFramework");

                Assert.That(targetFrameworks, Is.Not.Empty, $"The project '{project.Name}' declares no TargetFramework.");

                foreach (XElement targetFramework in targetFrameworks)
                {
                    Assert.That(
                        targetFramework.Value.Trim(),
                        Is.EqualTo("net10.0"),
                        $"The project '{project.Name}' targets '{targetFramework.Value}'; every project must target net10.0 or newer.");
                }
            }
        }

        [Test]
        public void GivenEveryProject_WhenItsCompilerSettingsAreChecked_ThenTheMandatoryPropertiesArePresent()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects)
            {
                Assert.That(
                    ProjectPropertyGroupValues(project.Name, "ImplicitUsings").Single().Value.Trim(),
                    Is.EqualTo(ImplicitUsingsDisabledValue),
                    $"The project '{project.Name}' must disable implicit usings.");

                Assert.That(
                    ProjectPropertyGroupValues(project.Name, "LangVersion").Single().Value.Trim(),
                    Is.EqualTo("latest"),
                    $"The project '{project.Name}' must set LangVersion to latest.");

                Assert.That(
                    ProjectPropertyGroupValues(project.Name, "Nullable").Single().Value.Trim(),
                    Is.EqualTo(NullableEnabledValue),
                    $"The project '{project.Name}' must enable nullable reference types.");
            }
        }

        [Test]
        public void GivenEveryProject_WhenItsPackageReferencesAreChecked_ThenTheyAreAlphabeticallyOrdered()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects)
            {
                IReadOnlyList<string> packageNames = PackageReferences(project.Name);

                Assert.That(
                    packageNames,
                    Is.Ordered.Using<string>(StringComparer.Ordinal),
                    $"The package references of '{project.Name}' must be ordered alphabetically by their Include attribute.");
            }
        }

        [Test]
        public void GivenEveryTestProject_WhenItsTestFlagsAreChecked_ThenTheyAreSetExplicitly()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects)
            {
                if (!project.Name.Contains("Tests", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.That(
                    ProjectPropertyGroupValues(project.Name, "IsTestProject").Single().Value.Trim(),
                    Is.EqualTo("true"),
                    $"The test project '{project.Name}' must set IsTestProject to true.");

                Assert.That(
                    ProjectPropertyGroupValues(project.Name, "ExcludeFromCodeCoverage").Single().Value.Trim(),
                    Is.EqualTo("true"),
                    $"The test project '{project.Name}' must set ExcludeFromCodeCoverage to true.");
            }
        }

        [Test]
        public void GivenTheSolution_WhenItsStructureIsChecked_ThenItIsSdkStyleWithoutDeclarationOrRootAttributes()
        {
            string content = File.ReadAllText(ArchitectureSourceRepository.Solution.FullPath);

            Assert.That(
                content.StartsWith("<Solution>", StringComparison.Ordinal),
                Is.True,
                "The solution file must start with a '<Solution>' element with no attributes.");

            Assert.That(
                content.TrimEnd().EndsWith("</Solution>", StringComparison.Ordinal),
                Is.True,
                "The solution file must end with a '</Solution>' element.");

            Assert.That(
                content.Contains("<?xml", StringComparison.Ordinal),
                Is.False,
                "The solution file must not contain an XML declaration.");

            Assert.That(
                content.Contains("<Version", StringComparison.Ordinal),
                Is.False,
                "The solution file must not contain a Version element.");

            XDocument document = XDocument.Load(ArchitectureSourceRepository.Solution.FullPath);

            foreach (XElement projectElement in document.Descendants("Project"))
            {
                Assert.That(
                    projectElement.Attributes().Select(attribute => attribute.Name.LocalName),
                    Is.EqualTo(["Path"]),
                    "Every solution Project element must declare only the Path attribute.");
            }
        }

        [Test]
        public void GivenTheSolution_WhenItsProjectOrderIsChecked_ThenTheMainProjectPrecedesTheTestProjects()
        {
            IReadOnlyList<string> declaredProjectPaths = ReadDeclaredProjectPaths(ArchitectureSourceRepository.Solution);

            Assert.That(declaredProjectPaths, Is.Not.Empty, "The solution must declare at least one project.");
            Assert.That(
                declaredProjectPaths[0],
                Is.EqualTo("IptvPlaylistAggregator/IptvPlaylistAggregator.csproj"),
                "The main project must be declared first in the solution.");

            IReadOnlyList<string> testProjects = [.. declaredProjectPaths.Where(path => path.Contains(".Tests/", StringComparison.Ordinal))];
            Assert.That(
                testProjects,
                Is.Ordered.Using<string>(StringComparer.Ordinal),
                "The test projects must be listed alphabetically after the main project.");
        }

        [Test]
        public void GivenEveryProject_WhenItsLocationIsChecked_ThenItResidesInASiblingProjectFolder()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects)
            {
                if (string.Equals(project.Name, ArchitectureSourceRepository.Solution.ProjectName, StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.That(
                    ArchitectureSourceRepository.Solution.HostsProjectInSiblingFolder(project.Name),
                    Is.True,
                    $"The project '{project.Name}' must reside in a sibling folder named after the project, without a 'src' nesting layer.");
            }
        }

        private static IReadOnlyList<string> ReadDeclaredProjectPaths(ArchitectureSolutionFile solution)
        {
            XDocument document = XDocument.Load(solution.FullPath);

            return [.. document
                .Descendants("Project")
                .Select(element => element.Attribute("Path")?.Value ?? string.Empty)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Replace('\\', '/'))];
        }
    }
}
