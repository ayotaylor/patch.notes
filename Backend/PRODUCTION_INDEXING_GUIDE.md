# Production Game Indexing Guide

## Best Practices for Production Environments

### 1. **Background Job Service (Recommended)**

**Pros:**
- Runs independently of web requests
- Handles failures gracefully with retries
- Configurable scheduling
- No blocking of application startup
- Built-in health checks

**Configuration:**
```json
{
  "RecommendationEngine": {
    "AutoReindex": false,
    "ReindexIntervalHours": 24,
    "BatchSize": 100,
    "MaxRetries": 3
  }
}
```

**Registration:**
```csharp
// Add to Program.cs
builder.Services.AddHostedService<GameIndexingBackgroundService>();
builder.Services.AddScoped<GameChangeTrackingService>();
```

### 2. **Real-time Change Tracking**

**Implementation:** Hook into your existing game service methods:

```csharp
// In GameService.cs
public async Task<Game> UpdateGameAsync(Game game)
{
    // Your existing update logic
    var result = await _context.SaveChangesAsync();
    
    // Track the change for indexing
    await _changeTrackingService.HandleGameUpdatedAsync(game.Id);
    
    return game;
}
```

### 3. **DevOps Integration**

**Docker Compose Setup:**
```yaml
version: '3.8'
services:
  qdrant:
    image: qdrant/qdrant
    ports:
      - "6333:6333"
    volumes:
      - qdrant_data:/qdrant/storage
      
  your-app:
    depends_on:
      - qdrant
    environment:
      - Qdrant__Url=http://qdrant:6333
      - RecommendationEngine__AutoReindex=false
```

**Kubernetes Jobs:**
```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: game-reindex
spec:
  template:
    spec:
      containers:
      - name: indexer
        image: your-app:latest
        command: ["dotnet", "run", "--", "reindex-games", "--batch-size", "100"]
        env:
        - name: ConnectionStrings__mysqldb
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
      restartPolicy: OnFailure
```

### 4. **Monitoring & Alerting**

**Health Check Endpoint:**
```csharp
// Add comprehensive health check
builder.Services.AddHealthChecks()
    .AddCheck<RecommendationSystemHealthCheck>("recommendation-system");
```

**Metrics Collection:**
```csharp
// Track indexing metrics
public class IndexingMetrics
{
    private static readonly Counter IndexedGamesCounter = 
        Metrics.CreateCounter("games_indexed_total", "Total games indexed");
        
    private static readonly Histogram IndexingDuration = 
        Metrics.CreateHistogram("game_indexing_duration_seconds", "Game indexing duration");
        
    public void RecordGameIndexed() => IndexedGamesCounter.Inc();
    public IDisposable TimeIndexing() => IndexingDuration.NewTimer();
}
```

## **Recommended Production Strategy**

### **Phase 1: Initial Setup**
1. Use admin API endpoint for one-time initial indexing
2. Monitor logs and performance
3. Validate index completeness

### **Phase 2: Real-time Updates**
1. Implement change tracking in game service methods
2. Queue updates for background processing
3. Handle failures with retry logic

### **Phase 3: Maintenance**
1. Schedule periodic full reindexing (weekly/monthly)
2. Monitor index health and consistency
3. Implement alerting for indexing failures

## **Configuration Examples**

### **Development:**
```json
{
  "RecommendationEngine": {
    "AutoReindex": true,
    "ReindexIntervalHours": 1,
    "BatchSize": 10
  }
}
```

### **Staging:**
```json
{
  "RecommendationEngine": {
    "AutoReindex": true,
    "ReindexIntervalHours": 6,
    "BatchSize": 50
  }
}
```

### **Production:**
```json
{
  "RecommendationEngine": {
    "AutoReindex": false,
    "ReindexIntervalHours": 24,
    "BatchSize": 100,
    "EnableRealTimeTracking": true,
    "MaxConcurrentIndexing": 5
  }
}
```

## **Command Line Operations**

### **Manual Reindexing:**
```bash
# Full reindex
curl -X POST https://api.yourapp.com/api/recommendation/admin/reindex \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Or using CLI tool
dotnet run -- reindex-games --batch-size 100 --force

# Validation
dotnet run -- validate-index
```

### **Emergency Procedures:**

**Clear and Rebuild Index:**
```bash
# 1. Clear existing collection
curl -X DELETE "http://qdrant:6333/collections/games"

# 2. Rebuild from scratch
curl -X POST https://api.yourapp.com/api/recommendation/admin/reindex \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

## **Performance Considerations**

1. **Batch Processing:** Process games in batches (50-100 at a time)
2. **Rate Limiting:** Don't overwhelm vector database
3. **Resource Management:** Monitor memory usage during indexing
4. **Concurrent Processing:** Limit concurrent embedding generation
5. **Error Handling:** Implement exponential backoff for retries

## **Rollback Strategy**

1. Keep backup of vector database collections
2. Version your embedding generation logic
3. Test index changes in staging first
4. Monitor recommendation quality after reindexing