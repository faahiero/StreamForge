using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // Importar
using StreamForge.Application.Interfaces;
using StreamForge.Infrastructure.Options; // Importar

namespace StreamForge.Infrastructure.Services;

public class SnsMessagePublisher : IMessagePublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly ILogger<SnsMessagePublisher> _logger;
    private readonly string _topicArnPrefix;

    public SnsMessagePublisher(IAmazonSimpleNotificationService snsClient, ILogger<SnsMessagePublisher> logger, IOptions<AwsSettings> options)
    {
        _snsClient = snsClient;
        _logger = logger;
        var region = options.Value.Region ?? "us-east-1";
        // Em LocalStack, accountId é 000000000000
        var accountId = "000000000000"; 
        _topicArnPrefix = $"arn:aws:sns:{region}:{accountId}:";
    }

    public async Task PublishAsync<T>(string topicName, T message)
    {
        var topicArn = $"{_topicArnPrefix}{topicName}";
        var messageBody = JsonSerializer.Serialize(message);

        try
        {
            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = messageBody
            };

            var response = await _snsClient.PublishAsync(request);
            _logger.LogInformation("📢 Evento publicado no SNS {Topic}: {MessageId}", topicName, response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Falha ao publicar evento no SNS {Topic}", topicName);
            throw;
        }
    }
}
