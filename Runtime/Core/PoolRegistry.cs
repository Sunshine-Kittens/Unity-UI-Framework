using System;
using System.Collections.Generic;

namespace UIFramework.Core
{
    // Diagnostic access to the framework's static object pools. Pools are process-wide and survive between
    // play sessions and tests, so a suite needs a way to clear them between cases and to assert that objects
    // are returned rather than leaked.
    //
    // Pools register themselves from their static constructor, so only pools that have actually been used
    // appear here — an unused pool is empty by definition.
    internal static class PoolRegistry
    {
        private readonly struct Pool
        {
            public readonly string Name;
            public readonly Func<int> Count;
            public readonly Action Reset;

            public Pool(string name, Func<int> count, Action reset)
            {
                Name = name;
                Count = count;
                Reset = reset;
            }
        }

        private static readonly List<Pool> _pools = new();

        internal static void Register(string name, Func<int> count, Action reset)
            => _pools.Add(new Pool(name, count, reset));

        internal static IEnumerable<string> Names
        {
            get
            {
                foreach (Pool pool in _pools)
                    yield return pool.Name;
            }
        }

        internal static int TotalPooled
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _pools.Count; i++)
                    total += _pools[i].Count();
                return total;
            }
        }

        // -1 when no pool of that name has been registered, so a typo'd assertion fails rather than passes.
        internal static int CountOf(string name)
        {
            for (int i = 0; i < _pools.Count; i++)
            {
                if (_pools[i].Name == name)
                    return _pools[i].Count();
            }
            return -1;
        }

        internal static void ResetAll()
        {
            for (int i = 0; i < _pools.Count; i++)
                _pools[i].Reset();
        }
    }
}
