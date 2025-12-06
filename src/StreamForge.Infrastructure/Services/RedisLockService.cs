using StackExchange.Redis;
using StreamForge.Application.Interfaces;
using Microsoft.Extensions.Logging; // Adicionar este using

namespace StreamForge.Infrastructure.Services;

public class RedisLockService : IDistributedLockService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisLockService> _logger;

    public RedisLockService(IConnectionMultiplexer redis, ILogger<RedisLockService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<bool> TryAcquireLockAsync(string key, string token, TimeSpan expiry)
    {
        // SET key token EX expiry NX
        // NX = Only set the key if it does not already exist.
        var acquired = await _database.StringSetAsync(key, token, expiry, When.NotExists);
        if (acquired)
        {
            _logger.LogDebug("🔒 Lock adquirido para {Key} com token {Token}", key, token);
        }
        else
        {
            _logger.LogDebug("⛔ Não foi possível adquirir lock para {Key}, já em uso.", key);
        }
        return acquired;
    }

    public async Task<bool> ReleaseLockAsync(string key, string token)
    {
        // Script Lua para garantir atomicidade: só apaga se o token for o mesmo
        string script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

        var result = (long)await _database.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { token });
        if (result > 0)
        {
            _logger.LogDebug("🔓 Lock liberado para {Key} com token {Token}", key, token);
        }
        else
        {
            _logger.LogWarning("⚠️ Não foi possível liberar lock para {Key}. Token não corresponde ou lock expirou.", key);
        }
        return result > 0;
    }
}