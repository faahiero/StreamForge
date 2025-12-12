using FluentAssertions;
using NSubstitute;
using StreamForge.Application.Features.Videos.Commands.InitiateUpload;
using StreamForge.Application.Interfaces;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Interfaces;
using Xunit;

namespace StreamForge.Tests.Application;

public class InitiateUploadHandlerTests
{
    private readonly IVideoRepository _videoRepository;
    private readonly IStorageService _storageService;
    private readonly InitiateUploadHandler _handler;

    public InitiateUploadHandlerTests()
    {
        _videoRepository = Substitute.For<IVideoRepository>();
        _storageService = Substitute.For<IStorageService>();
        _handler = new InitiateUploadHandler(_videoRepository, _storageService);
    }

    [Fact]
    public async Task Handle_ShouldReturnResult_WhenCommandIsValid()
    {
        // Arrange
        var command = new InitiateUploadCommand("test.mp4", 1024);
        var expectedUrl = "https://s3.fake/url";
        
        _storageService
            .GeneratePresignedUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(expectedUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UploadUrl.Should().Be(expectedUrl);
        result.VideoId.Should().NotBeEmpty();

        // Verifica se o repositório foi chamado para salvar
        await _videoRepository.Received(1).AddAsync(Arg.Is<Video>(v => v.FileName == command.FileName));
    }
}
