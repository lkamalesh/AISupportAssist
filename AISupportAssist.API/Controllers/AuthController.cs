using AISupportAssist.API.Configuration;
using AISupportAssist.API.Interfaces;
using AISupportAssist.API.Models.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AISupportAssist.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuthService _authService;

        public AuthController(UserManager<IdentityUser> usermanager, IAuthService service)
        {
            _userManager = usermanager;
            _authService = service;
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto register) 
        {
            var user = new IdentityUser
            {
                UserName = register.Username,
                Email = register.Email
            };

            var result = await _userManager.CreateAsync(user, register.Password);

            if (!result.Succeeded)
                return StatusCode(500,result.Errors);

            await _userManager.AddToRoleAsync(user, "Customer");
            return Ok("User registered successfully");

        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody]LoginDto login)
        {
            var user = await _userManager.FindByEmailAsync(login.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user,login.Password))
            {
                return Unauthorized("Invalid username or password!");
            }

            var userRole = await _userManager.GetRolesAsync(user);
            var token = _authService.GenerateJwtToken(user, userRole);
            return Ok(new {Token = token});
        }

    }
}
