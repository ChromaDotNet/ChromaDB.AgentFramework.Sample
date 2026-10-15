#!/bin/sh
# Waits until the ollama-models service of compose.yaml has downloaded both models.
set -e
echo "Waiting for the Ollama models..."
until curl -s "$OLLAMA_ENDPOINT/api/tags" | grep -q '"qwen2.5:3b"' && curl -s "$OLLAMA_ENDPOINT/api/tags" | grep -q '"nomic-embed-text:latest"'; do
  sleep 5
done
echo "The models are ready."
