using Backend.Services.Recommendation.Interfaces;
using Backend.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using static Qdrant.Client.Grpc.Conditions;

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

                await _client.CreateCollectionAsync(
                    collectionName: collectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = (ulong)vectorSize, // 384D for your embeddings
                        Distance = Distance.Cosine, // Optimal for normalized embeddings
                        // Keep in memory during bulk indexing for maximum speed
                        OnDisk = false,
                        HnswConfig = new HnswConfigDiff
                        {
                            // Optimized for bulk indexing speed, then accuracy
                            M = 48, // Good balance of speed and accuracy for 300k vectors
                            EfConstruct = 256, // Higher for better indexing quality
                            FullScanThreshold = 20000, // Avoid full scans during bulk ops
                            MaxIndexingThreads = 0 // Use all CPU cores
                        }
                    },
                    optimizersConfig: new OptimizersConfigDiff
                    {
                        // More segments for parallel bulk processing
                        DefaultSegmentNumber = 16,
                        // Larger segments to handle 300k vectors efficiently
                        MaxSegmentSize = 50000,
                        // Reduce memory pressure during bulk indexing
                        MemmapThreshold = 1000,
                        // Delay indexing until more vectors are added (huge speed boost)
                        IndexingThreshold = 20000,
                        // Less frequent flushes during bulk operations
                        FlushIntervalSec = 30,
                        // Use all CPU cores for optimization
                        MaxOptimizationThreads = new MaxOptimizationThreads
                        {
                            Value = 0
                        }
                    },
                    walConfig: new WalConfigDiff
                    {
                        WalCapacityMb = 128,
                        WalSegmentsAhead = 3
                    }
                );

                _logger.LogInformation("Created bulk-optimized Qdrant collection: {CollectionName} " +
                    "with {VectorSize}D Cosine vectors (M=48, EfConstruct=256, 16 segments)", 
                    collectionName, vectorSize);
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

                // Add payload with proper type handling
                foreach (var kvp in payload)
                {
                    pointStruct.Payload.Add(kvp.Key, ConvertToQdrantValue(kvp.Value));
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
                {
                    _logger.LogWarning("UPSERT DEBUG: Empty vector list provided to UpsertVectorsBulkAsync");
                    return true;
                }

                _logger.LogInformation("UPSERT DEBUG: Starting bulk upsert for {Count} vectors to collection {CollectionName}", 
                    vectors.Count, collectionName);

                // Process in optimized chunks for maximum throughput
                const int optimalChunkSize = 500; // Larger chunks for better performance
                var totalProcessed = 0;
                var totalFailed = 0;
                var initialPointCount = await GetCollectionPointCountAsync(collectionName);
                
                _logger.LogInformation("UPSERT DEBUG: Initial point count: {InitialCount}", initialPointCount);
                
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

                        // Add payload with proper type handling
                        foreach (var kvp in payload)
                        {
                            pointStruct.Payload.Add(kvp.Key, ConvertToQdrantValue(kvp.Value));
                        }

                        points.Add(pointStruct);
                    }

                    if (points.Count > 0)
                    {
                        _logger.LogInformation("UPSERT DEBUG: About to upsert chunk of {Count} vectors", points.Count);
                        // Maximum performance: wait=false for async processing
                        await _client.UpsertAsync(collectionName, points, wait: false);
                        totalProcessed += points.Count;
                        _logger.LogInformation("UPSERT DEBUG: Queued chunk of {Count} vectors to Qdrant (async) - totalProcessed now {Total}", 
                            points.Count, totalProcessed);
                    }
                }

                // Verify completion by checking point count after brief delay
                await Task.Delay(100); // Allow time for async processing
                var finalPointCount = await GetCollectionPointCountAsync(collectionName);
                var actuallyAdded = finalPointCount - initialPointCount;

                _logger.LogInformation("UPSERT DEBUG: Final verification - Initial: {Initial}, Final: {Final}, Added: {Added}, Queued: {Queued}, Failed: {Failed}", 
                    initialPointCount, finalPointCount, actuallyAdded, totalProcessed, totalFailed);

                // Log detailed results and return accurate status
                if (totalFailed > 0 || actuallyAdded < totalProcessed)
                {
                    _logger.LogWarning("UPSERT RESULT: queued {Queued}, failed validation {Failed}, actually persisted {Persisted} vectors to {CollectionName} - RETURNING FALSE", 
                        totalProcessed, totalFailed, actuallyAdded, collectionName);
                    return actuallyAdded > 0; // Partial success if some vectors were added
                }
                else
                {
                    _logger.LogInformation("UPSERT RESULT: Successfully bulk upserted {Count} vectors to collection {CollectionName} - verified {Persisted} persisted - RETURNING TRUE", 
                        totalProcessed, collectionName, actuallyAdded);
                }

                var success = totalFailed == 0 && actuallyAdded >= totalProcessed - totalFailed;
                _logger.LogInformation("UPSERT DEBUG: Final return value: {Success}", success);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk upsert {Count} vectors to collection: {CollectionName}", vectors.Count, collectionName);
                return false;
            }
        }

        private async Task<long> GetCollectionPointCountAsync(string collectionName)
        {
            try
            {
                var info = await _client.GetCollectionInfoAsync(collectionName);
                return (long)info.PointsCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get point count for collection: {CollectionName}", collectionName);
                return 0;
            }
        }

        public async Task<List<VectorSearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 10, Dictionary<string, object>? filter = null, QueryAnalysis? queryAnalysis = null)
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
                    searchFilter = BuildDynamicSearchFilter(filter, queryAnalysis);
                    _logger.LogDebug("Built search filter with {FilterCount} conditions for collection: {CollectionName}", 
                        CountFilterConditions(searchFilter), collectionName);
                }

                // Accuracy-optimized search for game recommendations (10-30 results)
                var searchResult = await _client.SearchAsync(
                    collectionName: collectionName,
                    vector: queryVector,
                    filter: searchFilter,
                    limit: (ulong)limit,
                    // TODO: remove the arguments below if they don't help with search
                    // Search parameters optimized for accuracy over speed
                    searchParams: new SearchParams
                    {
                        // Higher ef for better accuracy - rule of thumb: 2-4x the limit
                        HnswEf = (ulong)Math.Max(128, limit * 4),
                        // Enable exact search for highest accuracy when needed
                        Exact = false, // Keep false for performance, but higher ef compensates
                        // Return vectors for debugging if needed
                        Quantization = null
                    },
                    // Minimal score threshold - let the algorithm rank naturally
                    scoreThreshold: 0.01f,
                    // Return full payload for game recommendations
                    payloadSelector: new WithPayloadSelector
                    {
                         Enable = true
                    },
                    // Return vectors if you need them for analysis
                    vectorsSelector: new WithVectorsSelector
                    {
                        Enable = false
                    });

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

        private Filter BuildDynamicSearchFilter(Dictionary<string, object> filters, QueryAnalysis? queryAnalysis)
        {
            var mustConditions = new List<Condition>();
            var shouldConditions = new List<Condition>();

            // Determine filtering strategy based on query confidence and ambiguity
            var filteringStrategy = DetermineFilteringStrategy(queryAnalysis);

            foreach (var filterItem in filters)
            {
                var condition = CreateFilterCondition(filterItem.Key, filterItem.Value);
                if (condition == null) continue;

                // Apply dynamic logic based on field type and query confidence
                if (ShouldUseStrictFiltering(filterItem.Key, filteringStrategy))
                {
                    mustConditions.Add(condition);
                }
                else
                {
                    shouldConditions.Add(condition);
                }
            }

            return BuildFilterFromConditions(mustConditions, shouldConditions);
        }

        private static FilteringStrategy DetermineFilteringStrategy(QueryAnalysis? queryAnalysis)
        {
            if (queryAnalysis == null)
                return FilteringStrategy.Strict;

            // // High confidence + not ambiguous = strict filtering
            // if (queryAnalysis.ConfidenceScore >= 0.8f && !queryAnalysis.IsAmbiguous)
            //     return FilteringStrategy.Strict;

            // // Low confidence = exploratory filtering  
            // if (queryAnalysis.ConfidenceScore < 0.5f)
            //     return FilteringStrategy.Exploratory;

            // Medium confidence or ambiguous = balanced filtering
            return FilteringStrategy.Balanced;
        }

        private static bool ShouldUseStrictFiltering(string filterKey, FilteringStrategy strategy)
        {
            // Always strict for compatibility and date constraints
            if (IsHardRequirement(filterKey))
                return true;

            return strategy switch
            {
                FilteringStrategy.Strict => true,
                // TODO: review this. should genres or platforms be must conditions?
                FilteringStrategy.Balanced => IsHighPriorityFilter(filterKey),
                FilteringStrategy.Exploratory => false,
                _ => false
            };
        }

        private static bool IsHardRequirement(string filterKey)
        {
            return filterKey is "release_date";
        }

        private static bool IsHighPriorityFilter(string filterKey)
        {
            return filterKey is "genres" or "platforms" or "release_date";
        }

        private static Filter BuildFilterFromConditions(List<Condition> mustConditions, List<Condition> shouldConditions)
        {
            var filter = new Filter();

            if (mustConditions.Count > 0)
            {
                filter.Must.AddRange(mustConditions);
            }

            if (shouldConditions.Count > 0)
            {
                filter.Should.AddRange(shouldConditions);
                // Require at least one "should" condition to match for variety
                filter.MinShould = new MinShould { MinCount = 1 };
            }

            return filter;
        }

        private enum FilteringStrategy
        {
            Strict,      // High confidence: All filters as must (AND logic)
            Balanced,    // Medium confidence: Mixed must/should logic  
            Exploratory  // Low confidence: Prefer should for variety (OR logic)
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

            // Use Qdrant's static Conditions.Match method for array matching
            // This will match if ANY value in the stored array matches ANY of our filter values
            return Match(key, values.ToArray());
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

        private static Value ConvertToQdrantValue(object? value)
        {
            if (value == null)
                return new Value { StringValue = "" };

            if (value is string str)
                return new Value { StringValue = str };
            
            if (value is int intVal)
                return new Value { IntegerValue = intVal };
            
            if (value is long longVal)
                return new Value { IntegerValue = longVal };
            
            if (value is double doubleVal)
                return new Value { DoubleValue = doubleVal };
            
            if (value is float floatVal)
                return new Value { DoubleValue = floatVal };
            
            if (value is bool boolVal)
                return new Value { BoolValue = boolVal };
            
            if (value is DateTime dateTime)
                return new Value { IntegerValue = new DateTimeOffset(dateTime).ToUnixTimeSeconds() };
            
            if (value is DateTimeOffset dateTimeOffset)
                return new Value { IntegerValue = dateTimeOffset.ToUnixTimeSeconds() };
            
            if (value is IEnumerable<string> stringList)
            {
                var listValue = new ListValue();
                foreach (var item in stringList)
                {
                    listValue.Values.Add(new Value { StringValue = item });
                }
                return new Value { ListValue = listValue };
            }
            
            return new Value { StringValue = value.ToString() ?? "" };
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
                    Value.KindOneofCase.ListValue => kvp.Value.ListValue.Values
                        .Select(v => v.StringValue)
                        .ToList(),
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

        public async Task<long> GetPointCountAsync(string collectionName)
        {
            try
            {
                var info = await _client.GetCollectionInfoAsync(collectionName);
                return (long)info.PointsCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get point count for collection: {CollectionName}", collectionName);
                return 0;
            }
        }
    }
}