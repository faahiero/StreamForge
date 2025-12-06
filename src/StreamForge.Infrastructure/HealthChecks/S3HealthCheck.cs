using Amazon.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace StreamForge.Infrastructure.HealthChecks;

public class S3HealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;

    public S3HealthCheck(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.ListBucketsAsync(cancellationToken);
            return HealthCheckResult.Healthy("S3 está acessível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("S3 inacessível.", ex);
        }
    }
}
