﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cosmonaut.Configuration;
using Cosmonaut.Extensions;
using Cosmonaut.Response;
using Cosmonaut.Storage;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Cosmonaut
{
    public sealed class CosmosStore<TEntity> : ICosmosStore<TEntity> where TEntity : class
    {
        public bool IsShared => EntityCollectionMapping.IsShared;

        public string CollectionName { get; private set; }
        
        public string DatabaseName { get; }

        public CosmosStoreSettings Settings { get; }
        
        public ICosmonautClient CosmonautClient { get; }

        internal EntityCollectionMapping EntityCollectionMapping { get; }

        private readonly IDatabaseCreator _databaseCreator;
        private readonly ICollectionCreator _collectionCreator;

        public CosmosStore(CosmosStoreSettings settings) : this(settings, string.Empty)
        {
        }

        public CosmosStore(CosmosStoreSettings settings, string overriddenCollectionName)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            DatabaseName = settings.DatabaseName;
            var documentClient = DocumentClientFactory.CreateDocumentClient(settings);
            CosmonautClient = new CosmonautClient(documentClient, Settings.InfiniteRetries);
            if (string.IsNullOrEmpty(Settings.DatabaseName)) throw new ArgumentNullException(nameof(Settings.DatabaseName));
            _collectionCreator = new CosmosCollectionCreator(CosmonautClient, settings.EntityConfigurationProvider);
            _databaseCreator = new CosmosDatabaseCreator(CosmonautClient);
            InitialiseCosmosStore(overriddenCollectionName);
        }

        public CosmosStore(ICosmonautClient cosmonautClient,
            string databaseName) : this(cosmonautClient, databaseName, string.Empty,
            new CosmosDatabaseCreator(cosmonautClient),
            new CosmosCollectionCreator(cosmonautClient, null))
        {
        }

        public CosmosStore(ICosmonautClient cosmonautClient,
            string databaseName,
            string overriddenCollectionName) : this(cosmonautClient,
            databaseName,
            overriddenCollectionName,
            new CosmosDatabaseCreator(cosmonautClient),
            new CosmosCollectionCreator(cosmonautClient))
        {
        }

        internal CosmosStore(ICosmonautClient cosmonautClient,
            string databaseName,
            string overriddenCollectionName,
            IDatabaseCreator databaseCreator = null,
            ICollectionCreator collectionCreator = null)
        {
            DatabaseName = databaseName;
            CosmonautClient = cosmonautClient ?? throw new ArgumentNullException(nameof(cosmonautClient));
            Settings = new CosmosStoreSettings(databaseName, cosmonautClient.DocumentClient.ServiceEndpoint.ToString(), string.Empty, cosmonautClient.DocumentClient.ConnectionPolicy);

            if (Settings.InfiniteRetries)
                CosmonautClient.DocumentClient.SetupInfiniteRetries();

            if (string.IsNullOrEmpty(Settings.DatabaseName)) throw new ArgumentNullException(nameof(Settings.DatabaseName));

            _collectionCreator = collectionCreator ?? new CosmosCollectionCreator(CosmonautClient, Settings.EntityConfigurationProvider);
            _databaseCreator = databaseCreator ?? new CosmosDatabaseCreator(CosmonautClient);

            EntityCollectionMapping = InitialiseCosmosStore(overriddenCollectionName);
        }

        public IQueryable<TEntity> Query(FeedOptions feedOptions = null)
        {
            var queryable =
                CosmonautClient.Query<TEntity>(DatabaseName, CollectionName, GetFeedOptionsForQuery(feedOptions));

            return IsShared ? queryable.Where(EntityCollectionMapping.SharedCollectionExpression<TEntity>()) : queryable;
        }

        public IQueryable<TEntity> Query(string sql, object parameters = null, FeedOptions feedOptions = null,
            CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            return CosmonautClient.Query<TEntity>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
        }

        public async Task<TEntity> QuerySingleAsync(string sql, object parameters = null, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            var queryable = CosmonautClient.Query<TEntity>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
            return await queryable.SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object parameters = null, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            var queryable = CosmonautClient.Query<T>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
            return await queryable.SingleOrDefaultAsync(cancellationToken);
        }
        
        public async Task<IEnumerable<TEntity>> QueryMultipleAsync(string sql, object parameters = null, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            var queryable = CosmonautClient.Query<TEntity>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
            return await queryable.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> QueryMultipleAsync<T>(string sql, object parameters = null, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            var queryable = CosmonautClient.Query<T>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
            return await queryable.ToListAsync(cancellationToken);
        }

        public IQueryable<TEntity> Query(string sql, IDictionary<string, object> parameters, FeedOptions feedOptions = null,
            CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            return CosmonautClient.Query<TEntity>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
        }

        public async Task<TEntity> QuerySingleAsync(string sql, IDictionary<string, object> parameters, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            var queryable = CosmonautClient.Query<TEntity>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
            return await queryable.SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, IDictionary<string, object> parameters, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            var queryable = CosmonautClient.Query<T>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
            return await queryable.SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<TEntity>> QueryMultipleAsync(string sql, IDictionary<string, object> parameters, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            var queryable = CosmonautClient.Query<TEntity>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
            return await queryable.ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> QueryMultipleAsync<T>(string sql, IDictionary<string, object> parameters, FeedOptions feedOptions = null, CancellationToken cancellationToken = default)
        {
            var collectionSharingFriendlySql = EntityCollectionMapping.EnsureQueryIsCollectionSharingFriendly(sql);
            var queryable = CosmonautClient.Query<T>(DatabaseName, CollectionName, collectionSharingFriendlySql, parameters, GetFeedOptionsForQuery(feedOptions));
            return await queryable.ToListAsync(cancellationToken);
        }

        public async Task<CosmosResponse<TEntity>> AddAsync(TEntity entity, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await CosmonautClient.CreateDocumentAsync(DatabaseName, CollectionName, entity,
                GetRequestOptions(requestOptions, entity), cancellationToken);
        }
        
        public async Task<CosmosMultipleResponse<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, Func<TEntity, RequestOptions> requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteMultiOperationAsync(entities, x => AddAsync(x, requestOptions?.Invoke(x), cancellationToken));
        }
        
        public async Task<CosmosMultipleResponse<TEntity>> RemoveAsync(
            Expression<Func<TEntity, bool>> predicate, 
            FeedOptions feedOptions = null,
            Func<TEntity, RequestOptions> requestOptions = null,
            CancellationToken cancellationToken = default)
        {
            var entitiesToRemove = await Query(GetFeedOptionsForQuery(feedOptions)).Where(predicate).ToListAsync(cancellationToken);
            return await RemoveRangeAsync(entitiesToRemove, requestOptions, cancellationToken);
        }

        public async Task<CosmosResponse<TEntity>> RemoveAsync(TEntity entity, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            entity.ValidateEntityForCosmosDb();
            var documentId = entity.GetDocumentId();
            return await CosmonautClient.DeleteDocumentAsync(DatabaseName, CollectionName, documentId,
                GetRequestOptions(requestOptions, entity), cancellationToken).ExecuteCosmosCommand(entity);
        }
        
        public async Task<CosmosMultipleResponse<TEntity>> RemoveRangeAsync(IEnumerable<TEntity> entities, Func<TEntity, RequestOptions> requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteMultiOperationAsync(entities, x => RemoveAsync(x, requestOptions?.Invoke(x), cancellationToken));
        }

        public async Task<CosmosResponse<TEntity>> UpdateAsync(TEntity entity, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            entity.ValidateEntityForCosmosDb();
            var document = entity.ToCosmonautDocument();
            return await CosmonautClient.UpdateDocumentAsync(DatabaseName, CollectionName, document,
                GetRequestOptions(requestOptions, entity), cancellationToken).ExecuteCosmosCommand(entity);
        }
        
        public async Task<CosmosMultipleResponse<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities, Func<TEntity, RequestOptions> requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteMultiOperationAsync(entities, x => UpdateAsync(x, requestOptions?.Invoke(x), cancellationToken));
        }

        public async Task<CosmosResponse<TEntity>> UpsertAsync(TEntity entity, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            var document = entity.ToCosmonautDocument();
            return await CosmonautClient.UpsertDocumentAsync(DatabaseName, CollectionName, document,
                GetRequestOptions(requestOptions, entity), cancellationToken).ExecuteCosmosCommand(entity);
        }

        public async Task<CosmosMultipleResponse<TEntity>> UpsertRangeAsync(IEnumerable<TEntity> entities, Func<TEntity, RequestOptions> requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteMultiOperationAsync(entities, x => UpsertAsync(x, requestOptions?.Invoke(x), cancellationToken));
        }
        
        public async Task<CosmosResponse<TEntity>> RemoveByIdAsync(string id, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            var response = await CosmonautClient.DeleteDocumentAsync(DatabaseName, CollectionName, id,
                GetRequestOptions(id, requestOptions), cancellationToken);
            return new CosmosResponse<TEntity>(response);
        }

        public async Task<CosmosResponse<TEntity>> RemoveByIdAsync(string id, object partitionKeyValue, CancellationToken cancellationToken = default)
        {
            var requestOptions = partitionKeyValue != null
                ? new RequestOptions { PartitionKey = new PartitionKey(partitionKeyValue) }
                : null;

            return await RemoveByIdAsync(id, requestOptions, cancellationToken);
        }

        public async Task<TEntity> FindAsync(string id, RequestOptions requestOptions = null, CancellationToken cancellationToken = default)
        {
            return await CosmonautClient.GetDocumentAsync<TEntity>(DatabaseName, CollectionName, id,
                GetRequestOptions(id, requestOptions), cancellationToken);
        }

        public async Task<TEntity> FindAsync(string id, object partitionKeyValue, CancellationToken cancellationToken = default)
        {
            var requestOptions = partitionKeyValue != null
                ? new RequestOptions { PartitionKey = new PartitionKey(partitionKeyValue) }
                : null;
            return await FindAsync(id, requestOptions, cancellationToken);
        }

        public async Task<bool> EnsureInfrastructureProvisionedAsync()
        {
            var databaseCreated =
                await _databaseCreator.EnsureCreatedAsync(DatabaseName, Settings.DefaultDatabaseThroughput);
            var collectionCreated = await _collectionCreator.EnsureCreatedAsync<TEntity>(DatabaseName, CollectionName,
                Settings.DefaultCollectionThroughput, Settings.IndexingPolicy, Settings.OnDatabaseThroughput, Settings.UniqueKeyPolicy);

            return databaseCreated && collectionCreated;
        }
        
        private EntityCollectionMapping InitialiseCosmosStore(string overridenCollectionName)
        {
            var collectionInfo = Settings.EntityConfigurationProvider.GetEntityCollectionMapping<TEntity>();

            CollectionName = collectionInfo.GetCosmosStoreCollectionName(Settings.CollectionPrefix, overridenCollectionName);

            if (Settings.ProvisionInfrastructureIfMissing)
            {
                EnsureInfrastructureProvisionedAsync().GetAwaiter().GetResult();
            }

            return collectionInfo;
        }

        private async Task<CosmosMultipleResponse<TEntity>> ExecuteMultiOperationAsync(IEnumerable<TEntity> entities,
            Func<TEntity, Task<CosmosResponse<TEntity>>> operationFunc)
        {
            var multipleResponse = new CosmosMultipleResponse<TEntity>();

            var entitiesList = entities.ToList();
            if (!entitiesList.Any())
                return multipleResponse;
            
            var results = (await entitiesList.Select(operationFunc).WhenAllTasksAsync()).ToList();
            multipleResponse.SuccessfulEntities.AddRange(results.Where(x => x.IsSuccess));
            multipleResponse.FailedEntities.AddRange(results.Where(x => !x.IsSuccess));
            return multipleResponse;
        }

        private RequestOptions GetRequestOptions(RequestOptions requestOptions, TEntity entity)
        {
            var partitionKeyValue = entity.GetPartitionKeyValueForEntity();
            if (requestOptions == null)
            {
                return partitionKeyValue != null ? new RequestOptions
                {
                    PartitionKey = partitionKeyValue
                } : null;
            }

            requestOptions.PartitionKey = partitionKeyValue;
            return requestOptions;
        }

        private RequestOptions GetRequestOptions(string id, RequestOptions requestOptions)
        {
            var partitionKeyDefinition = typeof(TEntity).GetPartitionKeyDefinitionForEntity();
            var partitionKeyIsId = partitionKeyDefinition?.Paths?.SingleOrDefault()?.Equals($"/{CosmosConstants.CosmosId}") ?? false;
            if (requestOptions == null && partitionKeyIsId)
            {
                return new RequestOptions
                {
                    PartitionKey = new PartitionKey(id)
                };
            }

            if (requestOptions != null && partitionKeyIsId)
                requestOptions.PartitionKey = new PartitionKey(id);

            return requestOptions;
        }

        private FeedOptions GetFeedOptionsForQuery(FeedOptions feedOptions)
        {
            var shouldEnablePartitionQuery = (typeof(TEntity).HasPartitionKey() && feedOptions?.PartitionKey == null) 
                                             || (feedOptions != null && feedOptions.EnableCrossPartitionQuery);

            if (feedOptions == null)
            {
                return new FeedOptions
                {
                    EnableCrossPartitionQuery = shouldEnablePartitionQuery
                };
            }

            feedOptions.EnableCrossPartitionQuery = shouldEnablePartitionQuery;
            return feedOptions;
        }
    }
}