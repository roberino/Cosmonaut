using System;
using System.Linq;
using System.Reflection;
using Cosmonaut.Attributes;
using Cosmonaut.Configuration;
using Cosmonaut.Exceptions;
using Humanizer;

namespace Cosmonaut.Extensions
{
    internal static class CollectionExtensions
    {
        internal static string GetCollectionName(this Type entityType)
        {
            var collectionNameAttribute = entityType.GetTypeInfo().GetCustomAttribute<CosmosCollectionAttribute>();

            var collectionName = collectionNameAttribute?.Name;

            return !string.IsNullOrEmpty(collectionName) ? collectionName : entityType.Name.ToLower().Pluralize();
        }

        internal static ISharedCosmosCollectionInfo GetSharedCollectionInfo(this Type entityType)
        {
            var sharedCosmosCollectionAttribute = entityType.GetTypeInfo().GetCustomAttribute<SharedCosmosCollectionAttribute>();
            var implementsSharedCosmosEntity = entityType.GetTypeInfo().GetInterfaces().Contains(typeof(ISharedCosmosEntity));
            var hasSharedCosmosCollectionAttribute = sharedCosmosCollectionAttribute != null;

            if (hasSharedCosmosCollectionAttribute && !implementsSharedCosmosEntity)
                throw new SharedEntityDoesNotImplementExcepction(entityType);

            if (!hasSharedCosmosCollectionAttribute && implementsSharedCosmosEntity)
                throw new SharedEntityDoesNotHaveAttribute(entityType);

            return sharedCosmosCollectionAttribute;
        }
    }
}