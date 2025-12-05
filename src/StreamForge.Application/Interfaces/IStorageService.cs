namespace StreamForge.Application.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Gera uma URL pré-assinada (Pre-signed URL) para upload direto no S3.
    /// </summary>
    /// <param name="key">O caminho/nome do arquivo no bucket (ex: videos/id/arquivo.mp4)</param>
    /// <param name="contentType">O tipo MIME do arquivo (ex: video/mp4)</param>
    /// <param name="expiration">Tempo de expiração da URL</param>
    /// <returns>A URL completa para o PUT</returns>
    Task<string> GeneratePresignedUploadUrlAsync(string key, string contentType, TimeSpan expiration);
}
