using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StreamForge.Application.Interfaces;

namespace StreamForge.Infrastructure.Services;

public class SnsMessagePublisher : IMessagePublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly ILogger<SnsMessagePublisher> _logger;
    private readonly string _topicArnPrefix;

    public SnsMessagePublisher(IAmazonSimpleNotificationService snsClient, ILogger<SnsMessagePublisher> logger, IConfiguration configuration)
    {
        _snsClient = snsClient;
        _logger = logger;
        // Em LocalStack o ARN é previsível, mas em PROD viria do config.
        // Vamos assumir que passamos o nome do tópico e ele resolve o ARN ou usa um prefixo configurado.
        // Para simplificar, vou construir o ARN padrão do LocalStack ou pegar do config se houver.
        var accountId = configuration["AWS:AccountId"] ?? "000000000000";
        var region = configuration["AWS:Region"] ?? "us-east-1";
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
