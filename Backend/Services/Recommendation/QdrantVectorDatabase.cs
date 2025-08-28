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
                    case "release_year_from":
                    case "release_year_to":
                    case "platforms" when IsExactPlatformFilter(filterItem.Value):
                        mustConditions.Add(condition);
                        break;

                    // Platform alias matching - flexible platform matching
                    case "platform_aliases":
                    case "canonical_platforms":
                        shouldConditions.Add(condition);
                        break;

                    // Semantic expansions - should match for better relevance
                    case "semantic_genres_expanded":
                    case "semantic_moods":
                    case "semantic_mechanics":
                    case "semantic_themes":
                        semanticConditions.Add(condition);
                        break;

                    // Quality preferences
                    case "prefer_semantic_enhanced":
                        shouldConditions.Add(condition);
                        break;

                    // Core content filters - flexible matching
                    case "genres":
                    case "game_modes":
                    case "moods":
                    case "platforms":
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

        private static bool IsExactPlatformFilter(object value)
        {
            // Check if this is a specific platform requirement (not a general preference)
            if (value is string strValue)
            {
                var platforms = strValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
                // If only 1-2 specific platforms are specified, treat as exact requirement
                return platforms.Length <= 2 && platforms.Any(p => 
                    PlatformAliasService.AreSamePlatform(p.Trim(), "Nintendo Switch") ||
                    PlatformAliasService.AreSamePlatform(p.Trim(), "PlayStation 5") ||
                    PlatformAliasService.AreSamePlatform(p.Trim(), "Xbox Series X/S"));
            }
            return false;
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
                switch (value)
                {
                    case string stringValue:
                        // Handle comma-separated values for multi-value fields like genres, platforms, game modes
                        if (stringValue.Contains(','))
                        {
                            var values = stringValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(v => v.Trim())
                                                   .ToList();

                            // Create multiple conditions with OR logic - match ANY of the provided values
                            var conditions = values.Select(value => new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = key,
                                    Match = new Match
                                    {
                                        Text = value
                                    }
                                }
                            }).ToList();

                            // Return a condition that matches ANY of the values
                            return new Condition
                            {
                                Filter = new Filter
                                {
                                    Should = { conditions }
                                }
                            };
                        }
                        else
                        {
                            return new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = key,
                                    Match = new Match
                                    {
                                        Text = stringValue
                                    }
                                }
                            };
                        }

                    case int intValue:
                        // Handle range filters for years
                        if (key.EndsWith("_from"))
                        {
                            return new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = key.Replace("_from", ""),
                                    Range = new Qdrant.Client.Grpc.Range
                                    {
                                        Gte = intValue
                                    }
                                }
                            };
                        }
                        else if (key.EndsWith("_to"))
                        {
                            return new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = key.Replace("_to", ""),
                                    Range = new Qdrant.Client.Grpc.Range
                                    {
                                        Lte = intValue
                                    }
                                }
                            };
                        }
                        else
                        {
                            return new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = key,
                                    Match = new Match
                                    {
                                        Integer = intValue
                                    }
                                }
                            };
                        }

                    case double doubleValue:
                        // Handle range filters with _from/_to suffixes, otherwise exact match
                        if (key.EndsWith("_from"))
                        {
                            return new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = key.Replace("_from", ""),
                                    Range = new Qdrant.Client.Grpc.Range
                                    {
                                        Gte = doubleValue
                                    }
                                }
                            };
                        }
                        else if (key.EndsWith("_to"))
                        {
                            return new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = key.Replace("_to", ""),
                                    Range = new Qdrant.Client.Grpc.Range
                                    {
                                        Lte = doubleValue
                                    }
                                }
                            };
                        }
                        else
                        {
                            return new Condition
                            {
                                Field = new FieldCondition
                                {
                                    Key = key,
                                    Match = new Match
                                    {
                                        Text = doubleValue.ToString()
                                    }
                                }
                            };
                        }

                    case bool boolValue:
                        return new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = key,
                                Match = new Match
                                {
                                    Text = boolValue.ToString().ToLower()
                                }
                            }
                        };

                    default:
                        _logger.LogWarning("Unsupported filter value type: {Type} for key: {Key}", value.GetType(), key);
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create filter condition for key: {Key}", key);
                return null;
            }
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