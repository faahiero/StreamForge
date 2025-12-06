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

        // Extrai o nome da fila da URL (último segmento)
        var queueName = _queueUrl.Split('/').Last();

        try
        {
            _logger.LogInformation("🔍 Verificando existência da fila SQS: {QueueName}", queueName);
            await _sqsClient.GetQueueUrlAsync(queueName);
            _logger.LogInformation("✅ Fila SQS já existe.");
        }
        catch (QueueDoesNotExistException)
        {
            _logger.LogWarning("⚠️ Fila SQS não encontrada. Criando: {QueueName}...", queueName);
            await _sqsClient.CreateQueueAsync(queueName);
            _logger.LogInformation("✅ Fila SQS criada com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao verificar/criar fila SQS.");
        }
    }
}
