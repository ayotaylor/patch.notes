# GPU-Accelerated Vector Indexing Guide

This guide shows you how to dramatically speed up your game vector indexing from **7.5 hours to ~20-30 minutes** using GPU acceleration and optimized parallel processing.

## 🚀 Performance Improvements Implemented

| Optimization | Expected Speedup | Status |
|-------------|------------------|---------|
| **Docker-optimized Qdrant** | 1.5-2x faster | ✅ Complete |
| **Bulk indexing collection settings** | 2-3x faster | ✅ Complete |
| **Parallel batch processing** | 2-4x faster | ✅ Complete |
| **GPU-accelerated embeddings** | 5-10x faster | ✅ Complete |
| **Combined optimizations** | **10-20x faster** | ✅ Ready to test |

## 📋 Setup Instructions

### Step 1: Start Optimized Qdrant
```bash
cd /home/ayo/workspace
docker-compose up -d qdrant
```

**Verify Qdrant is running:**
```bash
curl http://localhost:6333/health
docker-compose logs qdrant
```

### Step 2: Setup GPU Embedding Service
```bash
cd /home/ayo/workspace/patch.notes/Backend

# Make sure script is executable
chmod +x start_gpu_service.sh

# Start the GPU service (works on both RTX 4070Ti and M2)
./start_gpu_service.sh
```

**Verify GPU service is running:**
```bash
# Health check
curl http://localhost:8001/health

# Check GPU detection and stats
curl http://localhost:8001/stats
```

### Step 3: Configure Your Application

**appsettings.Development.json** is already configured with:
```json
{
  "EmbeddingService": {
    "UseGpuService": true,
    "GpuServiceUrl": "http://localhost:8001"
  }
}
```

### Step 4: Run Parallel Indexing

**Option A: Use the new parallel method in your code:**
```csharp
var gameIndexingService = serviceProvider.GetRequiredService<GameIndexingService>();

// Index all games with optimized parallel processing
var success = await gameIndexingService.IndexGamesInParallelAsync(
    batchSize: 100,        // Games per batch
    maxParallelism: 8,     // Parallel batches
    skipCount: 0           // Start from beginning
);
```

**Option B: Add an endpoint to trigger indexing:**
```csharp
[HttpPost("reindex-parallel")]
public async Task<IActionResult> ReindexParallel()
{
    var success = await _gameIndexingService.IndexGamesInParallelAsync();
    return Ok(new { Success = success });
}
```

## 🖥️ Platform-Specific Performance

### RTX 4070Ti (Your PC)
- **Qdrant**: 14 CPU cores, 16GB RAM
- **GPU Service**: CUDA acceleration
- **Expected**: 300k vectors in **15-25 minutes**

### MacBook Air M2
- **Qdrant**: 6 CPU cores, 8GB RAM  
- **GPU Service**: Metal Performance Shaders (MPS)
- **Expected**: 300k vectors in **20-35 minutes**

## 📊 Monitoring Performance

### Real-time Progress Monitoring
The parallel indexing provides detailed progress logs:
```
[INFO] Processing 300000 games out of 300000 total games
[INFO] Progress: 15000/300000 games indexed (0 failed) - Rate: 850.2 games/min - ETA: 22.3 minutes
[INFO] Parallel bulk indexing completed: 300000 games indexed, 0 failed in 23.1 minutes (Rate: 1298.7 games/min)
```

### GPU Service Monitoring
Check GPU utilization and performance:
```bash
# Get detailed stats
curl http://localhost:8001/stats

# Example response:
{
  "device": "cuda",
  "model_name": "all-MiniLM-L6-v2", 
  "embedding_dimensions": 384,
  "max_batch_size": 32,
  "cuda_available": true
}
```

### Qdrant Monitoring
```bash
# Collection info
curl http://localhost:6333/collections/games

# Health and performance
curl http://localhost:6333/health
```

## 🔧 Troubleshooting

### GPU Service Issues

**1. Service won't start:**
```bash
# Check Python and dependencies
cd /home/ayo/workspace/patch.notes/Backend
python3 --version
pip install -r requirements.txt
```

**2. GPU not detected:**
```bash
# Test GPU availability
python3 -c "import torch; print(f'CUDA: {torch.cuda.is_available()}'); print(f'MPS: {torch.backends.mps.is_available()}')"
```

**3. C# can't connect to GPU service:**
- Check `appsettings.Development.json` has `"UseGpuService": true`
- Verify service is running: `curl http://localhost:8001/health`
- Check C# logs for connection errors

### Performance Issues

**1. Slow indexing despite optimizations:**
- Check GPU service is actually being used (logs should show GPU device)
- Verify batch sizes are appropriate (100 games per batch recommended)
- Monitor system resources (CPU, RAM, GPU usage)

**2. Memory issues:**
- Reduce `maxParallelism` from 8 to 4
- Reduce `batchSize` from 100 to 50
- Check available RAM with `htop` or Activity Monitor

### Qdrant Issues

**1. Collection creation fails:**
```bash
# Reset collection if needed
curl -X DELETE "http://localhost:6333/collections/games"
```

**2. Performance degradation:**
- Check disk space in `./qdrant_storage`
- Restart Qdrant: `docker-compose restart qdrant`

## 🎯 Usage Examples

### Basic Parallel Indexing
```csharp
// Simple reindex of all games
await gameIndexingService.IndexGamesInParallelAsync();
```

### Resuming Interrupted Indexing
```csharp
// Skip first 50,000 games if already indexed
await gameIndexingService.IndexGamesInParallelAsync(
    batchSize: 100,
    maxParallelism: 8, 
    skipCount: 50000
);
```

### Conservative Settings (Lower Resource Usage)
```csharp
// Gentler on system resources
await gameIndexingService.IndexGamesInParallelAsync(
    batchSize: 50,     // Smaller batches
    maxParallelism: 4, // Fewer parallel processes
    skipCount: 0
);
```

### Performance Testing
```csharp
// Time the operation
var stopwatch = Stopwatch.StartNew();
var success = await gameIndexingService.IndexGamesInParallelAsync();
stopwatch.Stop();

Console.WriteLine($"Indexing completed in {stopwatch.Elapsed.TotalMinutes:F1} minutes");
```

## 🏆 Expected Results

With all optimizations enabled, you should see:

- **Time reduction**: 7.5 hours → 20-30 minutes  
- **Throughput**: ~1000-1500 games/minute
- **GPU utilization**: 70-90% on RTX 4070Ti or M2
- **CPU utilization**: 80-90% across all cores
- **No timeouts or failures** in normal operation

## 🔄 Fallback Behavior

The system is designed with robust fallbacks:

1. **GPU service fails** → Falls back to local ONNX processing
2. **ONNX fails** → Falls back to individual embedding generation  
3. **Parallel processing fails** → Falls back to sequential processing
4. **Network issues** → Automatically retries with backoff

This ensures your indexing will complete even if some optimizations fail.

---

## 🎉 Ready to Test!

1. Start Qdrant: `docker-compose up -d qdrant`
2. Start GPU service: `./start_gpu_service.sh`
3. Run your indexing with the new parallel method
4. Watch your 300k games index in ~20-30 minutes instead of 7.5 hours!

Report any issues or unexpected behavior. The system is designed to provide detailed logging to help diagnose any problems.