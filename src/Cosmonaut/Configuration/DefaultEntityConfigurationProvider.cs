using System;
using Cosmonaut.Extensions;

namespace Cosmonaut.Configuration
{
    internal class DefaultEntityConfigurationProvider : IEntityConfigurationProvider
    {
        private DefaultEntityConfigurationProvider()
        {
        }

        internal static EntityCollectionMapping DefaultMapping<TEntity>()
        {
            return Instance.GetEntityCollectionMapping<TEntity>();
        }

        internal static EntityCollectionMapping DefaultMapping(Type entityType)
        {
            return Instance.GetEntityCollectionMapping(entityType);
        }

        public static readonly IEntityConfigurationProvider Instance = new DefaultEntityConfigurationProvider();

        public EntityCollectionMapping GetEntityCollectionMapping<TEntity>() =>
            GetEntityCollectionMapping(typeof(TEntity));

        public EntityCollectionMapping GetEntityCollectionMapping(Type entityType)
        {
            var pkd = entityType.GetPartitionKeyDefinitionForEntity();
            var collName = entityType.GetCollectionName();
            var sharedCollInfo = entityType.GetSharedCollectionInfo();

            return new EntityCollectionMapping(entityType, pkd, sharedCollInfo, collName);
        }
    }
}