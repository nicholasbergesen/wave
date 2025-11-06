#!/bin/bash
source ~/vllm-env/bin/activate
export HUGGINGFACE_HUB_TOKEN=""

# Start vLLM API server in background
python -m vllm.entrypoints.openai.api_server \
  --model meta-llama/Llama-3.2-3B-Instruct \
  --gpu-memory-utilization 0.85 \
  --max-model-len 16384 \
  --cpu-offload-gb 0 \
  --max-num-batched-tokens 2048 \
  --hf-token $HUGGINGFACE_HUB_TOKEN \
  --port 8000