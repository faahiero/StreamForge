using MediatR;

namespace StreamForge.Application.Features.Videos.Commands.InitiateUpload;

public record InitiateUploadCommand(string FileName, long FileSize) : IRequest<InitiateUploadResult>;
