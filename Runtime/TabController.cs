using UnityEngine.Extension;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

using UIFramework.Animation;

using UnityEngine;
using UnityEngine.Extension.Awaitable;

namespace UIFramework
{
    public class TabController
    {
        public readonly struct NavigationResponse
        {
            public readonly bool Success;
            public readonly IWidget Widget;
            private readonly Awaitable _awaitable;

            public NavigationResponse(bool success, IWidget widget, Awaitable awaitable)
            {
                Success = success;
                Widget = widget;
                _awaitable = awaitable;
            }
            
            public Awaiter GetAwaiter() => new Awaiter(_awaitable);

            public class Awaiter : INotifyCompletion
            {
                private readonly Awaitable _awaitable;
                
                public Awaiter(Awaitable awaitable)
                {
                    _awaitable = awaitable;
                }

                public bool IsCompleted => _awaitable == null || _awaitable.IsCompleted;

                public void OnCompleted(Action continuation)
                {
                    if (continuation == null) throw new ArgumentNullException(nameof(continuation));
                    if (_awaitable != null)
                        _awaitable.GetAwaiter().OnCompleted(continuation);
                    else
                        continuation();
                }

                public void GetResult()
                {
                    if(_awaitable != null)
                        _awaitable.GetAwaiter().GetResult();
                }
            }
        }
        
        private ObjectTypeMap<IWidget> _widgets = null;

        public IWidget ActiveTabWidget => _activeTabWidget;
        private IWidget _activeTabWidget = null;
        private WidgetAnimationRef _activeAnimationRef = default;

        public int ActiveTabIndex => _activeTabIndex;
        private int _activeTabIndex = 0;
        
        private TabController() { }

        public TabController(IWidget[] widgets, int activeTabIndex = 0)
        {
            Populate(widgets, activeTabIndex); 
        }

        public bool IsValid()
        {
            return _widgets != null;
        }

        public void Clear()
        {
            if (_activeTabWidget != null)
            {
                _activeTabWidget.SetVisibility(WidgetVisibility.Hidden);
            }
            _activeTabWidget = null;
            _widgets = null;
            _activeTabIndex = 0;
        }

        public void Populate(IWidget[] widgets, int activeTabIndex = 0)
        {
            if (_widgets != null)
            {
                Clear();
            }
            _widgets = new ObjectTypeMap<IWidget>(widgets);
            for (int i = 0; i < _widgets.Array.Length; i++)
            {
                _widgets.Array[i].Initialize();
                if (i == activeTabIndex)
                {
                    _widgets.Array[i].SetVisibility(WidgetVisibility.Visible);
                    _activeTabWidget = _widgets.Array[i];
                    _activeTabIndex = i;
                }
            }

            if (_activeTabWidget == null && _widgets.Array.Length > 0)
            {
                _activeTabWidget = _widgets.Array[0];
                _activeTabWidget.SetVisibility(WidgetVisibility.Visible);
            }
        }

        public NavigationResponse SetActive<TWidget>(GenericAnimation genericAnimation = GenericAnimation.Fade, float length = 0.3F, 
            EasingMode easingMode = EasingMode.Linear, CancellationToken cancellationToken = default) where TWidget : IWidget
        {
            return SetActiveWidgetInternal<TWidget>(null, WidgetAnimationRef.FromGeneric(genericAnimation), length, easingMode, cancellationToken);
        }

        public NavigationResponse SetActive<TWidget>(object data, GenericAnimation genericAnimation = GenericAnimation.Fade, float length = 0.3F, 
            EasingMode easingMode = EasingMode.Linear, CancellationToken cancellationToken = default) where TWidget : IWidget
        {
            return SetActiveWidgetInternal<TWidget>(data, WidgetAnimationRef.FromGeneric(genericAnimation), length, easingMode, cancellationToken);
        }
        
        public NavigationResponse SetActive<TWidget>(in WidgetAnimationRef animationRef, float length, EasingMode easingMode = EasingMode.Linear,
            CancellationToken cancellationToken = default) where TWidget : IWidget
        {
            return SetActiveWidgetInternal<TWidget>(null, in animationRef, length, easingMode, cancellationToken);
        }

        public NavigationResponse SetActive<TWidget>(object data, in WidgetAnimationRef animationRef, float length, EasingMode easingMode = EasingMode.Linear,
            CancellationToken cancellationToken = default) where TWidget : IWidget
        {
            return SetActiveWidgetInternal<TWidget>(data, in animationRef, length, easingMode, cancellationToken);
        }

        public NavigationResponse SetActiveIndex(int index, GenericAnimation genericAnimation = GenericAnimation.Fade, float length = 0.3F, 
            EasingMode easingMode = EasingMode.Linear, CancellationToken cancellationToken = default)
        {
            return SetActiveIndexInternal(index, null, WidgetAnimationRef.FromGeneric(genericAnimation), length, easingMode, cancellationToken);
        }

        public NavigationResponse SetActiveIndex(int index, object data, GenericAnimation genericAnimation = GenericAnimation.Fade, float length = 0.3F, 
            EasingMode easingMode = EasingMode.Linear, CancellationToken cancellationToken = default)
        {
            return SetActiveIndexInternal(index, data, WidgetAnimationRef.FromGeneric(genericAnimation), length, easingMode, cancellationToken);
        }

        public NavigationResponse SetActiveIndex(int index, in WidgetAnimationRef animationRef, float length, EasingMode easingMode = EasingMode.Linear,
        CancellationToken cancellationToken = default)
        {
            return SetActiveIndexInternal(index, null, in animationRef, length, easingMode, cancellationToken);
        }

        public NavigationResponse SetActiveIndex(int index, object data, in WidgetAnimationRef animationRef, float length, EasingMode easingMode = EasingMode.Linear,
            CancellationToken cancellationToken = default)
        {
            return SetActiveIndexInternal(index, data, in animationRef, length, easingMode, cancellationToken);
        }

        private bool TryGetWidget<TWidget>(out IWidget widget, out int index) where TWidget : IWidget
        {
            if (_widgets.Dictionary.TryGetValue(typeof(TWidget), out widget))
            {
                for (int i = 0; i < _widgets.Array.Length; i++)
                {
                    if (_widgets.Array[i] == widget)
                    {
                        index = i;
                        return true;
                    }
                }
            }
            index = -1;
            return false;
        }
        
        private bool TryGetWidget(int index, out IWidget widget)
        {
            if (_widgets.Array.IsValidIndex(index))
            {
                widget = _widgets.Array[index];
                return true;
            }
            widget = null;
            return false;
        }
        
        private NavigationResponse SetActiveWidgetInternal<TWidget>(object data, in WidgetAnimationRef animationRef, float length, EasingMode easingMode, 
            CancellationToken cancellationToken)
            where TWidget : IWidget
        {
            if (TryGetWidget<TWidget>(out IWidget widget, out int index) && _activeTabWidget != widget)
            {
                if(data != null)
                    widget.SetData(data);
                Awaitable awaitable = SetActiveInternal(_activeTabWidget, widget, in animationRef,  length, easingMode, cancellationToken);
                return new NavigationResponse(true, _activeTabWidget, awaitable);
            }
            return new NavigationResponse(false, _activeTabWidget, null);
        }

        private NavigationResponse SetActiveIndexInternal(int index, object data, in WidgetAnimationRef animationRef, float length, EasingMode easingMode, 
            CancellationToken cancellationToken)
        {
            if (TryGetWidget(index, out IWidget widget) && _activeTabWidget != widget)
            {
                if(data != null)
                    widget.SetData(data);
                Awaitable awaitable = SetActiveInternal(_activeTabWidget, widget, in animationRef,  length, easingMode, cancellationToken);
                return new NavigationResponse(true, _activeTabWidget, awaitable);
            }
            return new NavigationResponse(false, _activeTabWidget, null);
        }

        private Awaitable SetActiveInternal(IWidget current, IWidget next, in WidgetAnimationRef animationRef, float length, EasingMode easingMode,
            CancellationToken cancellationToken)
        {
            Awaitable awaitable = null;
            if (length > 0.0F)
            {
                WidgetAnimationRef currentAnimationRef = _activeAnimationRef;
                if (!currentAnimationRef.IsValid)
                {
                    IAnimation currentDefaultAnimation = current.GetDefaultAnimation(WidgetVisibility.Hidden);
                    if (currentDefaultAnimation != null)
                    {
                        currentAnimationRef = WidgetAnimationRef.FromExplicit(currentDefaultAnimation);   
                    }
                }
                IAnimation hideAnimation = currentAnimationRef.Resolve(current, WidgetVisibility.Hidden);
                IAnimation showAnimation = animationRef.Resolve(next, WidgetVisibility.Visible);
                Awaitable[] awaitables =
                {
                    current.AnimateVisibility(WidgetVisibility.Hidden)
                        .WithAnimation(hideAnimation)
                        .WithLength(length)
                        .WithEasingMode(easingMode)
                        .WithCancellation(cancellationToken)
                        .Animate(),
                    next.AnimateVisibility(WidgetVisibility.Visible)
                        .WithAnimation(showAnimation)
                        .WithLength(length)
                        .WithEasingMode(easingMode)
                        .WithCancellation(cancellationToken)
                        .Animate()
                };
                _activeTabWidget = next;
                _activeAnimationRef = animationRef;
                awaitable = WhenAll.Await(awaitables).Awaitable;
            }
            else
            {
                current.SetVisibility(WidgetVisibility.Hidden);
                next.SetVisibility(WidgetVisibility.Visible);
            }
            return awaitable;
        }
    }
}