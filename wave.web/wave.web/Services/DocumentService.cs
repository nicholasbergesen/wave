using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using wave.web.Models;

namespace wave.web.Services
{
    public class DocumentService
    {
        private readonly string _dataFolder;
        private readonly EmbeddingService _embeddingService;
        private const int ChunkSize = 500; // Characters per chunk

        public DocumentService(EmbeddingService embeddingService)
        {
            _embeddingService = embeddingService;
            _dataFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }
        }

        public async Task<Document> ProcessAndSaveDocument(string fileName, Stream fileStream)
        {
            // Sanitize filename to prevent path traversal
            var sanitizedFileName = Path.GetFileName(fileName);
            
            var document = new Document
            {
                FileName = sanitizedFileName,
                FilePath = Path.Combine(_dataFolder, $"{Guid.NewGuid()}_{sanitizedFileName}")
            };

            // Save the file
            using (var fileStreamWriter = new FileStream(document.FilePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamWriter);
            }

            var fileInfo = new FileInfo(document.FilePath);
            document.FileSize = fileInfo.Length;

            // Extract text and chunk it
            var text = await ExtractTextFromFile(document.FilePath);
            document.Chunks = ChunkText(text, document.Id);

            // Generate embeddings for all chunks
            GenerateEmbeddingsForChunks(document.Chunks);

            // Save metadata
            await SaveDocumentMetadata(document);

            return document;
        }

        private async Task<string> ExtractTextFromFile(string filePath)
        {
            // For now, assume plain text files. Can be extended for PDF, etc.
            return await File.ReadAllTextAsync(filePath);
        }

        private List<DocumentChunk> ChunkText(string text, string documentId)
        {
            var chunks = new List<DocumentChunk>();
            var chunkIndex = 0;

            for (int i = 0; i < text.Length; i += ChunkSize)
            {
                var chunkContent = text.Substring(i, Math.Min(ChunkSize, text.Length - i));
                chunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    Content = chunkContent,
                    ChunkIndex = chunkIndex++
                });
            }

            return chunks;
        }

        private void GenerateEmbeddingsForChunks(List<DocumentChunk> chunks)
        {
            foreach (var chunk in chunks)
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    chunk.Embedding = _embeddingService.GetEmbedding(chunk.Content);
                }
            }
        }

        private async Task SaveDocumentMetadata(Document document)
        {
            var metadataPath = Path.Combine(_dataFolder, $"{document.Id}_metadata.json");
            var json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(metadataPath, json);
        }

        public async Task<List<Document>> GetAllDocuments()
        {
            var documents = new List<Document>();
            var metadataFiles = Directory.GetFiles(_dataFolder, "*_metadata.json");

            foreach (var file in metadataFiles)
            {
                var json = await File.ReadAllTextAsync(file);
                var document = JsonSerializer.Deserialize<Document>(json);
                if (document != null)
                {
                    documents.Add(document);
                }
            }

            return documents;
        }

        public async Task<bool> DeleteDocument(string documentId)
        {
            var documents = await GetAllDocuments();
            var document = documents.FirstOrDefault(d => d.Id == documentId);

            if (document == null)
            {
                return false;
            }

            // Delete the file
            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
            }

            // Delete the metadata
            var metadataPath = Path.Combine(_dataFolder, $"{document.Id}_metadata.json");
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }

            return true;
        }

        public async Task<List<DocumentChunk>> GetRelevantChunks(string query, int topK = 3)
        {
            var documents = await GetAllDocuments();
            var allChunks = documents.SelectMany(d => d.Chunks).ToList();

            // If no chunks available, return empty list
            if (allChunks.Count == 0)
            {
                return new List<DocumentChunk>();
            }

            // Get embedding for the query
            var queryEmbedding = _embeddingService.GetEmbedding(query);

            // If embedding service fails, return empty list
            if (queryEmbedding == null || queryEmbedding.Count == 0)
            {
                return new List<DocumentChunk>();
            }

            // Calculate cosine similarity for each chunk
            var scoredChunks = allChunks
                .Where(chunk => chunk.Embedding != null && chunk.Embedding.Count > 0)
                .Select(chunk => new
                {
                    Chunk = chunk,
                    Similarity = _embeddingService.CosineSimilarity(queryEmbedding, chunk.Embedding!)
                })
                .OrderByDescending(sc => sc.Similarity)
                .Take(topK)
                .Select(sc => sc.Chunk)
                .ToList();

            return scoredChunks;
        }
    }
}
