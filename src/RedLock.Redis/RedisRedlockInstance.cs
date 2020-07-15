using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace RedLock.Redis
{
    /// <summary>
    /// Redis instance for distributed lock
    /// </summary>
    public class RedisRedlockInstance : IRedlockInstance
    {
        private readonly Func<IDatabase> _selectDb;

        // ReSharper disable once ConvertToConstant.Local
        private static readonly string UnlockLua = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
	return redis.call('del', KEYS[1])
else
	return 0
end
";
        
        /// <summary>
        /// Redis instance for distributed lock
        /// </summary>
        public RedisRedlockInstance(Func<IDatabase> selectDb)
        {
            _selectDb = selectDb;
        }


        /// <inheritdoc />
        public bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            var key = Key(resource);
            return _selectDb().StringSet(key, nonce, lockTimeToLive, When.NotExists, CommandFlags.DemandMaster);
        }

        /// <inheritdoc />
        public Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            var key = Key(resource);
            return _selectDb().StringSetAsync(key, nonce, lockTimeToLive, When.NotExists, CommandFlags.DemandMaster);
        }

        /// <inheritdoc />
        public void Unlock(string resource, string nonce)
        {
            var key = Key(resource);
            var _ = (bool) _selectDb()
                .ScriptEvaluate(UnlockLua, new RedisKey[] {key}, new RedisValue[] {nonce}, CommandFlags.DemandMaster);
        }

        /// <inheritdoc />
        public async Task UnlockAsync(string resource, string nonce)
        {
            var key = Key(resource);
            var _ = (bool) await _selectDb()
                .ScriptEvaluateAsync(UnlockLua, new RedisKey[] {key}, new RedisValue[] {nonce}, CommandFlags.DemandMaster)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Build key for redis from resource name
        /// </summary>
        protected virtual string Key(string resource)
        {
            return resource;
        }
    }
}