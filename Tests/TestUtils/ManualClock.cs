using System.Collections.Generic;

using UIFramework.Core;

using UnityEngine.Extension;

namespace UIFramework.TestUtils
{
    // Deterministic replacement for UnityEngine.Time, which does not advance in edit mode.
    public sealed class ManualClock : ITimeSource
    {
        public readonly List<TimeMode> Queries = new();

        public float Delta;

        public ManualClock(float delta = 0f) => Delta = delta;

        public float GetDeltaTime(TimeMode timeMode)
        {
            Queries.Add(timeMode);
            return Delta;
        }
    }
}
