using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace wave.web.Services
{
    public record SearchResult(string DocId, double Similarity, string Content);

    public class RagSearchService : IDisposable
    {
        private const string ModelUrl = "https://huggingface.co/optimum/all-MiniLM-L6-v2/resolve/main/model.onnx";
        private const string VocabUrl = "https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/vocab.txt";
        private const string ModelFileName = "model.onnx";
        private const string VocabFileName = "vocab.txt";
        private const string VectorDbFileName = "vectors.bin";
        private const int VectorDim = 384;

        private InferenceSession _session;
        private BertTokenizer _tokenizer;

        private record CachedDoc(float[] Vector, string Content);
        private readonly ConcurrentDictionary<string, CachedDoc> _vectorCache = new();

        public async Task InitializeAsync()
        {
            using var client = new HttpClient();

            if (!File.Exists(ModelFileName))
            {
                using var stream = await client.GetStreamAsync(ModelUrl);
                using var fileStream = new FileStream(ModelFileName, FileMode.CreateNew);
                await stream.CopyToAsync(fileStream);
            }

            if (!File.Exists(VocabFileName))
            {
                using var stream = await client.GetStreamAsync(VocabUrl);
                using var fileStream = new FileStream(VocabFileName, FileMode.CreateNew);
                await stream.CopyToAsync(fileStream);
            }

            _session = new InferenceSession(ModelFileName);
            using var vocabStream = File.OpenRead(VocabFileName);
            var options = new BertOptions { LowerCaseBeforeTokenization = true };
            _tokenizer = BertTokenizer.Create(vocabStream, options);

            LoadVectorsFromDisk();
        }

        public void AddDocument(string docId, string text)
        {
            var vector = GenerateVector(text);
            _vectorCache[docId] = new CachedDoc(vector, text);
            AppendVectorToDisk(docId, vector, text);
        }

        public List<SearchResult> Search(string query, int limit = 10)
        {
            var queryVector = GenerateVector(query);

            return _vectorCache
                .Select(entry => new SearchResult(
                    entry.Key,
                    CosineSimilarity(queryVector, entry.Value.Vector),
                    entry.Value.Content
                ))
                .OrderByDescending(x => x.Similarity)
                .Take(limit)
                .ToList();
        }

        private float[] GenerateVector(string text)
        {
            var tokenIds = _tokenizer.EncodeToIds(text).ToList();
            if (tokenIds.Count > VectorDim - 2) tokenIds = tokenIds.Take(VectorDim - 2).ToList();

            tokenIds.Insert(0, 101);
            tokenIds.Add(102);
            var count = tokenIds.Count;

            long[] inputIds = new long[VectorDim];
            long[] attentionMask = new long[VectorDim];
            long[] tokenTypeIds = new long[VectorDim];

            for (int i = 0; i < VectorDim; i++)
            {
                if (i < count) { inputIds[i] = tokenIds[i]; attentionMask[i] = 1; }
                else { inputIds[i] = 0; attentionMask[i] = 0; }
            }

            var inputTensor = new DenseTensor<long>(inputIds, new[] { 1, VectorDim });
            var maskTensor = new DenseTensor<long>(attentionMask, new[] { 1, VectorDim });
            var typeTensor = new DenseTensor<long>(tokenTypeIds, new[] { 1, VectorDim });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", typeTensor)
            };

            using var results = _session.Run(inputs);
            var lastHiddenState = results.First(x => x.Name == "last_hidden_state").AsTensor<float>();

            return Normalize(MeanPooling(lastHiddenState, attentionMask, count));
        }

        private float[] MeanPooling(Tensor<float> lastHiddenState, long[] attentionMask, int tokenCount)
        {
            var finalVector = new float[VectorDim];
            for (int i = 0; i < tokenCount; i++)
            {
                if (attentionMask[i] == 1)
                {
                    for (int j = 0; j < VectorDim; j++) finalVector[j] += lastHiddenState[0, i, j];
                }
            }
            int validCount = Math.Max(1, tokenCount);
            for (int i = 0; i < VectorDim; i++) finalVector[i] /= validCount;
            return finalVector;
        }

        private float[] Normalize(float[] v)
        {
            double norm = 0;
            foreach (var val in v) norm += val * val;
            norm = Math.Sqrt(norm);
            if (norm == 0) return v;
            for (int i = 0; i < v.Length; i++) v[i] = (float)(v[i] / norm);
            return v;
        }

        private double CosineSimilarity(float[] a, float[] b)
        {
            double dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            return (normA == 0 || normB == 0) ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private void AppendVectorToDisk(string docId, float[] vector, string content)
        {
            lock (VectorDbFileName)
            {
                using var writer = new BinaryWriter(File.Open(VectorDbFileName, FileMode.Append));
                writer.Write(docId);
                writer.Write(content);
                writer.Write(vector.Length);
                foreach (var f in vector) writer.Write(f);
            }
        }

        private void LoadVectorsFromDisk()
        {
            if (!File.Exists(VectorDbFileName)) return;
            using var reader = new BinaryReader(File.Open(VectorDbFileName, FileMode.Open));
            try
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var id = reader.ReadString();
                    var content = reader.ReadString();
                    var len = reader.ReadInt32();
                    var bytes = reader.ReadBytes(len * 4);

                    var vec = new float[len];
                    Buffer.BlockCopy(bytes, 0, vec, 0, bytes.Length);

                    _vectorCache[id] = new CachedDoc(vec, content);
                }
            }
            catch (EndOfStreamException) { }
        }

        public void Dispose() => _session?.Dispose();
    }
}