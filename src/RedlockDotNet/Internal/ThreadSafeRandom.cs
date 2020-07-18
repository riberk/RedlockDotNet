using System;
using System.Security.Cryptography;

namespace RedlockDotNet.Internal
{
    internal static class ThreadSafeRandom
    {
        private static readonly RNGCryptoServiceProvider Global = new RNGCryptoServiceProvider();
        
        [ThreadStatic]
        private static Random? _local;

        internal static Random Local => _local ??= CreateLocal();
        
        public static int Next(int maxValue) => Local.Next(maxValue);

        private static Random CreateLocal()
        {
            Span<byte> buffer = stackalloc byte[4];
            Global.GetBytes(buffer);
            return new Random(BitConverter.ToInt32(buffer));
        }
    }
}