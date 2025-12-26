# Wave - LLM Chat Assistant with Web Search

Wave is a chat assistant powered by LLM with RAG (Retrieval Augmented Generation) and web search capabilities.

## Features

- 💬 **Chat Interface**: Interactive chat with LLM (using vLLM backend)
- 📚 **RAG Search**: Search through uploaded documents for relevant context
- 🔍 **Web Search**: Automatic web search for real-time information (Google Custom Search API)
- 📄 **Document Management**: Upload and manage documents for RAG

## How to Run

### Setup Environment to Host vLLM
1. `software_setup.sh`
2. `setup_vllm.sh`

### Run Website
1. `run_vllm.sh` - starts the vLLM server. This allows interaction with the model via an API
2. Run wave.web website - consumes the vLLM server API for chat interactions

## Web Search Setup (Optional)

To enable real-time web search functionality:

1. Follow the setup guide: [GOOGLE_SEARCH_SETUP.md](./GOOGLE_SEARCH_SETUP.md)
2. Configure your Google Custom Search API credentials
3. The system will automatically detect queries requiring real-time information

**Note**: The application works without web search configured, but will be limited to its training data and uploaded documents.

## Documentation

- [Google Search Setup Guide](./GOOGLE_SEARCH_SETUP.md) - How to configure the Google Custom Search API
- [Testing Guide](./TESTING_GOOGLE_SEARCH.md) - How to test the web search integration
- [Implementation Summary](./IMPLEMENTATION_SUMMARY.md) - Technical details and architecture

## Technology Stack

- **Backend**: ASP.NET Core 9.0 (C#)
- **Frontend**: React + TypeScript (Vite)
- **LLM Server**: vLLM
- **ML/AI**: 
  - Microsoft.ML.OnnxRuntime (embeddings)
  - Microsoft.ML.Tokenizers (text tokenization)
  - Google Custom Search API (web search)
- **Testing**: MSTest

## Testing

Run tests:
```bash
cd wave.web
dotnet test
```

All 7 unit tests should pass successfully.
