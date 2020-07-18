using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RedLock;

namespace TestUtils
{
    public class TestRedlockImpl : IRedlockImplementation
    {
        public TestRedlockImpl(ImmutableArray<IRedlockInstance> instances)
        {
            Instances = instances;
        }

        public TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration)
        {
            return lockTimeToLive - lockingDuration - lockTimeToLive * 0.01;
        }

        public ImmutableArray<IRedlockInstance> Instances { get; }

        public static TestRedlockImpl Create<T>(IEnumerable<T> instances)
            where T: IRedlockInstance
        {
            return new TestRedlockImpl(instances.Cast<IRedlockInstance>().ToImmutableArray());
        }
            
        public static TestRedlockImpl Create(params IEnumerable<IRedlockInstance>[] instances)
        {
            return new TestRedlockImpl(instances.SelectMany(s => s).ToImmutableArray());
        }
        
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