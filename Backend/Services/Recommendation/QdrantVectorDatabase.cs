using Backend.Services.Recommendation.Interfaces;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Backend.Services.Recommendation
{
    public class QdrantVectorDatabase : IVectorDatabase
    {
        private readonly QdrantClient _client;
        private readonly ILogger<QdrantVectorDatabase> _logger;

        public QdrantVectorDatabase(QdrantClient client, ILogger<QdrantVectorDatabase> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<bool> CreateCollectionAsync(string collectionName, int vectorSize)
        {
            try
            {
                var collections = await _client.ListCollectionsAsync();
                if (collections.Any(c => c == collectionName))
                {
                    return true;
                }

                await _client.CreateCollectionAsync(collectionName, new VectorParams
                {
                    Size = (ulong)vectorSize,
                    Distance = Distance.Cosine
                });

                _logger.LogInformation("Created Qdrant collection: {CollectionName}", collectionName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create collection: {CollectionName}", collectionName);
                return false;
            }
        }

        public async Task<bool> UpsertVectorAsync(string collectionName, string id, float[] vector, Dictionary<string, object> payload)
        {
            try
            {
                var pointStruct = new PointStruct
                {
                    Id = new PointId { Uuid = id },
                    Vectors = new Vectors { Vector = new Vector { Data = { vector } } }
                };

                // Add payload
                foreach (var kvp in payload)
                {
                    pointStruct.Payload.Add(kvp.Key, new Value { StringValue = kvp.Value?.ToString() ?? "" });
                }

                var points = new List<PointStruct> { pointStruct };
                await _client.UpsertAsync(collectionName, points);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert vector: {Id} in collection: {CollectionName}", id, collectionName);
                return false;
            }
        }

        public async Task<List<VectorSearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 10, Dictionary<string, object>? filter = null)
        {
            try
            {
                var searchResult = await _client.SearchAsync(collectionName, queryVector, limit: (ulong)limit);

                return searchResult.Select(point => new VectorSearchResult
                {
                    Id = point.Id.Uuid ?? point.Id.Num.ToString(),
                    Score = point.Score,
                    Payload = point.Payload.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value.StringValue)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search vectors in collection: {CollectionName}", collectionName);
                return [];
            }
        }

        public async Task<bool> DeleteVectorAsync(string collectionName, string id)
        {
            try
            {
                // var selector = new PointsSelector
                // {
                //     Points = new PointsIdsList
                //     {
                //         Ids = { new PointId { Uuid = id } }
                //     }
                // };
                var points = new List<PointId> { new() { Uuid = id } };

                await _client.DeleteAsync(collectionName, points);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete vector: {Id} from collection: {CollectionName}", id, collectionName);
                return false;
            }
        }

        public async Task<bool> CollectionExistsAsync(string collectionName)
        {
            try
            {
                var collections = await _client.ListCollectionsAsync();
                return collections.Any(c => c == collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if collection exists: {CollectionName}", collectionName);
                return false;
            }
        }
    }
}