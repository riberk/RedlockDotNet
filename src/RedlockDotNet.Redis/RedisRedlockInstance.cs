using System;
using System.Collections.Generic;
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
        private readonly Func<string, RedisKey> _createKeyFromResourceName;
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
if redis.call('hget', KEYS[1], 'nonce') == ARGV[1] then
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
        /// result is <see cref="ExtendResult"/>
        /// </summary>
        private static readonly string ExtendLua = @"
local currentVal = redis.call('hget', KEYS[1], 'nonce')
if currentVal == false then
  return -2
elseif (currentVal == ARGV[1]) then
  redis.call('pexpire', KEYS[1], ARGV[2])
  return 1
else
  return -1
end
";
        
        
        // ReSharper disable once ConvertToConstant.Local
        /// <summary>
        /// KEYS[1] is locking resource redis key
        /// KEYS[2] is nonce
        /// ARGV[1] is ttl
        /// ARGV[2..n] is metadata ARGV[2] - key, ARGV[3] - value, etc
        /// </summary>
        private static readonly string SetLua = @"
local currentVal = redis.call('hget', KEYS[1], 'nonce')
if currentVal == false then
  local ttl = ARGV[1]
  table.remove(ARGV, 1)
  redis.call('hset', KEYS[1], 'nonce', KEYS[2], unpack(ARGV))
  redis.call('pexpire', KEYS[1], ttl)
  return true
else
  return false
end
";

        
        /// <summary>
        /// Redis instance for distributed lock
        /// </summary>
        public RedisRedlockInstance(
            Func<IDatabase> selectDb,
            Func<string, RedisKey> createKeyFromResourceName, 
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
        public bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata = null)
        {
            var key = Key(resource);
            _logger.TryLock(resource, nonce, _name, lockTimeToLive, key);
            var keys = new RedisKey[] {key, nonce,};
            var argv = RedisValuesForLock(lockTimeToLive, metadata);
            var res = (bool) SelectDb().ScriptEvaluate(SetLua, keys, argv, CommandFlags.DemandMaster);
            return res;
        }

        /// <inheritdoc />
        public async Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata = null)
        {
            var key = Key(resource);
            _logger.TryLock(resource, nonce, _name, lockTimeToLive, key);
            var keys = new RedisKey[] {key, nonce,};
            var argv = RedisValuesForLock(lockTimeToLive, metadata);
            var res = (bool) await SelectDb().ScriptEvaluateAsync(SetLua, keys, argv, CommandFlags.DemandMaster).ConfigureAwait(false);
            return res;
        }

        private static RedisValue[] RedisValuesForLock(TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata)
        {
            var argv = new RedisValue[(metadata?.Count ?? 0)*2 + 1];
            argv[0] = lockTimeToLive.TotalMilliseconds;
            var i = 1;
            if(metadata != null)
            {
                foreach (var (k, v) in metadata)
                {
                    argv[i++] = k;
                    argv[i++] = v;
                }
            }

            return argv;
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
        public ExtendResult TryExtend(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            var key = Key(resource);
            _logger.TryExtendLock(resource, nonce, _name, lockTimeToLive, key);
            var scriptEvaluateResult = SelectDb()
                .ScriptEvaluate(ExtendLua, new RedisKey[] {key}, new RedisValue[]
                {
                    nonce,
                    (long) lockTimeToLive.TotalMilliseconds,
                });
            _logger.ExtendScriptExecuted(resource, nonce, _name, lockTimeToLive, key, scriptEvaluateResult);
            return (ExtendResult) (int) scriptEvaluateResult;
        }

        /// <inheritdoc />
        public async Task<ExtendResult> TryExtendAsync(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            var key = Key(resource);
            _logger.TryExtendLock(resource, nonce, _name, lockTimeToLive, key);
            var scriptEvaluateResult = await SelectDb()
                .ScriptEvaluateAsync(ExtendLua, new RedisKey[] {key}, new RedisValue[]
                {
                    nonce,
                    (long) lockTimeToLive.TotalMilliseconds,
                });
            _logger.ExtendScriptExecuted(resource, nonce, _name, lockTimeToLive, key, scriptEvaluateResult);
            return (ExtendResult) (int) scriptEvaluateResult;
        }

        /// <inheritdoc />
        public InstanceLockInfo? GetInfo(string resource)
        {
            var (transaction, entriesTask, ttlTask) = InfoTransactionTasks(resource);
            if (!transaction.Execute())
            {
                throw new InvalidOperationException("Transaction return false");
            }
            return ToInstanceLockInfo(resource, ttlTask, entriesTask);
        }

        /// <inheritdoc />
        public async Task<InstanceLockInfo?> GetInfoAsync(string resource)
        {
            var (transaction, entriesTask, ttlTask) = InfoTransactionTasks(resource);
            if (!await transaction.ExecuteAsync())
            {
                throw new InvalidOperationException("Transaction return false");
            }
            return ToInstanceLockInfo(resource, ttlTask, entriesTask);
        }

        private (ITransaction, Task<HashEntry[]> entriesTask, Task<TimeSpan?> ttlTask) InfoTransactionTasks(string resource)
        {
            var key = Key(resource);
            var db = SelectDb();
            var transaction = db.CreateTransaction();
            var entriesTask = transaction.HashGetAllAsync(key);
            var ttlTask = transaction.KeyTimeToLiveAsync(key);
            return (transaction, entriesTask, ttlTask);
        }
        
        private static InstanceLockInfo? ToInstanceLockInfo(
            string resource,
            Task<TimeSpan?> ttlTask, 
            Task<HashEntry[]> entriesTask
        )
        {
            var entries = entriesTask.Result;
            if (!entries.Any())
            {
                return null;
            }

            var meta = entries.ToDictionary(
                x => (string?)x.Name ?? throw new InvalidOperationException("key is null"),
                x => (string?)x.Value ?? throw new InvalidOperationException("value is null")
            );
            
            if (!meta.Remove("nonce", out var nonce))
            {
                throw new InvalidOperationException($"Nonce not found in {resource}");
            }

            return new InstanceLockInfo(nonce, meta, ttlTask.Result);
        }

        /// <summary>
        /// Build key for redis from resource name
        /// </summary>
        private RedisKey Key(string resource) => _createKeyFromResourceName(resource);

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
            Func<string, RedisKey> createKeyFromResourceName,
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
            Func<string, RedisKey> createKeyFromResourceName,
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
            Func<string, RedisKey> createKeyFromResourceName,
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
            Func<string, RedisKey> createKeyFromResourceName,
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