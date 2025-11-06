#!/bin/bash
set -e

echo "=== Updating and upgrading system packages ==="
sudo apt-get update && sudo apt-get upgrade -y

echo "=== Installing base dependencies ==="
sudo apt-get install -y wget git curl build-essential python3 python3-venv python3-pip python3-dev cmake nvidia-cuda-toolkit software-properties-common

echo "=== Adding NVIDIA CUDA repository ==="
wget https://developer.download.nvidia.com/compute/cuda/repos/wsl-ubuntu/x86_64/cuda-keyring_1.1-1_all.deb
sudo dpkg -i cuda-keyring_1.1-1_all.deb
sudo apt-get update

echo "=== Installing CUDA Toolkit 12.8 (for WSL) ==="
sudo apt-get install -y cuda-toolkit-12-8

echo "=== Verifying CUDA compiler availability ==="
if ! command -v nvcc &>/dev/null; then
  echo "Error: nvcc not found after CUDA installation."
  echo "Please ensure /usr/local/cuda/bin is in PATH."
  export PATH="/usr/local/cuda/bin:$PATH"
fi
nvcc --version || { echo "nvcc still not found — aborting."; exit 1; }

echo "=== Creating isolated Python environment (per PEP 668) ==="
python3 -m venv ~/llama-env
source ~/llama-env/bin/activate
pip install --upgrade pip wheel setuptools

echo "=== Setting GPU architecture for RTX 5070 Ti (Ada Lovelace, SM 8.9) ==="
export TORCH_CUDA_ARCH_LIST="8.9+PTX"

echo "=== Installing PyTorch nightly build for CUDA 12.8 ==="
pip install --pre torch torchvision torchaudio --index-url https://download.pytorch.org/whl/nightly/cu128

echo "=== Installing Hugging Face ecosystem and model tools ==="
pip install huggingface-hub transformers accelerate bitsandbytes sentencepiece safetensors

echo "=== Installing quantization toolkit (AutoAWQ; stable on CUDA 12.x) ==="
pip install autoawq --no-build-isolation

echo "=== Checking GPU and compiler environment ==="
nvidia-smi || echo "Warning: nvidia-smi not found; ensure NVIDIA drivers are installed on Windows side."
nvcc --version
python - <<'EOF'
import torch
print("Torch CUDA available:", torch.cuda.is_available())
print("Torch CUDA version:", torch.version.cuda)
if torch.cuda.is_available():
    print("GPU:", torch.cuda.get_device_name(0))
EOF

echo "=== Environment setup and verification complete ==="
echo "Activate environment with: source ~/llama-env/bin/activate"
echo "Next step: run the LLaMA model setup + quantization script."
