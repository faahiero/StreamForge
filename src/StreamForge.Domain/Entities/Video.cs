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

    // Construtor sem parâmetros (Necessário para frameworks de mapeamento)
    public Video() 
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
        S3Key = $"videos/{Id}/{fileName}";
    }

    public void SetFileSize(long size)
    {
        if (size <= 0) throw new ArgumentException("File size must be greater than zero");
        FileSize = size;
    }

    public void MarkAsProcessing()
    {
        if (Status != ProcessingStatus.Pending && Status != ProcessingStatus.Failed)
            throw new InvalidOperationException($"Cannot start processing video in status {Status}. Only Pending or Failed videos can be processed.");

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

    // Métodos auxiliares para mapeamento (opcional, se usarmos construtores ou setters públicos)
    public void SetId(Guid id) => Id = id;
    public void SetStatus(ProcessingStatus status) => Status = status;
    public void SetCreatedAt(DateTime createdAt) => CreatedAt = createdAt;
    public void SetProcessedAt(DateTime? processedAt) => ProcessedAt = processedAt;
    public void SetS3Key(string s3Key) => S3Key = s3Key;
    public void SetDuration(TimeSpan? duration) => Duration = duration;
    public void SetFormat(string? format) => Format = format;
    public void SetFileName(string fileName) => FileName = fileName;
    public void SetOriginalName(string originalName) => OriginalName = originalName;
}