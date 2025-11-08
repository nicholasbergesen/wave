using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using wave.web.Models;

namespace wave.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ChatController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient();
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string messageContent)
        {
            var vllmPayload = new
            {
                model = "meta-llama/Llama-3.2-3B-Instruct",
                messages = new Message[]
                {
                    new Message()
                    {
                        Role = MessageRole.User.ToString(), Content = messageContent
                    }
                },
                temperature = 0.7
            };

            var vllmResponse = await _httpClient.PostAsJsonAsync("http://localhost:8000/v1/chat/completions", vllmPayload);
            var json = await vllmResponse.Content.ReadAsStringAsync();

            return Content(json, "application/json");
        }
    }
}
