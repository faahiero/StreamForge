using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using StreamForge.Application.Interfaces; // Importar
using StreamForge.Worker.Models;
using StreamForge.Worker.Services;

namespace StreamForge.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly IVideoProcessor _videoProcessor;
    private readonly IDistributedLockService _lockService; // Injeter Lock Service
    private readonly string _queueUrl;

    public Worker(ILogger<Worker> logger, IAmazonSQS sqsClient, IConfiguration configuration, 
                  IVideoProcessor videoProcessor, IDistributedLockService lockService) // Adicionar no construtor
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _videoProcessor = videoProcessor;
        _lockService = lockService; // Inicializar
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
        // Gerar um token único para este worker para este lock
        var lockToken = Guid.NewGuid().ToString();
        string? objectKey = null; // Definir fora do try para acesso no finally

        try
        {
            _logger.LogInformation("📩 Processando mensagem: {MessageId}", message.MessageId);

            var s3Event = JsonSerializer.Deserialize<S3EventNotification>(message.Body);
            
            if (s3Event?.Records == null || s3Event.Records.Count == 0)
            {
                _logger.LogWarning("⚠️ Mensagem ignorada (formato inválido ou não é evento S3).");
                return;
            }

            // Assumimos um único record por mensagem S3
            var record = s3Event.Records[0];
            objectKey = record.S3?.Object?.Key; // Obter a chave para usar como lock

            if (string.IsNullOrEmpty(objectKey))
            {
                _logger.LogWarning("⚠️ Mensagem S3 sem chave de objeto. Ignorando.");
                return;
            }

            // Tentar adquirir o lock
            // Tempo de expiração do lock (ex: 5 minutos) - deve ser maior que o tempo de processamento
            var lockAcquired = await _lockService.TryAcquireLockAsync($"video-processing:{objectKey}", lockToken, TimeSpan.FromMinutes(5));

            if (!lockAcquired)
            {
                _logger.LogWarning("🔒 Não foi possível adquirir lock para {ObjectKey}. Já está sendo processado por outro worker (ou re-entrega). Mensagem será reprocessada.", objectKey);
                // Não apagar a mensagem, ela voltará para a fila após o timeout de visibilidade
                return;
            }

            await _videoProcessor.ProcessVideoAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao processar mensagem {MessageId}", message.MessageId);
        }
        finally
        {
            // Sempre tentar liberar o lock, se foi adquirido e se a chave não for nula
            if (objectKey != null)
            {
                await _lockService.ReleaseLockAsync($"video-processing:{objectKey}", lockToken);
            }
        }
    }
}
