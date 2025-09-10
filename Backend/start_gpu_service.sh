#!/bin/bash
# Startup script for GPU Embedding Service
# Works on both Linux (RTX 4070Ti) and macOS (M2)

echo "🚀 Starting GPU Embedding Service Setup..."

# Check if Python 3.8+ is available
if ! command -v python3 &> /dev/null; then
    echo "❌ Python 3 is required but not installed"
    exit 1
fi

# Create virtual environment if it doesn't exist
if [ ! -d "venv" ]; then
    echo "📦 Creating virtual environment..."
    python3 -m venv venv
fi

# Activate virtual environment
echo "🔄 Activating virtual environment..."
source venv/bin/activate

# Install dependencies
echo "📥 Installing dependencies..."
pip install --upgrade pip
pip install -r requirements.txt

# Check GPU availability
echo "🔍 Checking GPU availability..."
python3 -c "
import torch
print(f'CUDA available: {torch.cuda.is_available()}')
if torch.cuda.is_available():
    print(f'CUDA device: {torch.cuda.get_device_name(0)}')
print(f'MPS available: {torch.backends.mps.is_available()}')
"

echo "✅ Setup complete!"
echo "🚀 Starting GPU Embedding Service on http://localhost:8001"
echo "📋 Health check will be available at: http://localhost:8001/health"
echo "📋 Stats available at: http://localhost:8001/stats"
echo ""
echo "Press Ctrl+C to stop the service"

# Start the service
python3 gpu_embedding_service.py