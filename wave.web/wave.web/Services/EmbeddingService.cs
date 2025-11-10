using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace wave.web.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;

        public EmbeddingService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<List<float>> GetEmbedding(string text)
        {
            try
            {
                var payload = new
                {
                    model = "meta-llama/Llama-3.2-3B-Instruct",
                    input = text
                };

                var response = await _httpClient.PostAsJsonAsync("http://localhost:8000/v1/embeddings", payload);
                response.EnsureSuccessStatusCode();

                var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
                
                if (embeddingResponse?.Data != null && embeddingResponse.Data.Count > 0)
                {
                    return embeddingResponse.Data[0].Embedding;
                }

                return new List<float>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting embedding: {ex.Message}");
                return new List<float>();
            }
        }

        public double CosineSimilarity(List<float> vector1, List<float> vector2)
        {
            if (vector1 == null || vector2 == null || vector1.Count != vector2.Count || vector1.Count == 0)
            {
                return 0;
            }

            double dotProduct = 0;
            double magnitude1 = 0;
            double magnitude2 = 0;

            for (int i = 0; i < vector1.Count; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0;
            }

            return dotProduct / (magnitude1 * magnitude2);
        }

        private class EmbeddingResponse
        {
            [JsonPropertyName("data")]
            public List<EmbeddingData>? Data { get; set; }
        }

        private class EmbeddingData
        {
            [JsonPropertyName("embedding")]
            public List<float> Embedding { get; set; } = new List<float>();
        }
    }
}
