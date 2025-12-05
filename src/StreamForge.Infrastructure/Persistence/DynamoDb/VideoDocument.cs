using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Persistence.DynamoDb;

[DynamoDBTable("Video")]
public class VideoDocument
{
    [DynamoDBHashKey]
    public Guid Id { get; set; }
    public string? FileName { get; set; }
    public string? OriginalName { get; set; }
    public long? FileSize { get; set; }
    
    [DynamoDBProperty(Converter = typeof(ProcessingStatusConverter))]
    public ProcessingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? S3Key { get; set; }
    
    [DynamoDBProperty(Converter = typeof(DynamoDBTimeSpanConverter))]
    public TimeSpan? Duration { get; set; }
    public string? Format { get; set; }
}

public class ProcessingStatusConverter : IPropertyConverter
{
    public DynamoDBEntry ToEntry(object value)
    {
        ProcessingStatus status = (ProcessingStatus)value;
        return new Primitive(status.ToString());
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        return Enum.Parse(typeof(ProcessingStatus), entry.AsString());
    }
}

public class DynamoDBTimeSpanConverter : IPropertyConverter
{
    public DynamoDBEntry ToEntry(object value)
    {
        var timeSpan = value as TimeSpan?;
        if (timeSpan.HasValue)
        {
            // Armazena como Número (Ticks)
            return new Primitive(timeSpan.Value.Ticks.ToString(), true);
        }
        return new DynamoDBNull();
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is DynamoDBNull) return null;
        return TimeSpan.FromTicks(long.Parse(entry.AsPrimitive().Value as string));
    }
}
