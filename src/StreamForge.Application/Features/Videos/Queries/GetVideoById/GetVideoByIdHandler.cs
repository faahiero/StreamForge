using Mapster;
using MediatR;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Features.Videos.Queries.GetVideoById;

public class GetVideoByIdHandler : IRequestHandler<GetVideoByIdQuery, VideoDto?>
{
    private readonly IVideoRepository _videoRepository;

    public GetVideoByIdHandler(IVideoRepository videoRepository)
    {
        _videoRepository = videoRepository;
    }

    public async Task<VideoDto?> Handle(GetVideoByIdQuery request, CancellationToken cancellationToken)
    {
        var video = await _videoRepository.GetByIdAsync(request.Id);
        return video?.Adapt<VideoDto>();
    }
}
