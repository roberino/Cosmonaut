using System;
using System.Linq;
using System.Threading.Tasks;
using Cosmonaut.Configuration;
using Cosmonaut.Exceptions;
using Cosmonaut.Extensions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Cosmonaut.Storage
{
    internal class CosmosCollectionCreator : ICollectionCreator
    {
        private readonly ICosmonautClient _cosmonautClient;
        private readonly IEntityConfigurationProvider _entityConfigurationProvider;

        public CosmosCollectionCreator(ICosmonautClient cosmonautClient,
            IEntityConfigurationProvider entityConfigurationProvider = null)
        {
            _cosmonautClient = cosmonautClient;
            _entityConfigurationProvider = entityConfigurationProvider ?? DefaultEntityConfigurationProvider.Instance;
        }

        public async Task<bool> EnsureCreatedAsync<TEntity>(
            string databaseId,
            string collectionId,
            int collectionThroughput,
            IndexingPolicy indexingPolicy = null,
            ThroughputBehaviour onDatabaseBehaviour = ThroughputBehaviour.UseDatabaseThroughput, 
            UniqueKeyPolicy uniqueKeyPolicy = null) where TEntity : class
        {
            var collectionResource = await _cosmonautClient.GetCollectionAsync(databaseId, collectionId);
            var databaseHasOffer = (await _cosmonautClient.GetOfferV2ForDatabaseAsync(databaseId)) != null;

            if (collectionResource != null)
                return true;

            var newCollection = new DocumentCollection
            {
                Id = collectionId,
                IndexingPolicy = indexingPolicy ?? CosmosConstants.DefaultIndexingPolicy,
                UniqueKeyPolicy = uniqueKeyPolicy ?? CosmosConstants.DefaultUniqueKeyPolicy
            };

            var pkd = _entityConfigurationProvider.GetEntityCollectionMapping<TEntity>();

            if (pkd.PartitionKeyDefinition != null)
            {
                newCollection.PartitionKey = pkd.PartitionKeyDefinition;
            }

            var finalCollectionThroughput = databaseHasOffer ? onDatabaseBehaviour == ThroughputBehaviour.DedicateCollectionThroughput ? (int?)collectionThroughput : null : collectionThroughput;

            newCollection = await _cosmonautClient.CreateCollectionAsync(databaseId, newCollection, new RequestOptions
            {
                OfferThroughput = finalCollectionThroughput
            });

            return newCollection != null;
        }
    }
}