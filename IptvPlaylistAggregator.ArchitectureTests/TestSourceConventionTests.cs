using System;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace IptvPlaylistAggregator.ArchitectureTests
{
    /// <summary>
    /// Verifies the structural conventions of the unit and integration test sources.
    /// </summary>
    [TestFixture]
    public sealed class TestSourceConventionTests
    {
        [Test]
        public void GivenTestSources_WhenEveryFileIsChecked_ThenItDeclaresExactlyOneTopLevelType()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.TestFiles)
            {
                SyntaxTree syntaxTree = Parse(file);
                CompilationUnitSyntax root = (CompilationUnitSyntax)syntaxTree.GetRoot();
                int topLevelTypeCount = root
                    .Members
                    .SelectMany(member => member.DescendantNodesAndSelf())
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Count();

                Assert.That(
                    topLevelTypeCount,
                    Is.EqualTo(1),
                    $"Test source '{file.RelativePath}' must declare exactly one top-level type.");
            }
        }

        [Test]
        public void GivenTestSources_WhenEveryFileIsChecked_ThenTheFilenameStemMatchesTheTypeIdentifier()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.TestFiles)
            {
                CompilationUnitSyntax root = (CompilationUnitSyntax)Parse(file).GetRoot();
                BaseTypeDeclarationSyntax declaration = root
                    .Members
                    .SelectMany(member => member.DescendantNodesAndSelf())
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Single();

                Assert.That(
                    Path.GetFileNameWithoutExtension(file.FullPath),
                    Is.EqualTo(declaration.Identifier.Text),
                    $"Test source '{file.RelativePath}' must use the declared type identifier as its filename stem.");
            }
        }

        [Test]
        public void GivenTestSources_WhenNamespacesAreChecked_ThenTheyUseBlockNamespacesMatchingTheFolderPath()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.TestFiles)
            {
                SyntaxTree syntaxTree = Parse(file);
                NamespaceDeclarationSyntax? namespaceDeclaration = syntaxTree.GetRoot()
                    .DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault();

                Assert.That(namespaceDeclaration, Is.Not.Null, $"Test source '{file.RelativePath}' must declare a block namespace.");
                Assert.That(
                    namespaceDeclaration!.Name.ToString(),
                    Is.EqualTo(file.ExpectedNamespace),
                    $"Test source '{file.RelativePath}' namespace must match its folder path.");
                Assert.That(
                    syntaxTree.GetRoot().DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>(),
                    Is.Empty,
                    $"Test source '{file.RelativePath}' must not use a file-scoped namespace.");
            }
        }

        [Test]
        public void GivenTestSources_WhenDeclarationsAreChecked_ThenNoNestedTypesOrTopLevelStatementsExist()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.TestFiles)
            {
                SyntaxNode root = Parse(file).GetRoot();

                Assert.That(
                    root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Any(
                        declaration => declaration.Parent is TypeDeclarationSyntax),
                    Is.False,
                    $"Test source '{file.RelativePath}' must not declare nested types.");

                Assert.That(
                    root.DescendantNodes().OfType<GlobalStatementSyntax>(),
                    Is.Empty,
                    $"Test source '{file.RelativePath}' must not contain top-level statements.");
            }
        }

        [Test]
        public void GivenTestFixtures_WhenChecked_ThenEveryFixtureUsesNUnitAndContainsTests()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.TestFiles)
            {
                SyntaxNode root = Parse(file).GetRoot();

                foreach (ClassDeclarationSyntax fixture in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    bool hasFixtureAttribute = fixture.AttributeLists
                        .SelectMany(attributeList => attributeList.Attributes)
                        .Any(attribute => string.Equals(attribute.Name.ToString(), "TestFixture", StringComparison.Ordinal));

                    Assert.That(hasFixtureAttribute, Is.True, $"Test class '{fixture.Identifier.Text}' in '{file.RelativePath}' must use NUnit TestFixture.");

                    bool hasTestMethod = fixture.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Any(method => method.AttributeLists
                            .SelectMany(attributeList => attributeList.Attributes)
                            .Any(attribute =>
                                string.Equals(attribute.Name.ToString(), "Test", StringComparison.Ordinal) ||
                                string.Equals(attribute.Name.ToString(), "TestCase", StringComparison.Ordinal)));

                    Assert.That(hasTestMethod, Is.True, $"Test fixture '{fixture.Identifier.Text}' in '{file.RelativePath}' must contain a Test or TestCase method.");
                }
            }
        }

        [Test]
        public void GivenTestSources_WhenFrameworkAttributesAreChecked_ThenNoXunitOrMSTestUsageExists()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.TestFiles)
            {
                string source = File.ReadAllText(file.FullPath);

                Assert.That(source, Does.Not.Contain("Xunit"), $"Test source '{file.RelativePath}' must use NUnit, not xUnit.");
                Assert.That(source, Does.Not.Contain("Microsoft.VisualStudio.TestTools"), $"Test source '{file.RelativePath}' must use NUnit, not MSTest.");
            }
        }

        private static SyntaxTree Parse(ArchitectureSourceFile file)
            => CSharpSyntaxTree.ParseText(
                File.ReadAllText(file.FullPath),
                new CSharpParseOptions(LanguageVersion.Latest),
                path: file.FullPath);
    }
}
