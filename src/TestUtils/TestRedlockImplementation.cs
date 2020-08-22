using System.Collections.Generic;
using System.Collections.Immutable;
using RedlockDotNet;

namespace TestUtils
{
    public class TestRedlockImplementation : IRedlockImplementation
    {
        private TestRedlockImplementation(
            ImmutableArray<IRedlockInstance> instances
        )
        {
            Instances = instances;
        }
        
        public ImmutableArray<IRedlockInstance> Instances { get; }
        
        public static TestRedlockImplementation Create<T>(IEnumerable<T> instances)
            where T: IRedlockInstance
        {
            return new TestRedlockImplementation(instances.ToInstances());
        }

        public static TestRedlockImplementation Create(params IEnumerable<IRedlockInstance>[] instances) 
            => Create<IRedlockInstance>(TestRedlock.Instances(instances));
    }
}