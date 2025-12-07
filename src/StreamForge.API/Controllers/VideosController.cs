using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamForge.Application.Features.Videos.Commands.InitiateUpload;
using StreamForge.Application.Features.Videos.Queries.GetVideoById;

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
    [Authorize] // Requer token JWT válido
    [ProducesResponseType(typeof(InitiateUploadResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiateUpload([FromBody] InitiateUploadCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVideoById(Guid id)
    {
        var result = await _mediator.Send(new GetVideoByIdQuery(id));
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}