using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using wave.web.Models;
using wave.web.Services;

namespace wave.web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentService _documentService;
        private readonly RagSearchService _ragService;

        public DocumentsController(DocumentService documentService, RagSearchService ragService)
        {
            _documentService = documentService;
            _ragService = ragService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Limit file size to 10MB
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest("File size exceeds 10MB limit");
            }

            using (var stream = file.OpenReadStream())
            {
                // 3. Process the file (Extract text and save metadata)
                var document = await _documentService.ProcessAndSaveDocument(file.FileName, stream);

                // 4. Generate Vector and Index content
                // Important: Ensure 'document.Content' holds the actual extracted text string.
                if (!string.IsNullOrWhiteSpace(document.Content))
                {
                    // This creates the vector and saves it to vectors.bin
                    _ragService.AddDocument(document.Id, document.Content);
                }

                return Ok(document);
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<Document>>> GetAll()
        {
            var documents = await _documentService.GetAllDocuments();
            return Ok(documents);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _documentService.DeleteDocument(id);
            if (!success)
            {
                return NotFound();
            }
            return Ok();
        }
    }
}
