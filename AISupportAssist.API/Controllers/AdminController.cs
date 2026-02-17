using AISupportAssist.API.Interfaces;
using AISupportAssist.API.Models.DTOs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AISupportAssist.API.Controllers
{
    [Authorize(Roles ="Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IFaqService _faqService;
        public AdminController(IFaqService service)
        {
            _faqService = service;
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var faq = await _faqService.GetByIdAsync(id);

            if (faq == null)
                return NotFound();

            return Ok(faq);
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var faqs = await _faqService.GetAllAsync();

            return Ok(faqs);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody]FaqDto faq)
        {
            await _faqService.AddAsync(faq);
            return CreatedAtAction(nameof(GetById), new { id = faq.Id }, faq);
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody]FaqDto faq)
        {
            await _faqService.UpdateAsync(faq);
            return NoContent();
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _faqService.DeleteAsync(id);
            return NoContent();
        }
    }
}
