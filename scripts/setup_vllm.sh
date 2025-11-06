#!/bin/bash
set -e

echo "[1/10] Updating system..."
sudo apt update && sudo apt upgrade -y
sudo apt install -y python3 python3-venv python3-pip git wget curl

echo "[2/10] Checking GPU visibility..."
if ! command -v nvidia-smi &> /dev/null; then
  echo "NVIDIA utilities not found. Install NVIDIA CUDA for WSL before continuing."
  exit 1
fi
nvidia-smi || { echo "GPU not detected in WSL. Fix CUDA before continuing."; exit 1; }

echo "[3/10] Creating virtual environment..."
python3 -m venv ~/vllm-env
source ~/vllm-env/bin/activate
pip install --upgrade pip

echo "[4/10] Installing PyTorch with CUDA support..."
pip install torch --index-url https://download.pytorch.org/whl/cu124

echo "[5/10] Verifying GPU access in PyTorch..."
python - <<'EOF'
import torch
print("CUDA available:", torch.cuda.is_available())
print("Device:", torch.cuda.get_device_name(0) if torch.cuda.is_available() else "None")
EOF

echo "[6/10] Installing vLLM..."
pip install "vllm[api]"

echo "[7/10] Installing Hugging Face CLI..."
pip install huggingface_hub

echo "[8/10] Installing Git LFS system binary..."
curl -s https://packagecloud.io/install/repositories/github/git-lfs/script.deb.sh | sudo bash
sudo apt install -y git-lfs
git lfs install

export HUGGINGFACE_HUB_TOKEN=""
echo "[9/10] Downloading Llama 3 3B Instruct Model"
mkdir -p ~/models
cd ~/models
if [ ! -d "Llama-3.2-3B-Instruct" ]; then
  git clone https://nbergesen:$HUGGINGFACE_HUB_TOKEN@huggingface.co/meta-llama/Llama-3.2-3B-Instruct
else
  echo "Model already downloaded, updating repo."
  cd ~/models/Llama-3.2-3B-Instruct
  git pull
fi
cd ~/