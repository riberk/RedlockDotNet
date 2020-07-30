using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RedlockDotNet;

namespace TestUtils
{
    public class TestRedlockImpl : IRedlockImplementation
    {
        public delegate TimeSpan MinValidityDelegate(TimeSpan lockTimeToLive, TimeSpan lockingDuration);

        private readonly MinValidityDelegate _minValidity;

        public TestRedlockImpl(
            ImmutableArray<IRedlockInstance> instances,
            MinValidityDelegate minValidity
        )
        {
            _minValidity = minValidity;
            Instances = instances;
        }

        public TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration) => _minValidity(lockTimeToLive, lockingDuration);

        public ImmutableArray<IRedlockInstance> Instances { get; }

        public static TestRedlockImpl Create<T>(IEnumerable<T> instances)
            where T : IRedlockInstance => Create(instances, (ttl, duration) => ttl - duration);
        
        public static TestRedlockImpl Create<T>(IEnumerable<T> instances, MinValidityDelegate minValidity)
            where T: IRedlockInstance
        {
            return new TestRedlockImpl(instances.Cast<IRedlockInstance>().ToImmutableArray(), minValidity);
        }

        public static TestRedlockImpl Create(params IEnumerable<IRedlockInstance>[] instances) 
            => Create<IRedlockInstance>(instances.SelectMany(s => s));
        
        public static TestRedlockImpl Create(MinValidityDelegate minValidity, params IEnumerable<IRedlockInstance>[] instances) 
            => Create(instances.SelectMany(s => s), minValidity);

        public static T[] CreateInstances<T>(int count, Func<T> create)
        {
            var arr = new T[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = create();
            }

            return arr;
        }
    }
}