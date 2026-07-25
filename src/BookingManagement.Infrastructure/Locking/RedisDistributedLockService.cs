using BookingManagement.Application.Abstractions;
using StackExchange.Redis;

namespace BookingManagement.Infrastructure.Locking;

/// <summary>
/// Redis-backed distributed lock using SET key value NX PX (atomic acquire).
/// Release uses a Lua script that only deletes the key if the value still
/// matches our token, so we never release a lock acquired by someone else
/// after ours expired (classic Redlock single-instance safety property).
/// </summary>
public class RedisDistributedLockService : IDistributedLockService
{
    private const string ReleaseScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end
        """;

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisDistributedLockService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<IAsyncDisposable?> AcquireAsync(string key, TimeSpan expiry, TimeSpan wait, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        var redisKey = $"lock:{key}";

        var deadline = DateTime.UtcNow.Add(wait);
        var pollDelay = TimeSpan.FromMilliseconds(100);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var acquired = await db.StringSetAsync(redisKey, token, expiry, When.NotExists);
            if (acquired)
            {
                return new RedisLockHandle(db, redisKey, token);
            }

            await Task.Delay(pollDelay, cancellationToken);
        }

        return null;
    }

    private sealed class RedisLockHandle : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;

        public RedisLockHandle(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            await _db.ScriptEvaluateAsync(ReleaseScript, new RedisKey[] { _key }, new RedisValue[] { _token });
        }
    }
}
