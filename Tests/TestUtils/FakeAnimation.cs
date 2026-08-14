using System;
using System.Collections.Generic;

using UnityEngine.Extension;

namespace UIFramework.TestUtils
{
    // Records evaluation instead of animating, so a test can tell whether the framework ever played it.
    public sealed class FakeAnimation : IAnimation
    {
        public readonly List<float> Evaluations = new();

        public float Length { get; }
        public IReadOnlyList<AnimationEvent> Events { get; } = Array.Empty<AnimationEvent>();

        public FakeAnimation(float length = 1f) => Length = length;

        public void Evaluate(float normalisedTime) => Evaluations.Add(normalisedTime);
    }
}
