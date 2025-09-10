namespace Backend.Services.Recommendation.Interfaces
{
    public interface IVectorDatabase
    {
        Task<bool> CreateCollectionAsync(string collectionName, int vectorSize);
        Task<bool> UpsertVectorAsync(string collectionName, string id, float[] vector, Dictionary<string, object> payload);
        Task<bool> UpsertVectorsBulkAsync(string collectionName, List<(string id, float[] vector, Dictionary<string, object> payload)> vectors);
        Task<List<VectorSearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 10, Dictionary<string, object>? filter = null, QueryAnalysis? queryAnalysis = null);
        Task<bool> DeleteVectorAsync(string collectionName, string id);
        Task<bool> CollectionExistsAsync(string collectionName);
        Task<long> GetPointCountAsync(string collectionName);
    }

    public class VectorSearchResult
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public Dictionary<string, object> Payload { get; set; } = new();
    }
}