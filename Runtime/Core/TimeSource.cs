using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Core
{
    // Supplies the per-frame delta used to tick widgets. Exists so ticking can be driven deterministically
    // instead of reading UnityEngine.Time, which does not advance outside play mode.
    public interface ITimeSource
    {
        public float GetDeltaTime(TimeMode timeMode);
    }

    public sealed class UnityTimeSource : ITimeSource
    {
        // Stateless, so one shared instance serves every controller and group.
        public static readonly UnityTimeSource Default = new();

        public float GetDeltaTime(TimeMode timeMode) =>
            timeMode == TimeMode.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
