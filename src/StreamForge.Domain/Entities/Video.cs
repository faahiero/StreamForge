using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Entities;

public class Video
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; }
    public string OriginalName { get; private set; }
    public long? FileSize { get; private set; }
    public ProcessingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string S3Key { get; private set; }
    
    // Metadados extraídos (opcional, preenchido pelo Worker)
    public TimeSpan? Duration { get; private set; }
    public string? Format { get; private set; }

    // Construtor privado para EF Core / DynamoDB
    private Video() 
    {
        FileName = null!;
        OriginalName = null!;
        S3Key = null!;
    }

    public Video(string fileName, string originalName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName cannot be empty", nameof(fileName));

        Id = Guid.NewGuid();
        FileName = fileName;
        OriginalName = originalName;
        Status = ProcessingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        // Definindo um padrão de caminho no S3: videos/{Guid}/{nome-limpo}
        S3Key = $"videos/{Id}/{fileName}";
    }

    public void SetFileSize(long size)
    {
        if (size <= 0) throw new ArgumentException("File size must be greater than zero");
        FileSize = size;
    }

    public void MarkAsProcessing()
    {
        if (Status != ProcessingStatus.Pending)
            throw new InvalidOperationException($"Cannot start processing video in status {Status}");

        Status = ProcessingStatus.Processing;
    }

    public void CompleteProcessing(TimeSpan duration, string format)
    {
        if (Status != ProcessingStatus.Processing)
            throw new InvalidOperationException($"Cannot complete video that is not processing. Current status: {Status}");

        Duration = duration;
        Format = format;
        Status = ProcessingStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void FailProcessing()
    {
        Status = ProcessingStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
    }
}
