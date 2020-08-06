# RedlockDotNet

Distributed locks with [Redlock](https://redis.io/topics/distlock) algorithm for .net projects


## Simple usage with Microsoft dependency injection


```csharp

IServiceCollection services = new ServiceCollection();
services.AddRedlock().AddRedisStorage(b => {
  b.AddInstance("redis1:6379");
  b.AddInstance("redis2:6379");
  b.AddInstance("redis3:6379");
  b.AddInstance("redis4:6379");
  b.AddInstance("redis5:6379");
});

```

Then you can inject singleton service `IRedlockFactory` and use it

```csharp
IRedlockFactory lockFactory;
// if operation failed in 3 repeats (default behavior), an exception will be thrown
using (var redlock = lockFactory.Create("locking-resource", TimeSpan.FromSeconds(30)))
{
  // this we got the lock
}
// lock is automaticaly released on dispose
```

All methods has async overloads
```csharp
await using var redlock = await lockFactory.CreateAsync("resource", TimeSpan.FromSeconds(30));
```

`IRedlockFactory` has many extensions and overloads, you can change default behavior, for example:

```csharp
// use default ttl - 30s
var redlock = lockFactory.Create("resource");

// Try lock 'resource' while cancellationToken is not cancelled. 
// Waits random (but no more than 200ms) interval between repeats. 
// If cancellation was requested, an exception will be thrown
var redlock = lockFactory.Create("resource", TimeSpan.FromSeconds(10), new CancellationRedlockRepeater(cancellationToken), maxWaitMs: 200);

```

AddInstance has overloads and you can configure redis store options

```csharp

IServiceCollection services = new ServiceCollection();
services.AddRedlock(opt => {
  // Configure clock drift factor for increase or decrease min validity
  opt.ClockDriftFactor = 0.3f;
  
  // Change this for your own for tests or other purposes
  opt.UtcNow = () => DateTime.UtcNow;
}).AddRedisStorage(b => {

  // connection string is StackExchange.Redis compatible
  // https://stackexchange.github.io/StackExchange.Redis/Configuration
  b.AddInstance("redis1:6379");

  // use database 5 on redis server and set name 'second redis server' for logs
  b.AddInstance("redis2:6379", database: 5, name: "second redis server");

  // use ConfigurationOptions for configure
  var conf = new ConfigurationOptions
  {
    EndPoints =
    {
      IPEndPoint.Parse("127.0.0.1:6379")
    }
  };
  b.AddInstance(conf);
  b.ConfigureOptions(opt =>
  {
    // Change redis key naming policy
    opt.RedisKeyFromResourceName = resource => $"locks_{resource}";
  });
});

```

## Use algorithm without di with other lock stores

All the functionality of the algorithm is in the static methods of `Redlock` struct. For use it, you need implements interface `IRedlockImplementation` and `IRedlockInstance`

`IRedlockInstance` represents instance to store distributed lock (e.g. one independent redis server). It contains methods for locking and unlocking a specific resource with a specific nonce for a specific time

`IRedlockImplementation` is a simple container for the `IRedlockInstance`s array and the `MinValidity' method, which calculates the minimum time that the lock will take

```csharp
IRedlockImplementation impl;
ILogger log;

// Try lock "resource" with automatic unblocking after 10 seconds
Redlock? redlock = Redlock.TryLock("resource", "nonce", TimeSpan.FromSeconds(10), impl, log, () => DateTime.UtcNow);

Redlock? redlock = Redlock.TryLock("resource", "nonce", TimeSpan.FromSeconds(10), impl, log, new CancellationRedlockRepeater(cancellationToken), maxWaitMs: 200);

// lock or exception
Redlock redlock = Redlock.Lock("resource", "nonce", TimeSpan.FromSeconds(10), impl, log, new CancellationRedlockRepeater(cancellationToken), maxWaitMs: 200);

```

### Repeaters

Repeater is a way for separate the algorithm  from the logic of repetion. It`s a simple interface
```csharp
public interface IRedlockRepeater
{
    bool Next();

    // Has default interface implementation Thread.Sleap(random(0,maxWaitMs))
    void WaitRandom(int maxWaitMs);
    
    // Has default interface implementation Task.Delay(random(0,maxWaitMs))
    ValueTask WaitRandomAsync(int maxWaitMs, CancellationToken cancellationToken = default);

    // Has default interface implementation RedlockException
    Exception CreateException(string resource, string nonce, int attemptCount);
}
```

We have three repeater implementation:
* `CancellationRedlockRepeater` - repeat while CancellationToken does not canceled
* `MaxRetriesRedlockRepeater` - repeat max count
* `NoopRedlockRepeater` - no repeat


