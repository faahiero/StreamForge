using Amazon.SQS;
using Amazon.SQS.Model;

namespace StreamForge.Worker.Services;

public class QueueInitializer
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<QueueInitializer> _logger;
    private readonly string _queueUrl;

    public QueueInitializer(IAmazonSQS sqsClient, ILogger<QueueInitializer> logger, IConfiguration configuration)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _queueUrl = configuration["AWS:QueueUrl"] ?? "";
    }

    public async Task EnsureQueueExistsAsync()
    {
        if (string.IsNullOrEmpty(_queueUrl)) return;

        var queueName = _queueUrl.Split('/').Last();
        var dlqName = $"{queueName}-dlq";

        try
        {
            // 1. Garantir DLQ
            string dlqArn;
            try
            {
                var dlqUrlResponse = await _sqsClient.GetQueueUrlAsync(dlqName);
                var dlqAttrs = await _sqsClient.GetQueueAttributesAsync(dlqUrlResponse.QueueUrl, new List<string> { "QueueArn" });
                dlqArn = dlqAttrs.Attributes["QueueArn"];
                _logger.LogInformation("✅ DLQ {DlqName} já existe.", dlqName);
            }
            catch (QueueDoesNotExistException)
            {
                _logger.LogWarning("⚠️ DLQ não encontrada. Criando: {DlqName}...", dlqName);
                var createDlqResponse = await _sqsClient.CreateQueueAsync(dlqName);
                var dlqAttrs = await _sqsClient.GetQueueAttributesAsync(createDlqResponse.QueueUrl, new List<string> { "QueueArn" });
                dlqArn = dlqAttrs.Attributes["QueueArn"];
                _logger.LogInformation("✅ DLQ criada com sucesso.");
            }

            // 2. Garantir Fila Principal com Redrive Policy
            try
            {
                await _sqsClient.GetQueueUrlAsync(queueName);
                _logger.LogInformation("✅ Fila Principal {QueueName} já existe.", queueName);
                // Nota: Em produção, deveríamos verificar se a RedrivePolicy está correta e atualizar se necessário (SetQueueAttributes)
            }
            catch (QueueDoesNotExistException)
            {
                _logger.LogWarning("⚠️ Fila Principal não encontrada. Criando: {QueueName} com DLQ...", queueName);
                
                var attributes = new Dictionary<string, string>
                {
                    {
                        "RedrivePolicy", 
                        System.Text.Json.JsonSerializer.Serialize(new 
                        {
                            deadLetterTargetArn = dlqArn,
                            maxReceiveCount = 3 // Tenta 3 vezes antes de mover pra DLQ
                        })
                    }
                };

                await _sqsClient.CreateQueueAsync(new CreateQueueRequest
                {
                    QueueName = queueName,
                    Attributes = attributes
                });
                
                _logger.LogInformation("✅ Fila Principal criada e vinculada à DLQ.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao inicializar filas SQS.");
        }
    }
}
