using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace wave.web.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly MLContext _ml;
        private const int fixedSize = 50;

        public EmbeddingService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _ml = new MLContext();
        }

        public class TextFeatures
        {
            [VectorType]
            public float[]? Features { get; set; }
        }

        public List<float> GetEmbedding(string text)
        {
            try
            {
                var data = _ml.Data.LoadFromEnumerable(new[] { new { Text = text } });
                var pipeline = _ml.Transforms.Text.FeaturizeText("Features", "Text");
                var model = pipeline.Fit(data);
                var transformed = model.Transform(data);
                //return _ml.Data.CreateEnumerable<TextFeatures>(transformed, reuseRowObject: false).First().Features?.ToList() 
                //    ?? new List<float>();

                var features = _ml.Data.CreateEnumerable<TextFeatures>(transformed, reuseRowObject: false)
                    .First()
                    .Features?
                    .ToList()
                    ?? new List<float>();

                // Pad with zeros or truncate to fixed size
                if (features.Count < fixedSize)
                {
                    features.AddRange(Enumerable.Repeat(0f, fixedSize - features.Count));
                }
                else if (features.Count > fixedSize)
                {
                    features = features.Take(fixedSize).ToList();
                }

                return features;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting embedding: {ex.Message}");
                return new List<float>();
            }
        }

        public async Task<List<float>> GetEmbeddingApi(string text)
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

        public double JaccardSimilarity(List<float> vector1, List<float> vector2)
        {
            if (vector1 == null || vector2 == null || vector1.Count == 0 || vector2.Count == 0)
            {
                return 0;
            }

            // Get non-zero indices (which features are present)
            var set1 = vector1.Select((val, idx) => new { val, idx })
                              .Where(x => x.val > 0)
                              .Select(x => x.idx)
                              .ToHashSet();

            var set2 = vector2.Select((val, idx) => new { val, idx })
                              .Where(x => x.val > 0)
                              .Select(x => x.idx)
                              .ToHashSet();

            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();

            return union == 0 ? 0 : (double)intersection / union;
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

            const double epsilon = 1e-8;
            if (Math.Abs(magnitude1) < epsilon || Math.Abs(magnitude2) < epsilon)
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
