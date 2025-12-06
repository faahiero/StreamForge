using MediatR;
using StreamForge.Domain.Enums;

namespace StreamForge.Application.Features.Videos.Queries.GetVideoById;

public record GetVideoByIdQuery(Guid Id) : IRequest<VideoDto?>;

public record VideoDto(
    Guid Id,
    string FileName,
    string OriginalName,
    long? FileSize,
    ProcessingStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    TimeSpan? Duration,
    string? Format
);
