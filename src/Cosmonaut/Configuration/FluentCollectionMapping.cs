using Cosmonaut.Attributes;
using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Cosmonaut.Configuration
{
    public interface IFluentCollectionMappingBuilder
    {
        IFluentCollectionMappingBuilder Configure<TEntity>(Action<FluentCollectionMapping<TEntity>> config)
            where TEntity : class;
    }

    internal sealed class FluentCollectionMappingBuilder : IFluentCollectionMappingBuilder
    {
        private readonly ProviderImpl provider;

        public FluentCollectionMappingBuilder(IEntityConfigurationProvider baseProvider = null)
        {
            provider = new ProviderImpl(baseProvider ?? DefaultEntityConfigurationProvider.Instance);
        }

        public IFluentCollectionMappingBuilder Configure<TEntity>(Action<FluentCollectionMapping<TEntity>> config)
            where TEntity : class
        {
            var mapper = new FluentCollectionMapping<TEntity>();

            config(mapper);

            provider.Mappings[typeof(TEntity)] = mapper.Mapping;

            return this;
        }

        public IEntityConfigurationProvider Build() => provider;

        private class ProviderImpl : IEntityConfigurationProvider
        {
            private readonly IEntityConfigurationProvider baseProvider;

            public IDictionary<Type, EntityCollectionMapping> Mappings { get; } = new Dictionary<Type, EntityCollectionMapping>();

            public ProviderImpl(IEntityConfigurationProvider baseProvider)
            {
                this.baseProvider = baseProvider;
            }

            public EntityCollectionMapping GetEntityCollectionMapping<TEntity>() =>
                GetEntityCollectionMapping(typeof(TEntity));

            public EntityCollectionMapping GetEntityCollectionMapping(Type entityType)
            {
                if (Mappings.TryGetValue(entityType, out var val))
                {
                    return val;
                }

                return baseProvider.GetEntityCollectionMapping(entityType);
            }
        }
    }

    public sealed class FluentCollectionMapping<TEntity>
        where TEntity : class
    {
        public FluentCollectionMapping()
        {
            Mapping = DefaultEntityConfigurationProvider.DefaultMapping<TEntity>();
        }

        public FluentCollectionMapping<TEntity> WithPartition(Expression<Func<TEntity, object>> partitioningExpression)
        {
            if (partitioningExpression.Body is MemberExpression me)
            {
                return SetPartition(me.Member.Name);
            }

            if (partitioningExpression.Body is UnaryExpression ux && ux.NodeType == ExpressionType.Convert && ux.Operand is MemberExpression me2)
            {
                return SetPartition(me2.Member.Name);
            }

            throw new NotSupportedException(partitioningExpression.ToString());
        }

        public FluentCollectionMapping<TEntity> WithCollection(string collectionName, bool isShared)
        {
            return Reconfigure(null, collectionName, isShared);
        }

        internal EntityCollectionMapping Mapping { get; private set; }

        private FluentCollectionMapping<TEntity> SetPartition(string name)
        {
            var pk = new PartitionKeyDefinition()
            {
                Paths = new Collection<string>(new[] {name})
            };

            return Reconfigure(pk, Mapping.CollectionName, Mapping.IsShared);
        }

        private FluentCollectionMapping<TEntity> Reconfigure(PartitionKeyDefinition partitionKeyDefinition, string collectionName, bool? isShared)
        {
            if (isShared.GetValueOrDefault(Mapping.IsShared))
            {
                Mapping = new EntityCollectionMapping(typeof(TEntity),
                    partitionKeyDefinition ?? Mapping.PartitionKeyDefinition, new SharedCosmosCollectionAttribute(collectionName, typeof(TEntity).Name), collectionName);
            }
            else
            {
                Mapping = new EntityCollectionMapping(typeof(TEntity),
                    partitionKeyDefinition ?? Mapping.PartitionKeyDefinition, collectionName ?? Mapping.CollectionName);
            }

            return this;
        }
    }
}
