#!/usr/bin/env python3
"""
GPU-accelerated embedding service for game recommendations
Supports both CUDA (RTX 4070Ti) and MPS (MacBook M2)
"""

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
import torch
import uvicorn
from typing import List
import numpy as np
import logging
import time

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="GPU Embedding Service", version="1.0.0")

# Global model variable
model = None
device = None

class EmbeddingRequest(BaseModel):
    texts: List[str]
    batch_size: int = 32

class SingleEmbeddingRequest(BaseModel):
    text: str

class EmbeddingResponse(BaseModel):
    embeddings: List[List[float]]
    processing_time: float
    device_used: str
    batch_size: int

class SingleEmbeddingResponse(BaseModel):
    embedding: List[float]
    processing_time: float
    device_used: str

def initialize_model():
    """Initialize the sentence transformer model with optimal device"""
    global model, device
    
    # Detect best available device
    if torch.backends.mps.is_available():
        device = 'mps'  # M2 MacBook
        logger.info("🚀 Using Apple M2 GPU (MPS) for embeddings")
    elif torch.cuda.is_available():
        device = 'cuda'  # RTX 4070Ti
        gpu_name = torch.cuda.get_device_name(0)
        logger.info(f"🚀 Using CUDA GPU: {gpu_name}")
    else:
        device = 'cpu'
        logger.info("⚠️  Using CPU - consider GPU acceleration for better performance")
    
    try:
        # Load your specific model - adjust model name as needed
        model_name = 'all-MiniLM-L6-v2'  # 384 dimensions - matches your setup
        logger.info(f"Loading model: {model_name}")
        
        model = SentenceTransformer(model_name)
        model = model.to(device)
        
        # Warm up the model
        warmup_text = ["warmup text for model initialization"]
        _ = model.encode(warmup_text, device=device, show_progress_bar=False)
        
        logger.info(f"✅ Model loaded successfully on {device}")
        logger.info(f"📊 Model embedding dimensions: {model.get_sentence_embedding_dimension()}")
        
    except Exception as e:
        logger.error(f"❌ Failed to initialize model: {e}")
        raise

@app.on_event("startup")
async def startup_event():
    """Initialize model on startup"""
    initialize_model()

@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "device": device,
        "model_loaded": model is not None,
        "embedding_dimensions": model.get_sentence_embedding_dimension() if model else None
    }

@app.post("/embed/batch", response_model=EmbeddingResponse)
async def embed_batch(request: EmbeddingRequest):
    """Generate embeddings for a batch of texts using GPU acceleration"""
    if not model:
        raise HTTPException(status_code=503, detail="Model not initialized")
    
    if not request.texts:
        raise HTTPException(status_code=400, detail="No texts provided")
    
    if len(request.texts) > 1000:
        raise HTTPException(status_code=400, detail="Batch size too large (max 1000)")
    
    start_time = time.time()
    
    try:
        # Optimize batch size based on device
        if device == 'mps':
            # M2 can handle larger batches due to unified memory
            optimal_batch_size = min(request.batch_size, 64)
        elif device == 'cuda':
            # RTX 4070Ti optimization
            optimal_batch_size = min(request.batch_size, 32)
        else:
            # CPU fallback
            optimal_batch_size = min(request.batch_size, 16)
        
        # Generate embeddings with GPU acceleration
        embeddings = model.encode(
            request.texts,
            device=device,
            batch_size=optimal_batch_size,
            show_progress_bar=False,
            convert_to_tensor=False,
            normalize_embeddings=True,  # Important for cosine similarity
            convert_to_numpy=True
        )
        
        processing_time = time.time() - start_time
        
        logger.info(f"✅ Processed {len(request.texts)} embeddings in {processing_time:.2f}s "
                   f"({len(request.texts)/processing_time:.1f} embeddings/sec) on {device}")
        
        return EmbeddingResponse(
            embeddings=embeddings.tolist(),
            processing_time=processing_time,
            device_used=device,
            batch_size=optimal_batch_size
        )
        
    except Exception as e:
        logger.error(f"❌ Error generating embeddings: {e}")
        raise HTTPException(status_code=500, detail=f"Embedding generation failed: {str(e)}")

@app.post("/embed/single", response_model=SingleEmbeddingResponse)
async def embed_single(request: SingleEmbeddingRequest):
    """Generate embedding for a single text"""
    if not model:
        raise HTTPException(status_code=503, detail="Model not initialized")
    
    start_time = time.time()
    
    try:
        embedding = model.encode(
            [request.text],
            device=device,
            show_progress_bar=False,
            convert_to_tensor=False,
            normalize_embeddings=True,
            convert_to_numpy=True
        )
        
        processing_time = time.time() - start_time
        
        return SingleEmbeddingResponse(
            embedding=embedding[0].tolist(),
            processing_time=processing_time,
            device_used=device
        )
        
    except Exception as e:
        logger.error(f"❌ Error generating single embedding: {e}")
        raise HTTPException(status_code=500, detail=f"Embedding generation failed: {str(e)}")

@app.get("/stats")
async def get_stats():
    """Get service statistics"""
    if not model:
        return {"error": "Model not initialized"}
    
    return {
        "device": device,
        "model_name": "all-MiniLM-L6-v2",
        "embedding_dimensions": model.get_sentence_embedding_dimension(),
        "max_batch_size": 64 if device == 'mps' else 32 if device == 'cuda' else 16,
        "cuda_available": torch.cuda.is_available(),
        "mps_available": torch.backends.mps.is_available(),
        "device_count": torch.cuda.device_count() if torch.cuda.is_available() else 0
    }

if __name__ == "__main__":
    logger.info("🚀 Starting GPU Embedding Service")
    logger.info("📋 Available at http://localhost:8001")
    logger.info("📋 Health check: http://localhost:8001/health")
    logger.info("📋 Stats: http://localhost:8001/stats")
    
    uvicorn.run(
        app, 
        host="0.0.0.0", 
        port=8001,
        log_level="info",
        access_log=True
    )