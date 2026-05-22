using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core.Interfaces;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public class MenuController : ScreenController
    {
        private readonly IWidget _background;

        public MenuController(IWidget background, IEnumerable<WidgetCollector<IScreen>> collectors, TimeMode timeMode)
            : base(collectors, timeMode)
        {
            _background = background;
        }

        protected override void OnEnter()
        {
            AnimationPlayable playable = GetBackgroundAnimation(WidgetVisibility.Visible);
            if (playable.IsValid())
                _background.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate);
            else
                _background.SetVisibility(WidgetVisibility.Visible);
        }

        protected override void OnExited()
        {
            AnimationPlayable playable = GetBackgroundAnimation(WidgetVisibility.Hidden);
            if (playable.IsValid())
                _background.AnimateVisibility(WidgetVisibility.Hidden, playable, InterruptBehavior.Immediate);
            else
                _background.SetVisibility(WidgetVisibility.Hidden);
        }

        protected virtual AnimationPlayable GetBackgroundAnimation(WidgetVisibility visibility)
        {
            IAnimation animation = _background.GetDefaultAnimation(visibility);
            if (animation == null) return default;
            return animation.ToPlayable().WithTimeMode(TimeMode).Create();
        }
    }
}
