using System.Collections.Generic;

using UnityEngine.Extension;

namespace UIFramework
{
    public abstract class WidgetAnimation : IAnimation
    {
        public virtual float Length => 1.0F;
        public IReadOnlyList<AnimationEvent> Events => _events;
        private readonly List<AnimationEvent> _events = new ();

        protected WidgetAnimation() { }

        public abstract void Evaluate(float normalisedTime);

        protected void AddEvent(AnimationEvent animationEvent)
        {
            _events.Add(animationEvent);
        }

        protected void RemoveEvent(AnimationEvent animationEvent)
        {
            _events.Remove(animationEvent);
        }
    }
}