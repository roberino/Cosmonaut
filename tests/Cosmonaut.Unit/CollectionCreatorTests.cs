using Cosmonaut.Storage;
using Cosmonaut.Testing;
using FluentAssertions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Cosmonaut.Unit
{
    public class CollectionCreatorTests
    {
        private readonly Mock<IDocumentClient> _mockDocumentClient;

        public CollectionCreatorTests()
        {
            _mockDocumentClient = MockHelpers.GetMockDocumentClient();
        }
        
        [Fact]
        public async Task EnsureCreatedCreatesCollectionIfMissing()
        {
            // Arrange
            IOrderedQueryable<DocumentCollection> queryable = new EnumerableQuery<DocumentCollection>(new List<DocumentCollection>());

            _mockDocumentClient.Setup(x =>
                x.CreateDocumentCollectionQuery(It.IsAny<string>(), It.IsAny<FeedOptions>())).Returns(queryable);
            var collection = new DocumentCollection {Id = "collection"};
            var collectionResponse = collection.ToResourceResponse(HttpStatusCode.OK);
            var databaseUri = UriFactory.CreateDatabaseUri("databaseName");
            _mockDocumentClient.Setup(x => x.CreateDocumentCollectionAsync(It.Is<Uri>(uri => uri == databaseUri),
                It.Is<DocumentCollection>(coll => coll.Id == collection.Id),
                It.IsAny<RequestOptions>())).ReturnsAsync(collectionResponse);

            var creator = new CosmosCollectionCreator(new CosmonautClient(_mockDocumentClient.Object));

            // Act
            var result = await creator.EnsureCreatedAsync<Dummy>("databaseName", collection.Id, 500);

            // Assert
            result.Should().BeTrue();
        }
    }
}