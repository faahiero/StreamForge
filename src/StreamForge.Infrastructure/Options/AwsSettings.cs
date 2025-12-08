namespace StreamForge.Infrastructure.Options;

public class AwsSettings
{
    public const string SectionName = "AWS";

    public string? Region { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? ServiceURL { get; set; }
    public string? BucketName { get; set; }
    public string? QueueUrl { get; set; }
}
