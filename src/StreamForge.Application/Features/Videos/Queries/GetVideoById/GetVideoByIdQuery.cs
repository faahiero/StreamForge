using MediatR;

namespace StreamForge.Application.Features.Videos.Queries.GetVideoById;

public record GetVideoByIdQuery(Guid Id) : IRequest<VideoDto?>;
