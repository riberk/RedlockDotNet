using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RedlockDotNet;

namespace TestUtils
{
    public static class TestRedlock
    {
        public static ImmutableArray<IRedlockInstance> ToInstances<T>(this IEnumerable<T> instances)
            where T: IRedlockInstance
        {
            return instances.Cast<IRedlockInstance>().ToImmutableArray();
        }

        public static ImmutableArray<IRedlockInstance> Instances(params IEnumerable<IRedlockInstance>[] instances) 
            => instances.SelectMany(s => s).ToInstances();
        
        public static ImmutableArray<T> Instances<T>(int count, Func<T> create) 
            => Instances(count, i => create()).ToImmutableArray();
        
        public static ImmutableArray<T> Instances<T>(int count, Func<int, T> create)
        {
            var arr = new T[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = create(i);
            }

            return arr.ToImmutableArray();
        }
    }
}