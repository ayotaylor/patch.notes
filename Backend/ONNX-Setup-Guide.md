# ONNX Model Setup Guide for Game Recommendation System

## Overview
This guide explains how to set up proper ONNX models to significantly improve embedding quality and search confidence scores in your game recommendation system.

## Quick Setup

### 1. Download Pre-trained Model
Download the **all-MiniLM-L6-v2** model (recommended for gaming contexts):

```bash
# The model files are already provided in the MLModel directory:
# - model.onnx (BERT model)
# - vocab.txt (BERT vocabulary with 30,522 tokens)
# - config.json (Model configuration)

# If you need to download different model files:
# wget https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/model.onnx
# wget https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/vocab.txt
# wget https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/config.json
```

### 2. Configure Application Settings
Add to your `appsettings.json`:

```json
{
  "EmbeddingModel": {
    "UseOnnx": true,
    "Path": "./MLModel/model.onnx",
    "TokenizerPath": "./MLModel/vocab.txt",
    "ConfigPath": "./MLModel/config.json"
  }
}
```

### 3. Enable ONNX Model
The system will automatically detect and use the ONNX model when:
- `EmbeddingModel:UseOnnx` is `true`
- Model files exist at the specified paths
- No initialization errors occur

## Model Options

### Recommended Models for Gaming

| Model | Size | Dimensions | Best For |
|-------|------|------------|----------|
| **all-MiniLM-L6-v2** | 80MB | 384 | General gaming (recommended) |
| all-MiniLM-L12-v2 | 120MB | 384 | Better accuracy, slower |
| all-mpnet-base-v2 | 420MB | 768 | Highest quality, requires config changes |

### Gaming-Optimized Configuration

The production tokenizer automatically includes:
- ✅ **All semantic keywords** from your `DefaultSemanticKeywordMappings.json`
- ✅ **Game genres**: action, rpg, strategy, shooter, etc.
- ✅ **Platform terms**: pc, playstation, xbox, nintendo, mobile
- ✅ **Gaming mechanics**: multiplayer, co-op, open-world, crafting
- ✅ **Common gaming vocabulary**: ~500+ gaming-specific terms

## Expected Performance Improvements

### Before ONNX (Simple Embeddings)
- Confidence scores: 0.15 (artificial minimum)
- Actual similarity scores: 0.05-0.12 (very low)
- Search quality: Poor semantic understanding

### After ONNX (Production Embeddings)
- Confidence scores: 0.3-0.8 (natural scores)
- Actual similarity scores: 0.2-0.7+ (much higher)
- Search quality: Rich semantic understanding

## Advanced Configuration

### Custom Model Path
```json
{
  "EmbeddingModel": {
    "UseOnnx": true,
    "Path": "/custom/path/to/model.onnx",
    "TokenizerPath": "/custom/path/to/vocab.txt",
    "ConfigPath": "/custom/path/to/config.json"
  }
}
```

### Dimension Configuration
If using a different model with different dimensions, update `EmbeddingConstants.cs`:

```csharp
// For all-mpnet-base-v2 (768 dimensions)
public const int BASE_TEXT_EMBEDDING_DIMENSIONS = 768;
```

## Troubleshooting

### Common Issues

**Issue: Model not loading**
```
Solution: Check file permissions and paths in appsettings.json
```

**Issue: Dimension mismatch errors**
```
Solution: Ensure BASE_TEXT_EMBEDDING_DIMENSIONS matches your model's output size
- all-MiniLM-L6-v2: 384 dimensions
- all-MiniLM-L12-v2: 384 dimensions  
- all-mpnet-base-v2: 768 dimensions
```

**Issue: Still getting low confidence scores**
```
Solution: 
1. Verify ONNX model is actually loading (check logs)
2. Remove the 0.15 minimum threshold in GameRecommendationService.cs
3. Re-index your games after enabling ONNX
```

### Validation

Check if ONNX is working:

1. **Log Messages**: Look for "Embedding service initialized with ONNX model" in logs
2. **Dimension Validation**: System will validate dimensions at startup
3. **Performance**: Search results should improve dramatically

### Fallback Behavior

If ONNX fails to load:
- ✅ System automatically falls back to enhanced simple embeddings
- ✅ Uses production tokenizer with gaming vocabulary
- ✅ No system crashes or errors
- ⚠️ Lower quality embeddings than ONNX

## Production Deployment

### Docker Configuration
```dockerfile
# Copy model files  
COPY MLModel/ ./MLModel/

# Ensure proper permissions
RUN chmod -R 755 ./MLModel/
```

### Environment Variables
```bash
EmbeddingModel__UseOnnx=true
EmbeddingModel__Path=./MLModel/model.onnx
EmbeddingModel__TokenizerPath=./MLModel/vocab.txt
EmbeddingModel__ConfigPath=./MLModel/config.json
```

## Performance Impact

### Memory Usage
- Simple embeddings: ~50MB RAM
- ONNX model: ~200-500MB RAM (depending on model)
- Gaming vocabulary: ~5MB additional

### Processing Speed
- Simple embeddings: ~1ms per game
- ONNX embeddings: ~10-50ms per game (depends on hardware)
- Batch processing recommended for large datasets

## Next Steps

1. Download and configure the ONNX model
2. Remove the 0.15 confidence threshold (optional)
3. Re-index your game database
4. Test search quality improvements
5. Monitor performance and adjust as needed

## Support

The system includes comprehensive logging and validation:
- Dimension mismatches are caught at startup
- Model loading failures are logged clearly  
- Fallback mechanisms ensure system stability

For issues, check the application logs for detailed error messages.