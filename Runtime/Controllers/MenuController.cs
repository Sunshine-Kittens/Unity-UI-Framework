using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core.Interfaces;

using UnityEngine.Extension;

namespace UIFramework.Controllers
{
    public class MenuController : ScreenController
    {
        protected IWidget BackgroundWidget { get; }

        public MenuController(IWidget backgroundWidget, IEnumerable<WidgetCollector<IScreen>> collectors, TimeMode timeMode)
            : base(collectors, timeMode)
        {
            BackgroundWidget = backgroundWidget;
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            AnimationPlayable playable = GetBackgroundAnimation(WidgetVisibility.Visible);
            if (playable.IsValid())
                BackgroundWidget.AnimateVisibility(WidgetVisibility.Visible, playable, InterruptBehavior.Immediate);
            else
                BackgroundWidget.SetVisibility(WidgetVisibility.Visible);
        }

        protected override void OnExit()
        {
            base.OnExit();
            AnimationPlayable playable = GetBackgroundAnimation(WidgetVisibility.Hidden);
            if (playable.IsValid())
                BackgroundWidget.AnimateVisibility(WidgetVisibility.Hidden, playable, InterruptBehavior.Immediate);
            else
                BackgroundWidget.SetVisibility(WidgetVisibility.Hidden);
        }

        protected virtual AnimationPlayable GetBackgroundAnimation(WidgetVisibility visibility)
        {
            IAnimation animation = BackgroundWidget.GetDefaultAnimation(visibility);
            if (animation == null) return default;
            return animation.ToPlayable().WithTimeMode(TimeMode).Create();
        }
    }
}
