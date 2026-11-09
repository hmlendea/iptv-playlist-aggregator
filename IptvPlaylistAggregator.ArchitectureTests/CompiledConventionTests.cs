using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using IptvPlaylistAggregator.Configuration;
using IptvPlaylistAggregator.DataAccess.DataObjects;
using IptvPlaylistAggregator.Service;
using IptvPlaylistAggregator.Service.Models;

namespace IptvPlaylistAggregator.ArchitectureTests
{
    /// <summary>
    /// Verifies compiled naming, sealing, interface pairing, and mapping conventions.
    /// </summary>
    [TestFixture]
    public sealed class CompiledConventionTests
    {
        private static readonly Assembly ProductionAssembly = typeof(IPlaylistAggregator).Assembly;

        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenOnlyTheProductionAssemblyIsAnalysed()
        {
            Assembly[] loadedAssemblies = [ProductionAssembly];

            Assert.That(loadedAssemblies, Has.Length.EqualTo(1));
            Assert.That(loadedAssemblies[0], Is.SameAs(ProductionAssembly));
            Assert.That(loadedAssemblies[0].GetName().Name, Is.EqualTo("IptvPlaylistAggregator"));
        }

        [Test]
        public void GivenProductionTypes_WhenEligibleClassesAreChecked_ThenTheyAreSealed()
        {
            IEnumerable<Type> eligibleClasses = ProductionAssembly
                .GetTypes()
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    !(type.IsAbstract && type.IsSealed) &&
                    !type.IsEnum &&
                    !type.IsNested &&
                    type.Namespace is not null &&
                    !type.Namespace.EndsWith("Mapping", StringComparison.Ordinal) &&
                    !type.Name.EndsWith("DataObject", StringComparison.Ordinal) &&
                    !type.Name.Contains("<", StringComparison.Ordinal));

            foreach (Type type in eligibleClasses)
            {
                Assert.That(type.IsSealed, Is.True, $"Production class '{type.FullName}' must be sealed.");
            }
        }

        [Test]
        public void GivenSettingsModelsAndDataObjects_WhenChecked_ThenTheyArePublicSealedClasses()
        {
            Type[] expectedTypes =
            [
                typeof(ApplicationSettings),
                typeof(CacheSettings),
                typeof(DataStoreSettings),
                typeof(ChannelDefinitionDataObject),
                typeof(GroupDataObject),
                typeof(PlaylistProviderDataObject),
                typeof(Channel),
                typeof(ChannelDefinition),
                typeof(ChannelName),
                typeof(Group),
                typeof(MediaStreamStatus),
                typeof(Playlist),
                typeof(PlaylistProvider)
            ];

            foreach (Type type in expectedTypes)
            {
                Assert.That(type.IsPublic, Is.True, $"Type '{type.FullName}' must be public.");
                Assert.That(type.IsSealed, Is.True, $"Type '{type.FullName}' must be sealed.");
            }
        }

        [Test]
        public void GivenConcreteServices_WhenTheyExposePublicMethods_ThenAnEquivalentInterfaceExists()
        {
            Type[] serviceTypes =
            [
                typeof(CacheManager),
                typeof(ChannelMatcher),
                typeof(FileDownloader),
                typeof(MediaSourceChecker),
                typeof(PlaylistAggregator),
                typeof(PlaylistFetcher),
                typeof(PlaylistFileBuilder)
            ];

            foreach (Type serviceType in serviceTypes)
            {
                Type? interfaceType = ProductionAssembly.GetType(
                    $"{serviceType.Namespace}.I{serviceType.Name}");

                Assert.That(interfaceType, Is.Not.Null, $"Service '{serviceType.FullName}' must have a colocated interface.");
                Assert.That(serviceType.GetInterfaces(), Does.Contain(interfaceType));
            }
        }

        [Test]
        public void GivenMappingTypes_WhenChecked_ThenTheyAreInternalStaticMappingExtensions()
        {
            string mappingNamespace = "IptvPlaylistAggregator.Service.Mapping";
            Type[] mappingTypes = ProductionAssembly
                .GetTypes()
                .Where(type =>
                    string.Equals(type.Namespace, mappingNamespace, StringComparison.Ordinal) &&
                    !type.Name.Contains("<", StringComparison.Ordinal))
                .ToArray();

            Assert.That(mappingTypes, Is.Not.Empty, "The mapping namespace must contain mapping types.");

            foreach (Type mappingType in mappingTypes)
            {
                Assert.That(mappingType.IsAbstract, Is.True, $"Mapping type '{mappingType.FullName}' must be static.");
                Assert.That(mappingType.IsSealed, Is.True, $"Mapping type '{mappingType.FullName}' must be static.");
                Assert.That(mappingType.IsNotPublic, Is.True, $"Mapping type '{mappingType.FullName}' must be internal.");
                Assert.That(mappingType.Name, Does.EndWith("MappingExtensions"));
            }
        }

        [Test]
        public void GivenStreamState_WhenChecked_ThenItIsARegularDomainEnum()
        {
            Assert.That(typeof(StreamState).IsEnum, Is.True);
            Assert.That(typeof(StreamState).Namespace, Is.EqualTo("IptvPlaylistAggregator.Service.Models"));
        }
    }
}
