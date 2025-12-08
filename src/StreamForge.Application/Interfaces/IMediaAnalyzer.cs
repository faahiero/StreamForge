namespace StreamForge.Application.Interfaces;

public record MediaMetadata(TimeSpan Duration, string Format, long Bitrate);

public interface IMediaAnalyzer
{
    Task<MediaMetadata> AnalyzeAsync(string filePath);
}
