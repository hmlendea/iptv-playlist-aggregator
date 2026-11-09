using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace IptvPlaylistAggregator.ArchitectureTests
{
    /// <summary>
    /// Verifies handwritten source-file conventions using Roslyn syntax analysis.
    /// These checks run over the staged production and test sources and must not
    /// depend on the host machine's file-system layout.
    /// </summary>
    [TestFixture]
    public sealed class SourceFileConventionTests
    {
        private const int MaximumConditionalLineLength = 72;
        private const int MaximumSignatureLineLength = 96;
        private const int MaximumParameterCount = 7;
        private const int IndentationWidth = 4;

        private static readonly SyntaxTree[] ProductionTrees =
            GetSyntaxTrees(ArchitectureSourceRepository.ProductionFiles);

        [Test]
        public void GivenProductionAssembly_WhenEveryHandwrittenFileIsChecked_ThenItDeclaresExactlyOneType()
        {
            foreach (SyntaxTree tree in ProductionTrees)
            {
                string path = tree.FilePath!;
                int typeCount = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Count();

                Assert.That(typeCount, Is.EqualTo(1), $"The file '{Path.GetFileName(path)}' must declare exactly one type.");
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenEveryHandwrittenFileIsChecked_ThenTheFilenameStemMatchesTheTypeIdentifier()
        {
            foreach (SyntaxTree tree in ProductionTrees)
            {
                string path = tree.FilePath!;
                string stem = Path.GetFileNameWithoutExtension(path);

                BaseTypeDeclarationSyntax? declaration = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .FirstOrDefault();

                Assert.That(declaration, Is.Not.Null, $"The file '{stem}' declares no type.");

                BaseTypeDeclarationSyntax declaredType = declaration!;

                string actualTypeName = declaredType.Identifier.Text;

                Assert.That(stem, Is.EqualTo(actualTypeName),
                    $"The file '{stem}.cs' declares type '{actualTypeName}'; the filename stem must match the type identifier exactly.");
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenHandwrittenFilesAreChecked_ThenNoFileContainsTopLevelStatements()
        {
            foreach (SyntaxTree tree in ProductionTrees)
            {
                string path = tree.FilePath!;

                Assert.That(
                    tree.GetRoot()
                        .DescendantNodes()
                        .OfType<CompilationUnitSyntax>()
                        .Any(unit => unit.Members.OfType<GlobalStatementSyntax>().Any()),
                    Is.False,
                    $"The file '{Path.GetFileName(path)}' contains top-level statements; every file requires an explicit namespace and type block.");
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenEveryHandwrittenFileIsChecked_ThenTheNamespaceMatchesTheProjectAndFolderPath()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                SyntaxTree tree = CSharpSyntaxTree.ParseText(
                    File.ReadAllText(file.FullPath),
                    new CSharpParseOptions(LanguageVersion.Preview),
                    path: file.FullPath);

                NamespaceDeclarationSyntax? namespaceDeclaration = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault();

                Assert.That(
                    namespaceDeclaration,
                    Is.Not.Null,
                    $"The file '{file.RelativePath}' must declare an explicit block namespace.");

                string expectedNamespace = file.ExpectedNamespace;
                string namespaceName = namespaceDeclaration!.Name.ToString();

                Assert.That(
                    namespaceName,
                    Is.EqualTo(expectedNamespace),
                    $"The file '{file.RelativePath}' declares namespace '{namespaceName}' but its path implies '{expectedNamespace}'.");
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenEveryHandwrittenFileIsChecked_ThenItUsesBlockNamespacesAndNoFileScopedNamespaces()
        {
            foreach (SyntaxTree tree in ProductionTrees)
            {
                string path = tree.FilePath!;

                Assert.That(
                    tree.GetRoot().DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>(),
                    Is.Empty,
                    $"The file '{Path.GetFileName(path)}' uses a file-scoped namespace; block namespaces are required.");
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenEveryHandwrittenFileIsChecked_ThenItContainsNoForbiddenLanguageConstructs()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                string source = File.ReadAllText(file.FullPath);

                Assert.That(
                    source.Contains("#region", StringComparison.Ordinal) ||
                    source.Contains("#endregion", StringComparison.Ordinal),
                    Is.False,
                    $"The file '{file.RelativePath}' contains a region directive; regions are prohibited.");

                Assert.That(
                    source.Contains("#nullable enable", StringComparison.Ordinal),
                    Is.False,
                    $"The file '{file.RelativePath}' contains '#nullable enable'; nullable reference types are configured once in the project file.");
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenControlFlowBodiesAreChecked_ThenEveryBodyUsesBraces()
        {
            foreach (SyntaxTree tree in ProductionTrees)
            {
                string path = tree.FilePath!;

                foreach (IfStatementSyntax statement in tree.GetRoot().DescendantNodes().OfType<IfStatementSyntax>())
                {
                    Assert.That(
                        statement.Statement is BlockSyntax,
                        Is.True,
                        $"The file '{Path.GetFileName(path)}' contains an 'if' body without braces.");
                }

                foreach (ForEachStatementSyntax statement in tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>())
                {
                    Assert.That(
                        statement.Statement is BlockSyntax,
                        Is.True,
                        $"The file '{Path.GetFileName(path)}' contains a 'foreach' body without braces.");
                }

                foreach (ForStatementSyntax statement in tree.GetRoot().DescendantNodes().OfType<ForStatementSyntax>())
                {
                    Assert.That(
                        statement.Statement is BlockSyntax,
                        Is.True,
                        $"The file '{Path.GetFileName(path)}' contains a 'for' body without braces.");
                }

                foreach (WhileStatementSyntax statement in tree.GetRoot().DescendantNodes().OfType<WhileStatementSyntax>())
                {
                    Assert.That(
                        statement.Statement is BlockSyntax,
                        Is.True,
                        $"The file '{Path.GetFileName(path)}' contains a 'while' body without braces.");
                }
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenDeclarationsAreChecked_ThenNoVarOrTernaryOrTupleOrRefOrOptionalParameterIsUsed()
        {
            foreach (SyntaxTree tree in ProductionTrees)
            {
                string path = Path.GetFileName(tree.FilePath!);
                SyntaxNode root = tree.GetRoot();

                foreach (VariableDeclarationSyntax declaration in root.DescendantNodes().OfType<VariableDeclarationSyntax>())
                {
                    Assert.That(
                        declaration.Type.IsVar,
                        Is.False,
                        $"The file '{path}' declares a variable with 'var'; explicit types are required.");
                }

                Assert.That(
                    root.DescendantNodes().OfType<ConditionalExpressionSyntax>(),
                    Is.Empty,
                    $"The file '{path}' contains a ternary expression; use an 'if' or a switch expression instead.");

                Assert.That(
                    root.DescendantNodes().OfType<TupleExpressionSyntax>(),
                    Is.Empty,
                    $"The file '{path}' contains a tuple expression; tuples are prohibited.");

                Assert.That(
                    root.DescendantNodes().OfType<RefTypeSyntax>(),
                    Is.Empty,
                    $"The file '{path}' uses 'ref'; ref parameters and returns are prohibited.");

                foreach (ParameterSyntax parameter in root.DescendantNodes().OfType<ParameterSyntax>())
                {
                    Assert.That(
                        parameter.Default is null,
                        Is.True,
                        $"The file '{path}' declares an optional parameter; use overloads instead.");
                }
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenDeclarationsAreChecked_ThenNoParametersCollectionIsUsed()
        {
            foreach (SyntaxTree tree in ProductionTrees)
            {
                string path = Path.GetFileName(tree.FilePath!);

                foreach (ParameterSyntax parameter in tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>())
                {
                    bool isParams = parameter.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ParamsKeyword));

                    Assert.That(
                        isParams,
                        Is.False,
                        $"The file '{path}' declares a 'params' parameter; pass a collection explicitly instead.");
                }
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenMembersAreChecked_ThenEveryMethodAndConstructorHasAtMostSevenParameters()
        {
            foreach (SyntaxTree tree in ProductionTrees)
            {
                string path = Path.GetFileName(tree.FilePath!);
                SyntaxNode root = tree.GetRoot();

                foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    Assert.That(
                        method.ParameterList.Parameters.Count,
                        Is.LessThanOrEqualTo(MaximumParameterCount),
                        $"The method '{method.Identifier.Text}' in '{path}' declares {method.ParameterList.Parameters.Count} parameters; the maximum is {MaximumParameterCount}.");
                }

                foreach (ConstructorDeclarationSyntax constructor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
                {
                    Assert.That(
                        constructor.ParameterList.Parameters.Count,
                        Is.LessThanOrEqualTo(MaximumParameterCount),
                        $"A constructor in '{path}' declares {constructor.ParameterList.Parameters.Count} parameters; the maximum is {MaximumParameterCount}.");
                }

                foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (classDeclaration.ParameterList is null)
                    {
                        continue;
                    }

                    Assert.That(
                        classDeclaration.ParameterList.Parameters.Count,
                        Is.LessThanOrEqualTo(MaximumParameterCount),
                        $"The primary constructor of '{classDeclaration.Identifier.Text}' in '{path}' declares {classDeclaration.ParameterList.Parameters.Count} parameters; the maximum is {MaximumParameterCount}.");
                }
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenDeclarationsAreChecked_ThenNoTypeUsesTabsForIndentation()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                string[] lines = File.ReadAllLines(file.FullPath);

                foreach (string line in lines)
                {
                    Assert.That(
                        line.StartsWith('\t'),
                        Is.False,
                        $"The file '{file.RelativePath}' uses a tab for indentation; {IndentationWidth} spaces are required.");
                }
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenDeclarationsAreChecked_ThenNoFileContainsConsecutiveBlankLines()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                string[] lines = File.ReadAllLines(file.FullPath);

                for (int index = 1; index < lines.Length; index++)
                {
                    bool bothBlank = string.IsNullOrWhiteSpace(lines[index]) && string.IsNullOrWhiteSpace(lines[index - 1]);

                    Assert.That(
                        bothBlank,
                        Is.False,
                        $"The file '{file.RelativePath}' contains consecutive blank lines near line {index + 1}.");
                }
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenCommentsAreChecked_ThenEveryCommentStartsWithAUppercaseLetterAndEndsWithAPeriod()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                string[] lines = File.ReadAllLines(file.FullPath);

                for (int index = 0; index < lines.Length; index++)
                {
                    string trimmedLine = lines[index].TrimStart();

                    if (!trimmedLine.StartsWith("//", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string commentText = trimmedLine[2..].Trim();

                    if (commentText.Length == 0 || commentText.StartsWith('<') || commentText.StartsWith('/'))
                    {
                        continue;
                    }

                    Assert.That(
                        char.IsUpper(commentText[0]),
                        Is.True,
                        $"The comment in '{file.RelativePath}' near line {index + 1} must start with an uppercase letter.");

                    Assert.That(
                        commentText.EndsWith('.'),
                        Is.True,
                        $"The comment in '{file.RelativePath}' near line {index + 1} must end with a period.");
                }
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenFilesAreChecked_ThenEachEndsWithExactlyOneBlankLine()
        {
            foreach (ArchitectureSourceFile file in ArchitectureSourceRepository.ProductionFiles)
            {
                string source = File.ReadAllText(file.FullPath);

                Assert.That(
                    source.EndsWith('\n'),
                    Is.True,
                    $"The file '{file.RelativePath}' must end with a line feed.");

                Assert.That(
                    source.EndsWith("\n\n", StringComparison.Ordinal),
                    Is.False,
                    $"The file '{file.RelativePath}' must end with exactly one line feed.");
            }
        }

        [Test]
        public void GivenProductionAssembly_WhenMemberOrderingIsChecked_ThenNestedTypeCountIsZero()
        {
            foreach (SyntaxTree tree in ProductionTrees)
            {
                string path = Path.GetFileName(tree.FilePath!);

                foreach (BaseTypeDeclarationSyntax declaration in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    Assert.That(
                        declaration.DescendantNodes().OfType<BaseTypeDeclarationSyntax>(),
                        Is.Empty,
                        $"The file '{path}' declares a nested type; extract it into its own file.");
                }
            }
        }

        private static SyntaxTree[] GetSyntaxTrees(IEnumerable<ArchitectureSourceFile> files)
        {
            const LanguageVersion languageVersion = LanguageVersion.Preview;
            CSharpParseOptions parseOptions = new(languageVersion);

            return [.. files
                .Select(
                    file =>
                    {
                        string source = File.ReadAllText(file.FullPath);
                        return CSharpSyntaxTree.ParseText(source, parseOptions, path: file.FullPath);
                    })];
        }
    }
}
