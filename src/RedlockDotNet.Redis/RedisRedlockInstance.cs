using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace RedlockDotNet.Redis
{
    /// <summary>
    /// Redis instance for distributed lock
    /// </summary>
    public sealed class RedisRedlockInstance : IRedlockInstance
    {
        internal readonly Func<IDatabase> SelectDb;
        private readonly Func<string, string> _createKeyFromResourceName;
        private readonly string _name;
        private readonly ILogger _logger;

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
        public RedisRedlockInstance(
            Func<IDatabase> selectDb,
            Func<string, string> createKeyFromResourceName, 
            string name,
            ILogger logger
        )
        {
            SelectDb = selectDb;
            _createKeyFromResourceName = createKeyFromResourceName;
            _name = name;
            _logger = logger;
        }


        /// <inheritdoc />
        public bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            var key = Key(resource);
            _logger.TryLock(resource, nonce, _name, lockTimeToLive, key);
            return SelectDb().StringSet(key, nonce, lockTimeToLive, When.NotExists, CommandFlags.DemandMaster);
        }

        /// <inheritdoc />
        public Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            var key = Key(resource);
            _logger.TryLock(resource, nonce, _name, lockTimeToLive, key);
            return SelectDb().StringSetAsync(key, nonce, lockTimeToLive, When.NotExists, CommandFlags.DemandMaster);
        }

        /// <inheritdoc />
        public void Unlock(string resource, string nonce)
        {
            var key = Key(resource);
            _logger.Unlocking(resource, nonce, _name, key);
            var res = (bool) SelectDb()
                .ScriptEvaluate(UnlockLua, new RedisKey[] {key}, new RedisValue[] {nonce}, CommandFlags.DemandMaster);
            _logger.Unlocked(resource, nonce, _name, key, res);
        }

        /// <inheritdoc />
        public async Task UnlockAsync(string resource, string nonce)
        {
            var key = Key(resource);
            _logger.Unlocking(resource, nonce, _name, key);
            var res = (bool) await SelectDb()
                .ScriptEvaluateAsync(UnlockLua, new RedisKey[] {key}, new RedisValue[] {nonce}, CommandFlags.DemandMaster)
                .ConfigureAwait(false);
            _logger.Unlocked(resource, nonce, _name, key, res);
        }

        /// <summary>
        /// Build key for redis from resource name
        /// </summary>
        private string Key(string resource) => _createKeyFromResourceName(resource);

        /// <inheritdoc />
        public override string ToString() => _name;

        /// <summary>
        /// Create <see cref="RedisRedlockInstance"/> from <see cref="ConnectionMultiplexer"/>
        /// </summary>
        /// <param name="con"></param>
        /// <param name="createKeyFromResourceName"></param>
        /// <param name="database">Number of the database where the locks will be stored</param>
        /// <param name="name">Instance name for logs and ToString</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IRedlockInstance Create(
            IConnectionMultiplexer con,
            Func<string, string> createKeyFromResourceName,
            int database,
            string name,
            ILogger logger
        ) => new RedisRedlockInstance(() => con.GetDatabase(database), createKeyFromResourceName, name, logger);

        /// <summary>
        /// Create <see cref="RedisRedlockInstance"/> from <see cref="ConnectionMultiplexer"/>
        /// </summary>
        /// <remarks>
        /// Name of instance sets to first connection endpoint ToString
        /// </remarks>
        /// <param name="con"></param>
        /// <param name="createKeyFromResourceName"></param>
        /// <param name="database">Number of the database where the locks will be stored</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IRedlockInstance Create(
            IConnectionMultiplexer con,
            Func<string, string> createKeyFromResourceName,
            int database, 
            ILogger logger
        ) => Create(con, createKeyFromResourceName, database, GetName(con), logger);

        /// <summary>
        /// Create <see cref="RedisRedlockInstance"/> from <see cref="ConnectionMultiplexer"/>
        /// </summary>
        /// <remarks>
        /// Database sets to default (<see cref="ConnectionMultiplexer.GetDatabase"/>)
        /// </remarks>
        /// <param name="con"></param>
        /// <param name="createKeyFromResourceName"></param>
        /// <param name="name">Instance name for logs and ToString</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IRedlockInstance Create(
            IConnectionMultiplexer con,
            Func<string, string> createKeyFromResourceName,
            string name,
            ILogger logger
        ) => new RedisRedlockInstance(() => con.GetDatabase(), createKeyFromResourceName, name, logger);

        /// <summary>
        /// Create <see cref="RedisRedlockInstance"/> from <see cref="ConnectionMultiplexer"/>
        /// </summary>
        /// <remarks>
        /// Name of instance sets to first connection endpoint ToString
        /// </remarks>
        /// <remarks>
        /// Database sets to default (<see cref="ConnectionMultiplexer.GetDatabase"/>)
        /// </remarks>
        /// <param name="con"></param>
        /// <param name="createKeyFromResourceName"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IRedlockInstance Create(
            IConnectionMultiplexer con,
            Func<string, string> createKeyFromResourceName,
            ILogger logger
        ) => Create(con, createKeyFromResourceName, GetName(con), logger);
        
        private static string GetName(IConnectionMultiplexer con) => con.GetEndPoints().FirstOrDefault()?.ToString() ?? "NO_ENDPOINT";
    }
}