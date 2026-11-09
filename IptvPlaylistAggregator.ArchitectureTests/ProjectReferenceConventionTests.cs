using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace IptvPlaylistAggregator.ArchitectureTests
{
    /// <summary>
    /// Verifies project-level dependency direction independently from compiled type dependencies.
    /// </summary>
    [TestFixture]
    public sealed class ProjectReferenceConventionTests
    {
        [Test]
        public void GivenTestProjects_WhenReferencesAreChecked_ThenTheyReferenceOnlyProductionAndPermittedTestPackages()
        {
            foreach (ArchitectureProjectFile project in ArchitectureSourceRepository.Projects
                .Where(project => project.Name.Contains("Tests", StringComparison.Ordinal)))
            {
                IReadOnlyList<string> projectReferences = ReadProjectReferences(project);

                Assert.That(
                    projectReferences,
                    Does.Contain("IptvPlaylistAggregator/IptvPlaylistAggregator.csproj")
                        .Or.Contain("../IptvPlaylistAggregator/IptvPlaylistAggregator.csproj")
                        .Or.Contain("..\\IptvPlaylistAggregator\\IptvPlaylistAggregator.csproj"),
                    $"Test project '{project.Name}' must reference the production project.");

                foreach (string reference in projectReferences)
                {
                    Assert.That(
                        reference.Contains("ArchitectureTests", StringComparison.Ordinal),
                        Is.False,
                        $"Test project '{project.Name}' must not reference the architecture test project.");
                }
            }
        }

        [Test]
        public void GivenProductionProject_WhenReferencesAreChecked_ThenItHasNoTestProjectReference()
        {
            ArchitectureProjectFile productionProject = ArchitectureSourceRepository.Projects
                .Single(project => string.Equals(project.Name, "IptvPlaylistAggregator", StringComparison.Ordinal));

            IReadOnlyList<string> references = ReadProjectReferences(productionProject);

            Assert.That(
                references.Any(reference => reference.Contains("Tests", StringComparison.Ordinal)),
                Is.False,
                "The production project must not reference a test project.");
        }

        [Test]
        public void GivenProductionProject_WhenInternalsVisibleToIsChecked_ThenItIsNotDeclared()
        {
            foreach (ArchitectureSourceFile sourceFile in ArchitectureSourceRepository.ProductionFiles)
            {
                string source = File.ReadAllText(sourceFile.FullPath);

                Assert.That(
                    source.Contains("InternalsVisibleTo", StringComparison.Ordinal),
                    Is.False,
                    $"Production source '{sourceFile.RelativePath}' must not declare InternalsVisibleTo.");
            }
        }

        [Test]
        public void GivenArchitectureProject_WhenRequiredPackagesAreChecked_ThenNUnitArchUnitAndRoslynAreDeclared()
        {
            ArchitectureProjectFile project = ArchitectureSourceRepository.Projects
                .Single(candidate => string.Equals(candidate.Name, "IptvPlaylistAggregator.ArchitectureTests", StringComparison.Ordinal));

            IReadOnlyList<string> packages = ReadPackageReferences(project);

            Assert.Multiple(
                () =>
                {
                    Assert.That(packages, Does.Contain("NUnit"));
                    Assert.That(packages, Does.Contain("TngTech.ArchUnitNET"));
                    Assert.That(packages, Does.Contain("TngTech.ArchUnitNET.NUnit"));
                    Assert.That(packages, Does.Contain("Microsoft.CodeAnalysis.CSharp"));
                });
        }

        private static IReadOnlyList<string> ReadProjectReferences(ArchitectureProjectFile project)
            => [.. XDocument
                .Load(project.FullPath)
                .Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .Where(reference => !string.IsNullOrWhiteSpace(reference))];

        private static IReadOnlyList<string> ReadPackageReferences(ArchitectureProjectFile project)
            => [.. XDocument
                .Load(project.FullPath)
                .Descendants("PackageReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .Where(reference => !string.IsNullOrWhiteSpace(reference))];
    }
}
