using System.Collections.Concurrent;

namespace Backend.Services.Recommendation
{
    public class ConversationStateService
    {
        private readonly ConcurrentDictionary<string, ConversationState> _conversations;
        private readonly ILogger<ConversationStateService> _logger;
        private readonly Timer _cleanupTimer;

        public ConversationStateService(ILogger<ConversationStateService> logger)
        {
            _conversations = new ConcurrentDictionary<string, ConversationState>();
            _logger = logger;
            
            // Clean up old conversations every hour
            _cleanupTimer = new Timer(CleanupOldConversations, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        public ConversationState GetOrCreateConversation(string? conversationId, Guid? userId = null)
        {
            conversationId ??= Guid.NewGuid().ToString();

            return _conversations.GetOrAdd(conversationId, _ => new ConversationState
            {
                ConversationId = conversationId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                QueryHistory = new List<string>(),
                Context = new Dictionary<string, object>()
            });
        }

        public ConversationState? GetConversation(string conversationId)
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                conversation.LastAccessedAt = DateTime.UtcNow;
                return conversation;
            }
            return null;
        }

        public void UpdateConversation(string conversationId, string query, List<string> recommendedGameIds)
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                conversation.QueryHistory.Add(query);
                conversation.LastQuery = query;
                conversation.LastRecommendedGameIds = recommendedGameIds;
                conversation.LastAccessedAt = DateTime.UtcNow;
                
                // Keep only the last 10 queries to prevent memory bloat
                if (conversation.QueryHistory.Count > 10)
                {
                    conversation.QueryHistory.RemoveAt(0);
                }
            }
        }

        public void AddContext(string conversationId, string key, object value)
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                conversation.Context[key] = value;
                conversation.LastAccessedAt = DateTime.UtcNow;
            }
        }

        public T? GetContext<T>(string conversationId, string key)
        {
            if (_conversations.TryGetValue(conversationId, out var conversation) &&
                conversation.Context.TryGetValue(key, out var value) &&
                value is T typedValue)
            {
                conversation.LastAccessedAt = DateTime.UtcNow;
                return typedValue;
            }
            return default;
        }

        public void EndConversation(string conversationId)
        {
            _conversations.TryRemove(conversationId, out _);
            _logger.LogInformation("Ended conversation: {ConversationId}", conversationId);
        }

        private void CleanupOldConversations(object? state)
        {
            var cutoff = DateTime.UtcNow.AddHours(-24); // Remove conversations older than 24 hours
            var toRemove = new List<string>();

            foreach (var kvp in _conversations)
            {
                if (kvp.Value.LastAccessedAt < cutoff)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var conversationId in toRemove)
            {
                _conversations.TryRemove(conversationId, out _);
            }

            if (toRemove.Any())
            {
                _logger.LogInformation("Cleaned up {Count} old conversations", toRemove.Count);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }

    public class ConversationState
    {
        public string ConversationId { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public List<string> QueryHistory { get; set; } = new();
        public string? LastQuery { get; set; }
        public List<string> LastRecommendedGameIds { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
    }
}