using FluentValidation;

namespace StreamForge.Application.Features.Videos.Commands.InitiateUpload;

public class InitiateUploadCommandValidator : AbstractValidator<InitiateUploadCommand>
{
    public InitiateUploadCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("FileName is required")
            .Must(name => name.EndsWith(".mp4") || name.EndsWith(".mov") || name.EndsWith(".avi"))
            .WithMessage("Only .mp4, .mov, and .avi files are allowed");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("FileSize must be greater than 0")
            .LessThan(1024 * 1024 * 1024).WithMessage("FileSize must be less than 1GB");
    }
}
