namespace StreamForge.Application.Features.Videos.Commands.InitiateUpload;

public record InitiateUploadResult(Guid VideoId, string UploadUrl, DateTime ExpiresAt);
