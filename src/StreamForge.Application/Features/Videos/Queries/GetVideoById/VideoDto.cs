using StreamForge.Domain.Enums;

namespace StreamForge.Application.Features.Videos.Queries.GetVideoById;

public record VideoDto(
    Guid Id, 
    string FileName, 
    string OriginalName, 
    long? FileSize, 
    string Status, // Enum convertido para string
    DateTime CreatedAt, 
    DateTime? ProcessedAt, 
    TimeSpan? Duration, 
    string? Format
);
