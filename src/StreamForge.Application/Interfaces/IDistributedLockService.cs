namespace StreamForge.Application.Interfaces;

public interface IDistributedLockService
{
    /// <summary>
    /// Tenta adquirir um lock distribuído para uma chave específica.
    /// </summary>
    /// <param name="key">A chave que representa o recurso a ser bloqueado.</param>
    /// <param name="token">Um token para identificar o proprietário do lock (geralmente um GUID).</param>
    /// <param name="expiry">O tempo de expiração do lock (para evitar deadlocks).</param>
    /// <returns>True se o lock foi adquirido com sucesso, False caso contrário.</returns>
    Task<bool> TryAcquireLockAsync(string key, string token, TimeSpan expiry);

    /// <summary>
    /// Libera um lock distribuído.
    /// </summary>
    /// <param name="key">A chave do lock.</param>
    /// <param name="token">O token do proprietário do lock.</param>
    /// <returns>True se o lock foi liberado com sucesso, False caso contrário.</returns>
    Task<bool> ReleaseLockAsync(string key, string token);
}
