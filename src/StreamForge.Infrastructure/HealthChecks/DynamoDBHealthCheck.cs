using Amazon.DynamoDBv2;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace StreamForge.Infrastructure.HealthChecks;

public class DynamoDBHealthCheck : IHealthCheck
{
    private readonly IAmazonDynamoDB _dynamoClient;

    public DynamoDBHealthCheck(IAmazonDynamoDB dynamoClient)
    {
        _dynamoClient = dynamoClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dynamoClient.ListTablesAsync(cancellationToken);
            return HealthCheckResult.Healthy("DynamoDB está acessível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("DynamoDB inacessível.", ex);
        }
    }
}
