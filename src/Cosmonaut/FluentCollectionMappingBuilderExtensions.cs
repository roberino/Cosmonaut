using System;
using Cosmonaut.Configuration;

namespace Cosmonaut
{
    public static class FluentCollectionMappingBuilderExtensions
    {
        public static void ConfigureMappings(this CosmosStoreSettings settings, Action<IFluentCollectionMappingBuilder> mapping)
        {
            var builder = new FluentCollectionMappingBuilder(settings.EntityConfigurationProvider);

            mapping?.Invoke(builder);

            settings.EntityConfigurationProvider = builder.Build();
        }
    }
}