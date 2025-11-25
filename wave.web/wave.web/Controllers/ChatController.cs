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
        private static List<Message> _conversationHistory = new List<Message>();

        public ChatController(IHttpClientFactory factory, DocumentService documentService, RagSearchService ragService)
        {
            _httpClient = factory.CreateClient();
            _documentService = documentService;
            _ragService = ragService;

            _conversationHistory.Add(new Message()
            {
                Role = MessageRole.System.ToString(),
                Content = "You are a helpful, respectful and honest assistant. Always answer as helpfully as possible. Please ensure that your responses are unbiased. If a question does not make any sense, or is not factually coherent, explain why instead of answering something not correct. If you don't know the answer to a question, please don't share false information. You can ask clarifying questions in the case that you feel you don't hace sufficient information. You assist Nick.",
            });
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string messageContent)
        {
            var relevantChunks = _ragService.Search(messageContent); //await _documentService.GetRelevantChunks(messageContent);

            string ragContext = "";
            if (relevantChunks.Any())
            {
                var contextText = string.Join("\n\n---\n\n", relevantChunks.Select(c => (c.Content?.Trim()) ?? ""));

                ragContext =
                    "The following information may help answer the user's question. " +
                    "Use it naturally if relevant, but do not repeat it verbatim.\n\n" +
                    $"Information:\n{contextText}\n\n" +
                    "User question:";
            }

            var combinedInput = string.IsNullOrEmpty(ragContext) ? messageContent : $"{ragContext}\n{messageContent}";

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
                Content = vllmMessage.Content
            };

            _conversationHistory.Add(new Message() { Role = message.Role, Content = message.Content });
            return Content(JsonSerializer.Serialize(message), "application/json");
        }
    }
}
