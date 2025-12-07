using MediatR;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.Features.Auth.Commands.LoginUser;
using StreamForge.Application.Features.Auth.Commands.RegisterUser;

namespace StreamForge.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var userId = await _mediator.Send(command);
        return CreatedAtAction(nameof(Register), new { id = userId }, new { UserId = userId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        try
        {
            var token = await _mediator.Send(command);
            return Ok(new { Token = token });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
