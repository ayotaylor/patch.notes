# Game Recommendation Engine Setup Guide

## Overview
This implementation provides a complete games recommendation engine using RAG (Retrieval-Augmented Generation), vector databases, and LLMs. The system incorporates user activity (favorites, likes, reviews, followed users) to provide personalized recommendations.

## Architecture Components

### 1. **Vector Database (Qdrant)**
- Stores game embeddings for semantic similarity search
- Easily swappable via `IVectorDatabase` interface
- Default: QdrantVectorDatabase

### 2. **Embedding Service**
- Converts games and user queries to vector representations
- Supports both simple embeddings (current) and production ONNX models
- Easily swappable via `IEmbeddingService` interface
- Default: SentenceTransformerEmbeddingService

### 3. **Language Model (Groq)**
- Processes natural language queries and generates explanations
- Handles follow-up questions and reasoning
- Easily swappable via `ILanguageModel` interface
- Default: GroqLanguageModel with Llama 3.1 8B

### 4. **User Activity Integration**
- Incorporates user favorites, likes, review preferences
- Considers followed users' preferences
- Boosts recommendation confidence based on social signals

## Configuration Required

Add to your `appsettings.json`:

```json
{
  "Qdrant": {
    "Url": "http://localhost:6333"
  },
  "Groq": {
    "ApiKey": "your-groq-api-key-here"
  },
  "EmbeddingModel": {
    "Path": "./models/all-MiniLM-L6-v2/model.onnx",
    "TokenizerPath": "./models/all-MiniLM-L6-v2/tokenizer.json",
    "ConfigPath": "./models/all-MiniLM-L6-v2/config.json"
  }
}
```

## Setup Steps

### 1. Install Qdrant (Vector Database)
```bash
# Using Docker
docker run -p 6333:6333 qdrant/qdrant

# Or install locally
# See: https://qdrant.tech/documentation/quick-start/
```

### 2. Get Groq API Key
1. Sign up at https://console.groq.com/
2. Create an API key
3. Add to configuration or user secrets

### 3. Initialize the System
```bash
# Start your application
dotnet run

# Initialize the vector database (admin endpoint)
POST /api/recommendation/admin/reindex
```

## API Endpoints

### Basic Search
```http
POST /api/recommendation/search
Content-Type: application/json

{
  "query": "I want an RPG that puts me in a happy mood from recent years",
  "maxResults": 10,
  "includeFollowedUsersPreferences": true
}
```

### Personalized Search (Authenticated Users)
```http
POST /api/recommendation/personalized
Authorization: Bearer <jwt-token>
Content-Type: application/json

{
  "query": "Show me games similar to what my friends like",
  "maxResults": 10
}
```

### Continue Conversation
```http
POST /api/recommendation/continue/{conversationId}
Content-Type: application/json

{
  "query": "Actually, I prefer single-player games"
}
```

### Example Queries
```http
GET /api/recommendation/examples
```

### Health Check
```http
GET /api/recommendation/health
```

## Provider Swapping

### To Swap Vector Database
1. Implement `IVectorDatabase` interface
2. Register in DI container:
```csharp
builder.Services.AddScoped<IVectorDatabase, YourVectorDatabase>();
```

### To Swap LLM Provider
1. Implement `ILanguageModel` interface
2. Register in DI container:
```csharp
builder.Services.AddScoped<ILanguageModel, YourLanguageModel>();
```

### To Swap Embedding Service
1. Implement `IEmbeddingService` interface
2. Register in DI container:
```csharp
builder.Services.AddScoped<IEmbeddingService, YourEmbeddingService>();
```

## Production Considerations

### 1. ONNX Model Setup
- Download all-MiniLM-L6-v2 ONNX model files
- Uncomment production methods in `SentenceTransformerEmbeddingService`
- Update configuration paths

### 2. Scaling
- Use Redis for conversation state instead of in-memory
- Implement caching for frequently requested embeddings
- Consider batch processing for large reindexing operations

### 3. Monitoring
- Add logging for recommendation performance
- Monitor vector database performance
- Track LLM API usage and costs

## Example Usage

The system handles queries like:
- "I want to play an RPG that would put me in a happy mood, released in the last few years"
- "Show me horror games similar to Silent Hill"
- "What games do my friends recommend?"
- "I'm looking for something relaxing after work"

It returns structured recommendations with:
- Game details (name, summary, cover, rating, genres, platforms)
- Reasoning for each recommendation
- User activity matches (favorites, likes, social signals)
- Follow-up questions when queries are ambiguous
- Conversation continuity for refined searches

## Integration with Existing Features

The system leverages your existing:
- User authentication and authorization
- Social features (follows, favorites, likes, reviews)
- Game database with rich metadata
- API response patterns and error handling

No changes required to existing functionality - this is a pure addition to your system.