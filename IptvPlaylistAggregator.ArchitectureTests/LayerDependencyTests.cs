using System;
using System.Collections.Generic;
using System.Linq;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using ProductionAssembly = System.Reflection.Assembly;

namespace IptvPlaylistAggregator.ArchitectureTests
{
    /// <summary>
    /// Verifies the layer dependency matrix documented in <c>ARCHITECTURE.md</c>
    /// against the compiled production assembly, using ArchUnit.NET.
    /// </summary>
    [TestFixture]
    public sealed class LayerDependencyTests
    {
        private const string CompositionNamespace = "IptvPlaylistAggregator";
        private const string ConfigurationNamespace = "IptvPlaylistAggregator.Configuration";
        private const string DomainNamespace = "IptvPlaylistAggregator.Service.Models";
        private const string ApplicationNamespace = "IptvPlaylistAggregator.Service";
        private const string MappingNamespace = "IptvPlaylistAggregator.Service.Mapping";
        private const string PersistenceNamespace = "IptvPlaylistAggregator.DataAccess.DataObjects";
        private const string LoggingNamespace = "IptvPlaylistAggregator.Logging";

        private const string AspNetCoreNamespacePattern = "^Microsoft\\.AspNetCore.*";
        private const string EntityFrameworkNamespace = "Microsoft.EntityFrameworkCore";
        private const string DynamoDbNamespace = "Amazon.DynamoDBv2";
        private const string DapperNamespace = "Dapper";
        private const string MediatRNamespace = "MediatR";

        private static readonly Architecture Architecture = LoadProductionArchitecture();

        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenItContainsEveryDocumentedLayer()
        {
            IReadOnlyList<string> namespaces = Architecture.Namespaces
                .Select(namespaceDefinition => namespaceDefinition.FullName)
                .OrderBy(namespaceName => namespaceName, StringComparer.Ordinal)
                .ToList();

            Assert.Multiple(
                () =>
                {
                    Assert.That(namespaces, Does.Contain(DomainNamespace));
                    Assert.That(namespaces, Does.Contain(ApplicationNamespace));
                    Assert.That(namespaces, Does.Contain(MappingNamespace));
                    Assert.That(namespaces, Does.Contain(PersistenceNamespace));
                    Assert.That(namespaces, Does.Contain(ConfigurationNamespace));
                    Assert.That(namespaces, Does.Contain(LoggingNamespace));
                });
        }

        [Test]
        public void GivenTheDocumentedLayers_WhenSelected_ThenEachLayerContainsAtLeastOneType()
        {
            Assert.Multiple(
                () =>
                {
                    Assert.That(Types().That().ResideInNamespace(DomainNamespace).GetObjects(Architecture), Is.Not.Empty);
                    Assert.That(Types().That().ResideInNamespace(ApplicationNamespace).GetObjects(Architecture), Is.Not.Empty);
                    Assert.That(Types().That().ResideInNamespace(MappingNamespace).GetObjects(Architecture), Is.Not.Empty);
                    Assert.That(Types().That().ResideInNamespace(PersistenceNamespace).GetObjects(Architecture), Is.Not.Empty);
                });
        }

        [Test]
        public void GivenTheDomainLayer_WhenItDependsOnOtherLayers_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespace(DomainNamespace)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace(ApplicationNamespace)
                        .Or().ResideInNamespace(MappingNamespace)
                        .Or().ResideInNamespace(PersistenceNamespace)
                        .Or().ResideInNamespace(ConfigurationNamespace)
                        .Or().ResideInNamespace(LoggingNamespace))
                .Because("the domain layer must not depend on the application, mapping, persistence, configuration, or logging layers");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheDomainLayer_WhenItUsesInfrastructureLibraries_ThenNoInfrastructureDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespace(DomainNamespace)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(AspNetCoreNamespacePattern)
                        .Or().ResideInNamespace(EntityFrameworkNamespace)
                        .Or().ResideInNamespace(DynamoDbNamespace)
                        .Or().ResideInNamespace(DapperNamespace)
                        .Or().ResideInNamespace(MediatRNamespace))
                .Because("the domain layer must not depend on ASP.NET Core, Entity Framework, DynamoDB, Dapper, or MediatR");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheAggregator_WhenItDependsOnTheDomainLayer_ThenTheDependencyIsDocumented()
        {
            IArchRule rule = Types()
                .That().ResideInNamespace(ApplicationNamespace)
                .And().HaveName("PlaylistAggregator")
                .Should().DependOnAny(Types().That().ResideInNamespace(DomainNamespace))
                .Because("the aggregator orchestrates the domain models");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheApplicationLayer_WhenItDependsOnInfrastructureLibraries_ThenNoInfrastructureDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespace(ApplicationNamespace)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(AspNetCoreNamespacePattern)
                        .Or().ResideInNamespace(EntityFrameworkNamespace)
                        .Or().ResideInNamespace(DynamoDbNamespace))
                .Because("the application layer must not depend on ASP.NET Core, Entity Framework, or DynamoDB");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenThePersistenceLayer_WhenItDependsOnOtherLayers_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespace(PersistenceNamespace)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace(ApplicationNamespace)
                        .Or().ResideInNamespace(MappingNamespace)
                        .Or().ResideInNamespace(DomainNamespace)
                        .Or().ResideInNamespace(ConfigurationNamespace)
                        .Or().ResideInNamespace(LoggingNamespace))
                .Because("the persistence layer must not depend on the application, mapping, domain, configuration, or logging layers");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheMappingLayer_WhenItDependsOnConfigurationOrLogging_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespace(MappingNamespace)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace(ConfigurationNamespace)
                        .Or().ResideInNamespace(LoggingNamespace))
                .Because("the mapping layer must convert between the persistence and domain layers only");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheConfigurationLayer_WhenItDependsOnOtherProductionLayers_ThenNoDependencyIsAllowed()
        {
            IArchRule rule = Types()
                .That().ResideInNamespace(ConfigurationNamespace)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace(ApplicationNamespace)
                        .Or().ResideInNamespace(MappingNamespace)
                        .Or().ResideInNamespace(DomainNamespace)
                        .Or().ResideInNamespace(PersistenceNamespace)
                        .Or().ResideInNamespace(LoggingNamespace))
                .Because("the configuration layer is a leaf that supplies settings to other layers");

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheProductionAssembly_WhenItsNamespacesAreSliced_ThenNoDependencyCycleExists()
        {
            SliceRule rule = SliceRuleDefinition.Slices()
                .Matching("IptvPlaylistAggregator.(**)")
                .Should().BeFreeOfCycles();

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheProductionProject_WhenItsTopLevelNamespacesAreSliced_ThenNoDependencyCycleExists()
        {
            SliceRule rule = SliceRuleDefinition.Slices()
                .Matching("IptvPlaylistAggregator.(*)")
                .Should().BeFreeOfCycles();

            rule.Check(Architecture);
        }

        [Test]
        public void GivenTheCompositionRoot_WhenItDependsOnApplicationServices_ThenTheDependencyIsDocumented()
        {
            IArchRule rule = Types()
                .That().ResideInNamespace(CompositionNamespace)
                .Should().DependOnAny(Types().That().ResideInNamespace(ApplicationNamespace))
                .Because("the composition root wires the application services");

            rule.Check(Architecture);
        }

        private static Architecture LoadProductionArchitecture()
        {
            ProductionAssembly productionAssembly = typeof(Service.IPlaylistAggregator).Assembly;

            return new ArchLoader()
                .LoadAssemblies(productionAssembly)
                .Build();
        }
    }
}
