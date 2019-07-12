using FluentAssertions;
using Newtonsoft.Json;
using System;
using Xunit;

namespace Cosmonaut.Unit
{
    public class FluentCollectionMappingBuilderExtensionsTests
    {
        [Fact]
        public void ConfigureMappings_SingleCallTwoMappings_MapsBothEntities()
        {
            var settings = new CosmosStoreSettings("db1", new Uri("http://some-host-123/"), "xyz");

            settings.ConfigureMappings(m =>
            {
                m.Configure<MyEntity1>(c => c
                    .WithCollection("CollectionX", false)
                    .WithPartition(x => x.MyPartitionKey1));

                m.Configure<MyEntity2>(c => c
                    .WithCollection("CollectionY", false)
                    .WithPartition(x => x.MyPartitionKey2));
            });

            settings.EntityConfigurationProvider.GetEntityCollectionMapping<MyEntity1>().CollectionName.Should()
                .Be("CollectionX");

            settings.EntityConfigurationProvider.GetEntityCollectionMapping<MyEntity2>().CollectionName.Should()
                .Be("CollectionY");
        }

        [Fact]
        public void ConfigureMappings_TwoCallsSingleMappingPerCall_MapsBothEntities()
        {
            var settings = new CosmosStoreSettings("db1", new Uri("http://some-host-123/"), "xyz");

            settings.ConfigureMappings(m =>
            {
                m.Configure<MyEntity1>(c => c
                    .WithCollection("CollectionX", false)
                    .WithPartition(x => x.MyPartitionKey1));
            });

            settings.ConfigureMappings(m =>
            {
                m.Configure<MyEntity2>(c => c
                    .WithCollection("CollectionY", false)
                    .WithPartition(x => x.MyPartitionKey2));
            });

            settings.EntityConfigurationProvider.GetEntityCollectionMapping<MyEntity1>().CollectionName.Should()
                .Be("CollectionX");

            settings.EntityConfigurationProvider.GetEntityCollectionMapping<MyEntity2>().CollectionName.Should()
                .Be("CollectionY");
        }

        [Fact]
        public void ConfigureMappings_SecondCallWithOverride_OverridesFirstCall()
        {
            var settings = new CosmosStoreSettings("db1", new Uri("http://some-host-123/"), "xyz");

            settings.ConfigureMappings(m =>
            {
                m.Configure<MyEntity1>(c => c
                    .WithCollection("CollectionX", false)
                    .WithPartition(x => x.MyPartitionKey1));
            });

            settings.ConfigureMappings(m =>
            {
                m.Configure<MyEntity1>(c => c
                    .WithCollection("CollectionY", false)
                    .WithPartition(x => x.MyPartitionKey1));
            });

            settings.EntityConfigurationProvider.GetEntityCollectionMapping<MyEntity1>().CollectionName.Should()
                .Be("CollectionY");
        }

        public class MyEntity1
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            public string MyPartitionKey1 { get; set; }
        }

        public class MyEntity2
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            public string MyPartitionKey2 { get; set; }
        }
    }
}
