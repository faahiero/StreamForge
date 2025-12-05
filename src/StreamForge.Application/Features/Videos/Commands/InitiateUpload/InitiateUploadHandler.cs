using MediatR;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;

namespace StreamForge.Application.Features.Videos.Commands.InitiateUpload;

public class InitiateUploadHandler : IRequestHandler<InitiateUploadCommand, InitiateUploadResult>
{
    private readonly IVideoRepository _videoRepository;
    private readonly IStorageService _storageService;

    public InitiateUploadHandler(IVideoRepository videoRepository, IStorageService storageService)
    {
        _videoRepository = videoRepository;
        _storageService = storageService;
    }

    public async Task<InitiateUploadResult> Handle(InitiateUploadCommand request, CancellationToken cancellationToken)
    {
        // 1. Criar Entidade
        var video = new Video(request.FileName, request.FileName);
        video.SetFileSize(request.FileSize);

        // 2. Persistir metadados iniciais (Status: Pending)
        await _videoRepository.AddAsync(video);

        // 3. Gerar URL de Upload (Válida por 1 hora)
        var expiration = TimeSpan.FromHours(1);
        var uploadUrl = await _storageService.GeneratePresignedUploadUrlAsync(video.S3Key, "video/mp4", expiration);

        // 4. Retornar resultado
        return new InitiateUploadResult(video.Id, uploadUrl, DateTime.UtcNow.Add(expiration));
    }
}
