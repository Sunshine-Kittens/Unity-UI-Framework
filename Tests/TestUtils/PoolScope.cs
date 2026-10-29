using System;

using UIFramework.Core;

namespace UIFramework.TestUtils
{
    // Clears every registered object pool on entry and exit. Pools are process-wide statics, so without this
    // a test both inherits whatever the previous test left behind and leaks into the next one.
    //
    //     using (new PoolScope()) { ... }
    public sealed class PoolScope : IDisposable
    {
        public PoolScope() => PoolRegistry.ResetAll();

        public int TotalPooled => PoolRegistry.TotalPooled;

        public int CountOf(string poolName) => PoolRegistry.CountOf(poolName);

        public void Dispose() => PoolRegistry.ResetAll();
    }
}
