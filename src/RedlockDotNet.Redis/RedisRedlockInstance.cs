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
        private readonly float _clockDriftFactor;

        /// <summary>
        /// Redis expire error is from 0 to 1 milliseconds. (https://redis.io/commands/expire#expire-accuracy)
        /// </summary>
        private static readonly TimeSpan RedisResolution = TimeSpan.FromMilliseconds(1);
        
        /// <summary>
        /// We cast <see cref="TimeSpan.TotalMilliseconds"/> from double to long then 10.99 = 10
        /// </summary>
        private static readonly TimeSpan SharpTimeSpanCastMaxError = TimeSpan.FromMilliseconds(1);
        
        private static readonly TimeSpan ConstDrift = RedisResolution + SharpTimeSpanCastMaxError;

        // ReSharper disable once ConvertToConstant.Local
        private static readonly string UnlockLua = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
  return redis.call('del', KEYS[1])
else
  return 0
end
";
        
        // ReSharper disable once ConvertToConstant.Local
        /// <summary>
        /// KEYS[1] is locking resource redis key
        /// ARGV[1] is nonce
        /// ARGV[2] is lock time to live in milliseconds
        /// ARGV[3] is (tryReacquire ? 1 : 0)
        /// result is <see cref="ExtendResult"/>
        /// </summary>
        private static readonly string ExtendLua = @"
local currentVal = redis.call('get', KEYS[1])
if (currentVal == false) then
  if(ARGV[3] == ""1"") then  
    return redis.call('set', KEYS[1], ARGV[1], 'PX', ARGV[2]) and 2 or 0
  else
    return -2
  end
elseif (currentVal == ARGV[1]) then
  redis.call('pexpire', KEYS[1], ARGV[2])
  return 1
else
  return -1
end
";

        
        /// <summary>
        /// Redis instance for distributed lock
        /// </summary>
        public RedisRedlockInstance(
            Func<IDatabase> selectDb,
            Func<string, string> createKeyFromResourceName, 
            string name,
            float clockDriftFactor,
            ILogger logger
        )
        {
            SelectDb = selectDb;
            _createKeyFromResourceName = createKeyFromResourceName;
            _name = name;
            _clockDriftFactor = clockDriftFactor;
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

        /// <inheritdoc />
        public ExtendResult TryExtend(string resource, string nonce, TimeSpan lockTimeToLive, bool tryReacquire)
        {
            var key = Key(resource);
            _logger.TryExtendLock(resource, nonce, _name, lockTimeToLive, tryReacquire, key);
            var scriptEvaluateResult = SelectDb()
                .ScriptEvaluate(ExtendLua, new RedisKey[] {key}, new RedisValue[]
                {
                    nonce,
                    (long) lockTimeToLive.TotalMilliseconds,
                    tryReacquire ? 1 : 0
                });
            _logger.ExtendScriptExecuted(resource, nonce, _name, lockTimeToLive, key, scriptEvaluateResult);
            return (ExtendResult) (int) scriptEvaluateResult;
        }

        /// <inheritdoc />
        public async Task<ExtendResult> TryExtendAsync(string resource, string nonce, TimeSpan lockTimeToLive, bool tryReacquire)
        {
            var key = Key(resource);
            _logger.TryExtendLock(resource, nonce, _name, lockTimeToLive, tryReacquire, key);
            var scriptEvaluateResult = await SelectDb()
                .ScriptEvaluateAsync(ExtendLua, new RedisKey[] {key}, new RedisValue[]
                {
                    nonce,
                    (long) lockTimeToLive.TotalMilliseconds,
                    tryReacquire ? 1 : 0
                });
            _logger.ExtendScriptExecuted(resource, nonce, _name, lockTimeToLive, key, scriptEvaluateResult);
            return (ExtendResult) (int) scriptEvaluateResult;
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
        /// <param name="clockDriftFactor">Drift factor for system clock (multiply with ttl of lock)</param>
        /// <returns></returns>
        public static IRedlockInstance Create(
            IConnectionMultiplexer con,
            Func<string, string> createKeyFromResourceName,
            int database,
            string name,
            float clockDriftFactor,
            ILogger logger
        ) => new RedisRedlockInstance(() => con.GetDatabase(database), createKeyFromResourceName, name, clockDriftFactor, logger);
        
        /// <summary>
        /// Create <see cref="RedisRedlockInstance"/> from <see cref="ConnectionMultiplexer"/>
        /// </summary>
        /// <param name="con"></param>
        /// <param name="createKeyFromResourceName"></param>
        /// <param name="database">Number of the database where the locks will be stored</param>
        /// <param name="logger"></param>
        /// <param name="clockDriftFactor">Drift factor for system clock (multiply with ttl of lock)</param>
        /// <returns></returns>
        public static IRedlockInstance Create(
            IConnectionMultiplexer con,
            Func<string, string> createKeyFromResourceName,
            int database,
            float clockDriftFactor,
            ILogger logger
        ) => new RedisRedlockInstance(() => con.GetDatabase(database), createKeyFromResourceName, GetName(con), clockDriftFactor, logger);
        
        /// <summary>
        /// Create <see cref="RedisRedlockInstance"/> from <see cref="ConnectionMultiplexer"/>
        /// </summary>
        /// <remarks>
        /// Database sets to default (<see cref="ConnectionMultiplexer.GetDatabase"/>)
        /// </remarks>
        /// <param name="con"></param>
        /// <param name="createKeyFromResourceName"></param>
        /// <param name="name">Instance name for logs and ToString</param>
        /// <param name="clockDriftFactor">Drift factor for system clock (multiply with ttl of lock)</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IRedlockInstance Create(
            IConnectionMultiplexer con,
            Func<string, string> createKeyFromResourceName,
            string name,
            float clockDriftFactor,
            ILogger logger
        ) => new RedisRedlockInstance(() => con.GetDatabase(), createKeyFromResourceName, name, clockDriftFactor, logger);

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
        /// <param name="clockDriftFactor">Drift factor for system clock (multiply with ttl of lock)</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IRedlockInstance Create(
            IConnectionMultiplexer con,
            Func<string, string> createKeyFromResourceName,
            float clockDriftFactor,
            ILogger logger
        ) => Create(con, createKeyFromResourceName, GetName(con), clockDriftFactor, logger);
        
        private static string GetName(IConnectionMultiplexer con) => con.GetEndPoints().FirstOrDefault()?.ToString() ?? "NO_ENDPOINT";
        
        /// <inheritdoc />
        public TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration)
        {
            var drift = lockTimeToLive * _clockDriftFactor;
            return lockTimeToLive - lockingDuration - drift - ConstDrift;
        }
    }
}