using System;
using Cosmonaut.Attributes;
using Humanizer;
using Microsoft.Azure.Documents;

namespace Cosmonaut.Configuration
{
    public class EntityCollectionMapping
    {
        public EntityCollectionMapping(
            Type entityType,
            PartitionKeyDefinition partitionKeyDefinition, 
            ISharedCosmosCollectionInfo sharedCollectionInfo, 
            string collectionName)
        {
            PartitionKeyDefinition = partitionKeyDefinition;
            CollectionName = sharedCollectionInfo?.SharedCollectionName ?? collectionName;
            SharedCollectionEntityName = GetSharedCollectionEntityName(entityType, sharedCollectionInfo);
            IsShared = sharedCollectionInfo != null;
        }

        public EntityCollectionMapping(
            Type entityType,
            PartitionKeyDefinition partitionKeyDefinition,
            string collectionName,
            string sharedCollectionEntityName = null)
        {
            PartitionKeyDefinition = partitionKeyDefinition;
            CollectionName = collectionName;
            SharedCollectionEntityName = sharedCollectionEntityName;
            IsShared = sharedCollectionEntityName != null;
        }

        public PartitionKeyDefinition PartitionKeyDefinition { get; }

        public string CollectionName { get; }

        public string SharedCollectionEntityName { get; }

        public bool IsShared { get; }

        internal string GetCosmosStoreCollectionName(string collectionPrefix, string overridenCollectionName)
        {
            var hasOverridenName = !string.IsNullOrEmpty(overridenCollectionName);

            return $"{collectionPrefix ?? string.Empty}{(hasOverridenName ? overridenCollectionName : CollectionName)}";
        }

        private static string GetSharedCollectionEntityName(Type entityType, ISharedCosmosCollectionInfo collectionNameAttribute)
        {
            if (collectionNameAttribute == null)
            {
                return null;
            }

            var collectionName = collectionNameAttribute.UseEntityFullName ? entityType.FullName : collectionNameAttribute.EntityName;

            return !string.IsNullOrEmpty(collectionName) ? collectionName : entityType.Name.ToLower().Pluralize();
        }
    }
}