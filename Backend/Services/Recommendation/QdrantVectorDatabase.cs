using Backend.Services.Recommendation.Interfaces;
using Backend.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Backend.Services.Recommendation
{
    public class QdrantVectorDatabase : IVectorDatabase
    {
        private readonly QdrantClient _client;
        private readonly ILogger<QdrantVectorDatabase> _logger;
        private readonly PlatformAliasService _platformAliasService;

        public QdrantVectorDatabase(QdrantClient client, ILogger<QdrantVectorDatabase> logger, PlatformAliasService platformAliasService)
        {
            _client = client;
            _logger = logger;
            _platformAliasService = platformAliasService;
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
                    Distance = Distance.Dot
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
                // Strict validation: NEVER allow upserting vectors with incorrect dimensions
                var (isValid, errorMessage) = EmbeddingDimensionValidator.ValidateEmbeddingDimensions(vector.Length, $"collection {collectionName} upsert");
                if (!isValid)
                {
                    var criticalMessage = $"CRITICAL: Cannot upsert vector with incorrect dimensions. Vector ID: {id}, {errorMessage}. This would corrupt the collection.";
                    _logger.LogCritical(criticalMessage);
                    throw new InvalidOperationException(criticalMessage);
                }

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

        public async Task<bool> UpsertVectorsBulkAsync(string collectionName, List<(string id, float[] vector, Dictionary<string, object> payload)> vectors)
        {
            try
            {
                if (vectors == null || vectors.Count == 0)
                    return true;

                // Process in optimized chunks for maximum throughput
                const int optimalChunkSize = 100;
                var totalProcessed = 0;
                var totalFailed = 0;
                
                for (int i = 0; i < vectors.Count; i += optimalChunkSize)
                {
                    var chunk = vectors.Skip(i).Take(optimalChunkSize).ToList();
                    var points = new List<PointStruct>(chunk.Count);

                    foreach (var (id, vector, payload) in chunk)
                    {
                        // Validate each vector - track failures
                        if (!EmbeddingConstants.ValidateDimensions(vector.Length))
                        {
                            _logger.LogError("Skipping vector {VectorId} due to incorrect dimensions: {ActualDimensions}", 
                                id, vector.Length);
                            totalFailed++;
                            continue;
                        }

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

                        points.Add(pointStruct);
                    }

                    if (points.Count > 0)
                    {
                        // Optimized bulk upsert with wait=false for better throughput
                        await _client.UpsertAsync(collectionName, points, wait: false);
                        totalProcessed += points.Count;
                    }
                }

                // Log detailed results and return accurate status
                if (totalFailed > 0)
                {
                    _logger.LogWarning("Bulk upsert completed with {ProcessedCount} successful and {FailedCount} failed vectors to collection {CollectionName}", 
                        totalProcessed, totalFailed, collectionName);
                }
                else
                {
                    _logger.LogDebug("Bulk upserted {Count} vectors to collection {CollectionName} in {ChunkCount} chunks", 
                        totalProcessed, collectionName, (vectors.Count + optimalChunkSize - 1) / optimalChunkSize);
                }

                // Return true only if all vectors were processed successfully
                return totalFailed == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk upsert {Count} vectors to collection: {CollectionName}", vectors.Count, collectionName);
                return false;
            }
        }

        public async Task<List<VectorSearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 10, Dictionary<string, object>? filter = null)
        {
            try
            {
                // Strict validation: NEVER allow searching with incorrect dimensions
                var (isValid, errorMessage) = EmbeddingDimensionValidator.ValidateEmbeddingDimensions(queryVector.Length, $"collection {collectionName} search");
                if (!isValid)
                {
                    var criticalMessage = $"CRITICAL: Query vector search failed. {errorMessage}. This will cause search failures.";
                    _logger.LogCritical(criticalMessage);
                    throw new InvalidOperationException(criticalMessage);
                }

                Filter? searchFilter = null;

                // Apply filters if provided to enrich the search results
                if (filter != null && filter.Count > 0)
                {
                    searchFilter = BuildSemanticSearchFilter(filter);
                    _logger.LogDebug("Built search filter with {FilterCount} conditions for collection: {CollectionName}", 
                        CountFilterConditions(searchFilter), collectionName);
                }

                var searchResult = await _client.SearchAsync(
                    collectionName: collectionName,
                    vector: queryVector,
                    filter: searchFilter,
                    limit: (ulong)limit);

                _logger.LogDebug("Search returned {ResultCount} results for collection: {CollectionName} with {FilterCount} filters", 
                    searchResult.Count, collectionName, filter?.Count ?? 0);

                return searchResult.Select(point => new VectorSearchResult
                {
                    Id = point.Id.Uuid ?? point.Id.Num.ToString(),
                    Score = point.Score,
                    Payload = ExtractPayload(point.Payload)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search vectors in collection: {CollectionName}", collectionName);
                return [];
            }
        }

        private Filter BuildSemanticSearchFilter(Dictionary<string, object> filters)
        {
            var mustConditions = new List<Condition>();      // Required filters (AND logic)
            var shouldConditions = new List<Condition>();    // Optional filters (OR logic) 
            var semanticConditions = new List<Condition>();  // Semantic expansion filters

            foreach (var filterItem in filters)
            {
                var condition = CreateFilterCondition(filterItem.Key, filterItem.Value);
                if (condition == null) continue;

                // Categorize filters based on their purpose and priority
                switch (filterItem.Key)
                {
                    // Hard requirements - must match exactly
                    case "release_date":
                        mustConditions.Add(condition);
                        break;

                    // Core content filters - flexible matching (should match for better relevance)
                    case "genres":
                    case "platforms":
                    case "game_modes": 
                    case "player_perspectives":
                    default:
                        shouldConditions.Add(condition);
                        break;
                }
            }

            // Build hierarchical filter structure
            var filter = new Filter();

            // Add required conditions (must match ALL)
            if (mustConditions.Count > 0)
            {
                filter.Must.AddRange(mustConditions);
            }

            // Add flexible content conditions (match ANY)
            if (shouldConditions.Count > 0)
            {
                filter.Should.AddRange(shouldConditions);
            }

            // Add semantic expansion conditions (bonus scoring)
            if (semanticConditions.Count > 0)
            {
                // Semantic conditions boost relevance but don't exclude results
                filter.Should.AddRange(semanticConditions);
            }

            // Ensure at least one "should" condition matches if any are specified
            if (shouldConditions.Count > 0 || semanticConditions.Count > 0)
            {
                filter.MinShould = new MinShould { MinCount = 1 };
            }

            return filter;
        }


        private static int CountFilterConditions(Filter? filter)
        {
            if (filter == null) return 0;
            return filter.Must.Count + filter.Should.Count + filter.MustNot.Count;
        }

        private Condition? CreateFilterCondition(string key, object value)
        {
            try
            {
                // Handle Dictionary<string, object> for complex operations ($in, $gte, $lte)
                if (value is Dictionary<string, object> dictValue)
                {
                    return CreateComplexCondition(key, dictValue);
                }

                // Handle string values
                if (value is string stringValue)
                {
                    // Handle comma-separated values (legacy support)
                    if (stringValue.Contains(','))
                    {
                        var values = stringValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(v => v.Trim())
                                               .ToList();
                        return CreateInCondition(key, values);
                    }
                    
                    // Simple string match
                    return CreateMatchCondition(key, stringValue);
                }

                // Handle integer values TODO: check if this is needed
                if (value is int intValue)
                {
                    if (key.EndsWith("_from"))
                    {
                        return CreateRangeCondition(key.Replace("_from", ""), intValue, null);
                    }
                    else if (key.EndsWith("_to"))
                    {
                        return CreateRangeCondition(key.Replace("_to", ""), null, intValue);
                    }
                    else
                    {
                        return CreateMatchCondition(key, intValue);
                    }
                }

                // Handle double values
                if (value is double doubleValue)
                {
                    if (key.EndsWith("_from"))
                    {
                        return CreateRangeCondition(key.Replace("_from", ""), doubleValue, null);
                    }
                    else if (key.EndsWith("_to"))
                    {
                        return CreateRangeCondition(key.Replace("_to", ""), null, doubleValue);
                    }
                    else
                    {
                        return CreateMatchCondition(key, doubleValue.ToString());
                    }
                }

                // Handle boolean values
                if (value is bool boolValue)
                {
                    return CreateMatchCondition(key, boolValue.ToString().ToLower());
                }

                // Unsupported type
                _logger.LogWarning("Unsupported filter value type: {Type} for key: {Key}", value.GetType(), key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create filter condition for key: {Key}", key);
                return null;
            }
        }

        private Condition? CreateComplexCondition(string key, Dictionary<string, object> dictValue)
        {
            // Handle $in operations (lists of values)
            if (dictValue.TryGetValue("$in", out var inValue))
            {
                return CreateInCondition(key, inValue);
            }
                
            // Handle range operations ($gte and $lte)
            var hasGte = dictValue.TryGetValue("$gte", out var gteValue);
            var hasLte = dictValue.TryGetValue("$lte", out var lteValue);
            
            if (hasGte || hasLte)
            {
                var gte = hasGte ? gteValue : null;
                var lte = hasLte ? lteValue : null;
                return CreateRangeCondition(key, gte, lte);
            }
                
            return null;
        }

        private static Condition CreateInCondition(string key, object inValue)
        {
            List<string> values;

            // Convert the input value to a list of strings
            if (inValue is List<string> stringList)
            {
                values = stringList;
            }
            else if (inValue is IEnumerable<string> stringEnum)
            {
                values = stringEnum.ToList();
            }
            else if (inValue is string singleValue)
            {
                values = new List<string> { singleValue };
            }
            else
            {
                values = new List<string> { inValue?.ToString() ?? "" };
            }

            // Create a condition for each value
            var conditions = new List<Condition>();
            foreach (var val in values)
            {
                var condition = new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = key,
                        Match = new Match { Text = val }
                    }
                };
                conditions.Add(condition);
            }

            // Return a condition that matches ANY of the values (OR logic)
            return new Condition
            {
                Filter = new Filter { Should = { conditions } }
            };
        }

        private Condition CreateRangeCondition(string key, object? gteValue, object? lteValue)
        {
            var range = new Qdrant.Client.Grpc.Range();
            
            if (gteValue != null)
                range.Gte = Convert.ToDouble(gteValue);
            if (lteValue != null)
                range.Lte = Convert.ToDouble(lteValue);

            return new Condition
            {
                Field = new FieldCondition
                {
                    Key = key,
                    Range = range
                }
            };
        }

        private Condition CreateMatchCondition(string key, object value)
        {
            var fieldCondition = new FieldCondition { Key = key };

            // Handle different value types for matching
            if (value is int intVal)
            {
                fieldCondition.Match = new Match { Integer = intVal };
            }
            else if (value is string strVal)
            {
                fieldCondition.Match = new Match { Text = strVal };
            }
            else
            {
                fieldCondition.Match = new Match { Text = value.ToString() };
            }

            return new Condition { Field = fieldCondition };
        }


        private static Dictionary<string, object> ExtractPayload(Google.Protobuf.Collections.MapField<string, Value> payload)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in payload)
            {
                object value = kvp.Value.KindCase switch
                {
                    Value.KindOneofCase.StringValue => kvp.Value.StringValue,
                    Value.KindOneofCase.IntegerValue => kvp.Value.IntegerValue,
                    Value.KindOneofCase.DoubleValue => kvp.Value.DoubleValue,
                    Value.KindOneofCase.BoolValue => kvp.Value.BoolValue,
                    _ => kvp.Value.StringValue
                };

                result[kvp.Key] = value;
            }

            return result;
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