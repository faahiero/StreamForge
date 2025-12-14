using FluentAssertions;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Domain.Exceptions; // Importar Exceções
using Xunit;

namespace StreamForge.Tests.Domain;

public class VideoTests
{
    [Fact]
    public void Constructor_ShouldCreateVideo_WhenParametersAreValid()
    {
        // Arrange
        var fileName = "test.mp4";
        var originalName = "original.mp4";

        // Act
        var video = new Video(fileName, originalName);

        // Assert
        video.Should().NotBeNull();
        video.Status.Should().Be(ProcessingStatus.Pending);
        video.S3Key.Should().NotBeNullOrEmpty();
        video.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenFileNameIsEmpty()
    {
        // Act
        Action act = () => new Video("", "original.mp4");

        // Assert
        act.Should().Throw<ValidationDomainException>() // Atualizado
            .WithMessage("*FileName cannot be empty*");
    }

    [Fact]
    public void MarkAsProcessing_ShouldUpdateStatus_WhenStatusIsPending()
    {
        // Arrange
        var video = new Video("test.mp4", "original.mp4");

        // Act
        video.MarkAsProcessing();

        // Assert
        video.Status.Should().Be(ProcessingStatus.Processing);
    }

    [Fact]
    public void MarkAsProcessing_ShouldThrow_WhenStatusIsAlreadyCompleted()
    {
        // Arrange
        var video = new Video("test.mp4", "original.mp4");
        video.MarkAsProcessing();
        video.CompleteProcessing(TimeSpan.FromSeconds(10), "mp4");

        // Act
        Action act = () => video.MarkAsProcessing();

        // Assert
        act.Should().Throw<ValidationDomainException>(); // Atualizado
    }
}
