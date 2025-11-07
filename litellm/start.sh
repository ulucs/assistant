#!/usr/bin/env bash

set -e

echo "🚀 Starting LiteLLM proxy..."
echo ""
echo "Make sure you have set the following environment variables:"
echo "  - OPENAI_API_KEY (for GPT models)"
echo "  - ANTHROPIC_API_KEY (for Claude models)"
echo ""

# Check if config file exists
if [ ! -f "litellm/config.yaml" ]; then
    echo "❌ Error: litellm/config.yaml not found"
    exit 1
fi

# Start LiteLLM
litellm --config litellm/config.yaml
