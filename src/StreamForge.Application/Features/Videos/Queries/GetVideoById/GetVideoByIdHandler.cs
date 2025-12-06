using MediatR;
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

        if (video == null)
            return null;

        return new VideoDto(
            video.Id,
            video.FileName,
            video.OriginalName,
            video.FileSize,
            video.Status,
            video.CreatedAt,
            video.ProcessedAt,
            video.Duration,
            video.Format
        );
    }
}
