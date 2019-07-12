﻿using System.Linq;
using Cosmonaut.Configuration;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Cosmonaut.Unit
{
    public class FluentCollectionMappingBuilderTests
    {
        [Fact]
        public void Configure_ThenBuildMappingWithPartitionAndCustomCollection_ReturnsExpectedMapping()
        {
            var builder = new FluentCollectionMappingBuilder();

            builder.Configure<MyEntity>(c => c
                .WithCollection("CollectionX", false)
                .WithPartition(x => x.MyPartitionKey));

            var provider = builder.Build();

            var mapping = provider.GetEntityCollectionMapping<MyEntity>();

            mapping.IsShared.Should().BeFalse();
            mapping.CollectionName.Should().Be("CollectionX");
            mapping.PartitionKeyDefinition.Paths.Single().Should().Be($"/{nameof(MyEntity.MyPartitionKey)}");
        }

        public class MyEntity
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            public string MyPartitionKey { get; set; }
        }
    }
}
