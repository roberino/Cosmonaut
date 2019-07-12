using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cosmonaut.Attributes;
using Cosmonaut.Configuration;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Cosmonaut.Unit
{
    public class DefaultEntityConfigurationProviderTests
    {
        [Fact]
        public void GetEntityCollectionMapping_EntityWithAttributeAnnotations_MapsCollectionAndPartitionKeyPerAttributesSpecified()
        {
            var mapping = DefaultEntityConfigurationProvider.Instance.GetEntityCollectionMapping<MyEntityWithAttributeAnnotations>();

            mapping.CollectionName.Should().Be("CollectionX");
            mapping.PartitionKeyDefinition.Paths.Single().Should()
                .Be($"/{nameof(MyEntityWithAttributeAnnotations.MyPartitionKey1)}");
            mapping.IsShared.Should().BeFalse();
        }

        [Fact]
        public void GetEntityCollectionMapping_EntityWithoutAttributeAnnotations_MapsCollectionAndPartitionKeyPerDefaults()
        {
            var mapping = DefaultEntityConfigurationProvider.Instance.GetEntityCollectionMapping<MyEntityWithoutAnnotation>();

            mapping.CollectionName.Should().Be($"{nameof(MyEntityWithoutAnnotation).ToLower()}s");
            mapping.PartitionKeyDefinition.Should().BeNull();
            mapping.IsShared.Should().BeFalse();
        }

        [CosmosCollection("CollectionX")]
        public class MyEntityWithAttributeAnnotations
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [CosmosPartitionKey]
            public string MyPartitionKey1 { get; set; }
        }

        public class MyEntityWithoutAnnotation
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            public string MyPartitionKey1 { get; set; }
        }
    }
}
