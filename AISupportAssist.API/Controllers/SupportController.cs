using AISupportAssist.API.Interfaces;
using AISupportAssist.API.Models.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AISupportAssist.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SupportController : ControllerBase
    {
        private readonly ISupportService _supportService;

        public SupportController(ISupportService service)
        {
            _supportService = service;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] SupportRequestDto request)
        {
            var response = await _supportService.HandleQuestionsAsync(request.Question);
            return Ok(response);
        }
    }
}
