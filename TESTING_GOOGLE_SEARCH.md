# Testing Google Search Integration

This guide explains how to manually test the Google Search integration in the Wave chat assistant.

## Prerequisites

1. You must have configured Google Search API credentials as described in [GOOGLE_SEARCH_SETUP.md](./GOOGLE_SEARCH_SETUP.md)
2. The application must be running with the vLLM server accessible at `http://localhost:8000`

## Test Scenarios

### Scenario 1: Testing with Real-Time Keywords

**Purpose**: Verify that the system automatically triggers web search when appropriate keywords are detected.

**Test Cases**:

1. **Current Events Query**
   - Input: "What's the latest news about artificial intelligence?"
   - Expected: The system should detect the keyword "latest" and perform a web search
   - Verify: The response should include information from recent web sources

2. **Weather Query**
   - Input: "What's the weather like today in Seattle?"
   - Expected: The system should detect "today" and "weather" keywords and perform a web search
   - Verify: The response should attempt to include current weather information

3. **Recent Year Query**
   - Input: "What happened in 2024?"
   - Expected: The system should detect "2024" and perform a web search
   - Verify: The response should include recent events from 2024

4. **Stock Price Query**
   - Input: "What's the current stock price of Tesla?"
   - Expected: The system should detect "current" and "stock" and perform a web search
   - Verify: The response should attempt to include recent stock information

### Scenario 2: Testing without Web Search Triggers

**Purpose**: Verify that normal queries don't unnecessarily trigger web searches.

**Test Cases**:

1. **General Knowledge Query**
   - Input: "What is the capital of France?"
   - Expected: No web search should be triggered (no real-time keywords)
   - Verify: The model should answer from its knowledge base

2. **Historical Query**
   - Input: "Who was the first president of the United States?"
   - Expected: No web search should be triggered
   - Verify: The model should answer from its knowledge base

### Scenario 3: Testing with Configuration Disabled

**Purpose**: Verify the system gracefully handles missing configuration.

**Test Cases**:

1. **Empty Configuration**
   - Remove or empty the `GoogleSearch:ApiKey` and `GoogleSearch:SearchEngineId` from configuration
   - Start the application
   - Expected: Application should start successfully with a warning logged
   - Input any query
   - Expected: System continues to work normally without web search

### Scenario 4: Testing RAG + Web Search Integration

**Purpose**: Verify that both RAG (Retrieval Augmented Generation) and web search can work together.

**Test Cases**:

1. **Query with Both Context Sources**
   - First, upload some documents using the documents API
   - Input: "What's the latest update on [topic in your documents]?"
   - Expected: System may use both RAG context and web search results
   - Verify: Response considers both local documents and web information

## How to Verify Logs

Check the application logs for:

1. **Successful Configuration**:
   ```
   No warnings about "Google Search Service is not configured"
   ```

2. **Failed Configuration**:
   ```
   warn: wave.web.Services.GoogleSearchService[0]
         Google Search Service is not configured. API Key or Search Engine ID is missing.
   ```

3. **Search Executed**:
   ```
   info: wave.web.Services.GoogleSearchService[0]
         No search results found for query: [query]
   ```
   or successful search with results

4. **Search Error**:
   ```
   error: wave.web.Services.GoogleSearchService[0]
         Error performing Google search for query: [query]
   ```

## Expected Behavior

### When Web Search is Available and Triggered:
1. The query is analyzed for real-time keywords
2. If triggered, a Google Custom Search is performed (up to 5 results)
3. Search results are formatted and provided to the model as context
4. The model generates a response using the web search context
5. The conversation history includes the enriched query with web context

### When Web Search is Not Available:
1. The system continues to function normally
2. RAG search is still performed if relevant documents exist
3. The model responds based on its training data and RAG context

## Troubleshooting

### Issue: No Web Searches Happening
**Possible Causes**:
- API key or Search Engine ID not configured
- Keywords not detected in query
- API quota exhausted (free tier: 100 queries/day)

**Solution**:
- Check logs for configuration warnings
- Verify credentials in configuration
- Try queries with explicit keywords like "latest", "current", "today"
- Check Google Cloud Console for quota status

### Issue: HTTP 403 Errors
**Possible Causes**:
- Invalid API key
- API not enabled in Google Cloud Console
- API key restrictions preventing access

**Solution**:
- Verify API key is correct
- Ensure Custom Search API is enabled in Google Cloud Console
- Check API key restrictions in Google Cloud Console

### Issue: Empty Search Results
**Possible Causes**:
- Query too specific with no matching results
- Search engine configuration issues
- Network connectivity issues

**Solution**:
- Try broader queries
- Verify Custom Search Engine is configured to search the entire web
- Check network connectivity

## Unit Test Coverage

The following areas are covered by automated tests:

- Configuration validation (empty API key, empty search engine ID)
- Service initialization with missing configuration
- Search behavior when not configured
- Logging of configuration warnings
- ChatController initialization with GoogleSearchService

Run tests with:
```bash
cd wave.web
dotnet test
```

## Performance Considerations

- Web searches add latency (typically 0.5-2 seconds per search)
- Free tier limited to 100 queries per day
- Consider monitoring usage in production environments
- The system performs at most one web search per user query
