using System;
using System.Collections.Generic;

namespace wave.web.Models
{
    public class Document
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public long FileSize { get; set; }
        public List<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }

    public class DocumentChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? DocumentId { get; set; }
        public string? Content { get; set; }
        public int ChunkIndex { get; set; }
        public List<float>? Embedding { get; set; }
    }
}
