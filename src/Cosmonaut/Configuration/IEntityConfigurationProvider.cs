using System;

namespace Cosmonaut.Configuration
{
    public interface IEntityConfigurationProvider
    {
        EntityCollectionMapping GetEntityCollectionMapping<TEntity>();
        EntityCollectionMapping GetEntityCollectionMapping(Type entityType);
    }
}