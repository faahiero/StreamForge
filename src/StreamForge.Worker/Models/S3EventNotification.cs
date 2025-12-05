using System.Text.Json.Serialization;

namespace StreamForge.Worker.Models;

public class S3EventNotification
{
    [JsonPropertyName("Records")]
    public List<S3EventNotificationRecord>? Records { get; set; }
}

public class S3EventNotificationRecord
{
    [JsonPropertyName("s3")]
    public S3Entity? S3 { get; set; }
}

public class S3Entity
{
    [JsonPropertyName("bucket")]
    public S3Bucket? Bucket { get; set; }

    [JsonPropertyName("object")]
    public S3Object? Object { get; set; }
}

public class S3Bucket
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class S3Object
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
}
