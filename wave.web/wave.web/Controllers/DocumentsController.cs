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

        public DocumentsController(DocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            using (var stream = file.OpenReadStream())
            {
                var document = await _documentService.ProcessAndSaveDocument(file.FileName, stream);
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
