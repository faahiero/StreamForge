using Microsoft.AspNetCore.Mvc;
using StreamForge.API.Services;

namespace StreamForge.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Mock de validação: Aceita qualquer coisa não vazia
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Invalid credentials");

        var token = _tokenService.GenerateToken(request.Username);
        return Ok(new { Token = token });
    }
}

public record LoginRequest(string Username, string Password);
