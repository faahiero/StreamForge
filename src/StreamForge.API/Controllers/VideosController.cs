using MediatR;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.Features.Videos.Commands.InitiateUpload;

namespace StreamForge.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IMediator _mediator;

    public VideosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("init")]
    [ProducesResponseType(typeof(InitiateUploadResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiateUpload([FromBody] InitiateUploadCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
