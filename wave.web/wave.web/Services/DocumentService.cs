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
        private const int ChunkSize = 500; // Characters per chunk

        public DocumentService()
        {
            _dataFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            if (!Directory.Exists(_dataFolder))
            {
                Directory.CreateDirectory(_dataFolder);
            }
        }

        public async Task<Document> ProcessAndSaveDocument(string fileName, Stream fileStream)
        {
            var document = new Document
            {
                FileName = fileName,
                FilePath = Path.Combine(_dataFolder, $"{Guid.NewGuid()}_{fileName}")
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

            // Simple keyword-based relevance (can be improved with embeddings)
            var queryWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var scoredChunks = allChunks.Select(chunk => new
            {
                Chunk = chunk,
                Score = queryWords.Count(word => chunk.Content?.ToLower().Contains(word) ?? false)
            })
            .Where(sc => sc.Score > 0)
            .OrderByDescending(sc => sc.Score)
            .Take(topK)
            .Select(sc => sc.Chunk)
            .ToList();

            return scoredChunks;
        }
    }
}
