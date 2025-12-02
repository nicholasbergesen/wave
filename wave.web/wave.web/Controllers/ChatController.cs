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

            // Check if the query might benefit from web search
            var webSearchContext = "";
            if (_googleSearchService.IsConfigured() && RequiresWebSearch(messageContent))
            {
                var searchResults = await _googleSearchService.SearchAsync(messageContent, 3);
                if (searchResults.Any())
                {
                    usedWebSearch = true;

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
                        webSearchContext =
                            "I found the following recent information from the web that may help answer your question:\n\n" +
                            $"{string.Join("\n\n---\n\n", contentParts)}\n\n" +
                            "User question: ";
                    }
                }
            }

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

            // Combine contexts in order of priority: web search (most current), then RAG
            var contextPrefix = !string.IsNullOrEmpty(webSearchContext) ? webSearchContext :
                               !string.IsNullOrEmpty(ragContext) ? ragContext : "";
            
            var combinedInput = string.IsNullOrEmpty(contextPrefix) ? messageContent : $"{contextPrefix}\n{messageContent}";

            _conversationHistory.Add(new Message()
            {
                Role = MessageRole.User.ToString(),
                Content = combinedInput
            });

            var vllmPayload = new
            {
                model = "meta-llama/Llama-3.2-3B-Instruct",
                messages = _conversationHistory,
                temperature = 0.7
            };

            var vllmResponse = await _httpClient.PostAsJsonAsync("http://localhost:8000/v1/chat/completions", vllmPayload);
            var jsonResponse = await vllmResponse.Content.ReadFromJsonAsync<VllmChatCompletionResponse>();
            var vllmMessage = jsonResponse?.Choices?.First().Message ?? new VllmMessage() { Role = MessageRole.Assistant.ToString(), Content = "Unable to read message from mdoel response." };
            var message = new Message()
            {
                Role = vllmMessage.Role,
                Content = vllmMessage.Content,
                UsedWebSearch = usedWebSearch,
                UsedRag = usedRag
            };

            _conversationHistory.Add(new Message() { Role = message.Role, Content = message.Content });
            return Content(JsonSerializer.Serialize(message), "application/json");
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
