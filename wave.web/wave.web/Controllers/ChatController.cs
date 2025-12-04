using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using wave.web.Models;
using wave.web.Services;

namespace wave.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly DocumentService _documentService;
        private readonly RagSearchService _ragService;
        private readonly IGoogleSearchService _googleSearchService;
        private readonly IWebContentFetcherService _webContentFetcher;
        private static List<Message> _conversationHistory = new List<Message>();

        public ChatController(IHttpClientFactory factory, DocumentService documentService, RagSearchService ragService, IGoogleSearchService googleSearchService, IWebContentFetcherService webContentFetcher)
        {
            _httpClient = factory.CreateClient();
            _documentService = documentService;
            _ragService = ragService;
            _googleSearchService = googleSearchService;
            _webContentFetcher = webContentFetcher;

            _conversationHistory.Add(new Message()
            {
                Role = MessageRole.System.ToString(),
                Content = "You are a helpful, respectful and honest assistant. Always answer as helpfully as possible. Please ensure that your responses are unbiased. If a question does not make any sense, or is not factually coherent, explain why instead of answering something not correct. If you don't know the answer to a question, please don't share false information. You can ask clarifying questions in the case that you feel you don't have sufficient information. You assist Nick. When you need current, real-time, or recent information (like news, weather, stock prices, current events, or anything after your training data cutoff), say '[WEB_SEARCH_NEEDED]' at the start of your response followed by the search query you want to perform.",
            });
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string messageContent)
        {
            bool usedWebSearch = false;
            bool usedRag = false;

            // First, check RAG for relevant context
            var relevantChunks = _ragService.Search(messageContent);
            string ragContext = "";
            if (relevantChunks.Any())
            {
                usedRag = true;
                var contextText = string.Join("\n\n---\n\n", relevantChunks.Select(c => (c.Content?.Trim()) ?? ""));

                ragContext =
                    "The following information may help answer the user's question. " +
                    "Use it naturally if relevant, but do not repeat it verbatim.\n\n" +
                    $"Information:\n{contextText}\n\n" +
                    "User question:";
            }

            // Add user message with RAG context if available
            var userMessageContent = string.IsNullOrEmpty(ragContext) ? messageContent : $"{ragContext}\n{messageContent}";
            _conversationHistory.Add(new Message()
            {
                Role = MessageRole.User.ToString(),
                Content = userMessageContent
            });

            // Get initial LLM response
            var initialResponse = await GetLlmResponse();
            
            // Check if LLM requested web search
            if (_googleSearchService.IsConfigured() && initialResponse.Contains("[WEB_SEARCH_NEEDED]"))
            {
                // Extract search query from the response
                var searchQuery = ExtractSearchQuery(initialResponse, messageContent);
                
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    usedWebSearch = true;
                    
                    // Perform web search
                    var searchResults = await _googleSearchService.SearchAsync(searchQuery, 3);
                    if (searchResults.Any())
                    {
                        // Fetch full content from top results and save as RAG documents
                        var contentParts = new List<string>();
                        foreach (var result in searchResults.Take(3))
                        {
                            var content = await _webContentFetcher.FetchPageContentAsync(result.Link);
                            if (!string.IsNullOrEmpty(content))
                            {
                                // Save to RAG for future use
                                var docId = $"web_{result.Link.GetHashCode()}_{DateTime.UtcNow.Ticks}";
                                _ragService.AddDocument(docId, content);

                                // Add to context with metadata
                                contentParts.Add($"Source: {result.Title} ({result.DisplayLink})\nContent: {content}");
                            }
                            else
                            {
                                // Fallback to snippet if full content fetch fails
                                contentParts.Add($"Source: {result.Title} ({result.DisplayLink})\nSummary: {result.Snippet}");
                            }
                        }

                        if (contentParts.Any())
                        {
                            // Replace the initial response with search context and re-query LLM
                            _conversationHistory.RemoveAt(_conversationHistory.Count - 1); // Remove initial assistant response
                            
                            var webSearchContext =
                                "I found the following recent information from the web that may help answer your question:\n\n" +
                                $"{string.Join("\n\n---\n\n", contentParts)}\n\n" +
                                "User question: ";
                            
                            // Update the user message with web search context
                            _conversationHistory[_conversationHistory.Count - 1] = new Message()
                            {
                                Role = MessageRole.User.ToString(),
                                Content = $"{webSearchContext}\n{messageContent}"
                            };
                            
                            // Get new response with web search context
                            initialResponse = await GetLlmResponse();
                        }
                    }
                }
            }

            var message = new Message()
            {
                Role = MessageRole.Assistant.ToString(),
                Content = initialResponse,
                UsedWebSearch = usedWebSearch,
                UsedRag = usedRag
            };

            _conversationHistory.Add(new Message() { Role = message.Role, Content = message.Content });
            return Content(JsonSerializer.Serialize(message), "application/json");
        }

        private async Task<string> GetLlmResponse()
        {
            var vllmPayload = new
            {
                model = "meta-llama/Llama-3.2-3B-Instruct",
                messages = _conversationHistory,
                temperature = 0.7
            };

            var vllmResponse = await _httpClient.PostAsJsonAsync("http://localhost:8000/v1/chat/completions", vllmPayload);
            var jsonResponse = await vllmResponse.Content.ReadFromJsonAsync<VllmChatCompletionResponse>();
            var vllmMessage = jsonResponse?.Choices?.First().Message ?? new VllmMessage() { Role = MessageRole.Assistant.ToString(), Content = "Unable to read message from model response." };
            
            return vllmMessage.Content ?? "Unable to read message from model response.";
        }

        private string ExtractSearchQuery(string llmResponse, string originalQuery)
        {
            // Try to extract the search query after [WEB_SEARCH_NEEDED]
            var searchMarker = "[WEB_SEARCH_NEEDED]";
            var markerIndex = llmResponse.IndexOf(searchMarker, StringComparison.OrdinalIgnoreCase);
            
            if (markerIndex >= 0)
            {
                // Get text after the marker
                var textAfterMarker = llmResponse.Substring(markerIndex + searchMarker.Length).Trim();
                
                // Take the first line or sentence as the search query
                var lines = textAfterMarker.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    var firstLine = lines[0].Trim();
                    // Remove common punctuation at the end
                    firstLine = firstLine.TrimEnd('.', '?', '!', ',');
                    
                    if (!string.IsNullOrWhiteSpace(firstLine))
                    {
                        return firstLine;
                    }
                }
            }
            
            // Fallback to the original query if we can't extract a specific search query
            return originalQuery;
        }

        private bool RequiresWebSearch(string query)
        {
            // Keywords that suggest the user needs real-time or current information
            var realtimeKeywords = new[]
            {
                "latest", "recent", "current", "today", "now", "this week", "this month", "this year",
                "news", "update", "breaking", "trending", "happening",
                "weather", "stock", "price", "score", "result",
                "2024", "2025" // Current years that suggest recent events
            };

            var lowerQuery = query.ToLower();
            return realtimeKeywords.Any(keyword => lowerQuery.Contains(keyword));
        }
    }
}
