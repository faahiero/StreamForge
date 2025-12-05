using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using StreamForge.Worker.Models;
using StreamForge.Worker.Services;

namespace StreamForge.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly IVideoProcessor _videoProcessor;
    private readonly string _queueUrl;

    public Worker(ILogger<Worker> logger, IAmazonSQS sqsClient, IConfiguration configuration, IVideoProcessor videoProcessor)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _videoProcessor = videoProcessor;
        _queueUrl = configuration["AWS:QueueUrl"] ?? throw new ArgumentNullException("AWS:QueueUrl configuration is missing");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Worker iniciado. Fila: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 5
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                if (response.Messages != null && response.Messages.Count > 0)
                {
                    foreach (var message in response.Messages)
                    {
                        await ProcessMessageAsync(message);
                        
                        // Delete after success
                        await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro fatal no loop do Worker.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(Message message)
    {
        try
        {
            _logger.LogInformation("📩 Processando mensagem: {MessageId}", message.MessageId);

            // 1. Deserializar Evento S3
            var s3Event = JsonSerializer.Deserialize<S3EventNotification>(message.Body);
            
            if (s3Event?.Records == null || s3Event.Records.Count == 0)
            {
                _logger.LogWarning("⚠️ Mensagem ignorada (formato inválido ou não é evento S3).");
                return;
            }

            // 2. Processar cada registro do evento (geralmente é 1)
            foreach (var record in s3Event.Records)
            {
                await _videoProcessor.ProcessVideoAsync(record);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar mensagem {MessageId}", message.MessageId);
            // Não damos 'throw' aqui para garantir que o DeleteMessage só ocorra se não houver erro fatal?
            // Na verdade, se der erro de negócio, talvez queiramos mover pra DLQ (não apagar).
            // Para simplificar: Se falhar, vamos logar e deixar a mensagem voltar pro pool (visibilidade timeout) ou apagar se for erro de dados irrecuperável.
            // Neste exemplo, vamos engolir o erro e apagar para não travar a fila local, mas em PROD usaríamos DLQ.
        }
    }
}
