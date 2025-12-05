using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;

namespace StreamForge.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public Worker(ILogger<Worker> logger, IAmazonSQS sqsClient, IConfiguration configuration)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _queueUrl = configuration["AWS:QueueUrl"] ?? throw new ArgumentNullException("AWS:QueueUrl configuration is missing");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Worker iniciado. Aguardando mensagens na fila: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 5 // Long Polling
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                if (response.Messages != null && response.Messages.Count > 0)
                {
                    foreach (var message in response.Messages)
                    {
                        _logger.LogInformation("📩 Mensagem recebida: {Body}", message.Body);

                        // TODO: Processar o vídeo aqui (Download -> Extract -> Update DB)

                        // Apagar mensagem da fila (Acknowledge)
                        await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                        _logger.LogInformation("✅ Mensagem processada e removida da fila.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao processar mensagem SQS.");
                await Task.Delay(1000, stoppingToken); // Backoff simples em caso de erro
            }
        }
    }
}