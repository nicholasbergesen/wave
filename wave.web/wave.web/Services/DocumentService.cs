using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using wave.web.Models;

namespace wave.web.Services
{
    public class DocumentService
    {
        private readonly string _dataFolder;
        private const int ChunkSize = 500;

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
            var sanitizedFileName = Path.GetFileName(fileName);

            var document = new Document
            {
                FileName = sanitizedFileName,
                FilePath = Path.Combine(_dataFolder, $"{Guid.NewGuid()}_{sanitizedFileName}")
            };

            using (var fileStreamWriter = new FileStream(document.FilePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamWriter);
            }

            var fileInfo = new FileInfo(document.FilePath);
            document.FileSize = fileInfo.Length;

            var text = await ExtractTextFromFile(document.FilePath);
            document.Content = text;
            document.Chunks = ChunkText(text, document.Id);
            await SaveDocumentMetadata(document);

            return document;
        }

        private async Task<string> ExtractTextFromFile(string filePath)
        {
            return await File.ReadAllTextAsync(filePath);
        }

        private List<DocumentChunk> ChunkText(string text, string documentId)
        {
            var chunks = new List<DocumentChunk>();
            if (string.IsNullOrEmpty(text)) return chunks;

            string normalized = Regex.Replace(text, @"\r\n|\n\r|\n|\r", "\r\n");
            var chunkIndex = 0;

            while (chunkIndex < normalized.Length)
            {
                var length = Math.Min(ChunkSize, normalized.Length - chunkIndex);
                var chunkContent = normalized.Substring(chunkIndex, length);

                chunkContent = chunkContent.ReplaceLineEndings();
                if (chunkContent.Contains(' '))
                    chunkContent = string.Join(' ', chunkContent.Split(' ')[0..^1]);

                var trim = chunkContent.Trim();
                if (!string.IsNullOrWhiteSpace(trim))
                {
                    chunks.Add(new DocumentChunk
                    {
                        DocumentId = documentId,
                        Content = trim,
                        ChunkIndex = chunkIndex
                    });
                }

                chunkIndex += Math.Max(1, chunkContent.Length); // Prevent infinite loops
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
            if (!Directory.Exists(_dataFolder)) return documents;

            var metadataFiles = Directory.GetFiles(_dataFolder, "*_metadata.json");

            var documentTasks = metadataFiles.Select(async file =>
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    return JsonSerializer.Deserialize<Document>(json);
                }
                catch
                {
                    return null;
                }
            });

            var loadedDocuments = await Task.WhenAll(documentTasks);
            documents.AddRange(loadedDocuments.Where(d => d != null)!);
            return documents;
        }

        public async Task<bool> DeleteDocument(string documentId)
        {
            var documents = await GetAllDocuments();
            var document = documents.FirstOrDefault(d => d.Id == documentId);

            if (document == null) return false;

            if (File.Exists(document.FilePath)) File.Delete(document.FilePath);

            var metadataPath = Path.Combine(_dataFolder, $"{document.Id}_metadata.json");
            if (File.Exists(metadataPath)) File.Delete(metadataPath);

            return true;
        }
    }
}