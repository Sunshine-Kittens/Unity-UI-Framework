using UnityEngine.Extension;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

using UnityEngine;
using UnityEngine.Extension.Awaitable;

namespace UIFramework
{
    public class TabController
    {
        public readonly struct NavigationResponse
        {
            public readonly bool Success;
            public readonly IWindow Window;
            private readonly Awaitable _awaitable;

            public NavigationResponse(bool success, IWindow window, Awaitable awaitable)
            {
                Success = success;
                Window = window;
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
        
        private ObjectTypeMap<IWindow> _windows = null;

        public IWindow ActiveTabWindow => _activeTabWindow;
        private IWindow _activeTabWindow = null;
        private WidgetAnimationRef _activeAnimationRef = default;

        public int ActiveTabIndex => _activeTabIndex;
        private int _activeTabIndex = 0;
        
        private TabController() { }

        public TabController(IWindow[] windows, int activeTabIndex = 0)
        {
            Populate(windows, activeTabIndex); 
        }

        public bool IsValid()
        {
            return _windows != null;
        }

        public void Clear()
        {
            if (_activeTabWindow != null)
            {
                _activeTabWindow.SetVisibility(WidgetVisibility.Hidden);
            }
            _activeTabWindow = null;
            _windows = null;
            _activeTabIndex = 0;
        }

        public void Populate(IWindow[] windows, int activeTabIndex = 0)
        {
            if (_windows != null)
            {
                Clear();
            }
            _windows = new ObjectTypeMap<IWindow>(windows);
            for (int i = 0; i < _windows.Array.Length; i++)
            {
                _windows.Array[i].Initialize();
                if (i == activeTabIndex)
                {
                    _windows.Array[i].SetVisibility(WidgetVisibility.Visible);
                    _activeTabWindow = _windows.Array[i];
                    _activeTabIndex = i;
                }
            }

            if (_activeTabWindow == null && _windows.Array.Length > 0)
            {
                _activeTabWindow = _windows.Array[0];
                _activeTabWindow.SetVisibility(WidgetVisibility.Visible);
            }
        }

        public NavigationResponse SetActive<TWindow>(GenericAnimation genericAnimation = GenericAnimation.Fade, float length = 0.3F, 
            EasingMode easingMode = EasingMode.Linear, CancellationToken cancellationToken = default) where TWindow : IWindow
        {
            return SetActiveWindowInternal<TWindow>(null, WidgetAnimationRef.FromGeneric(genericAnimation), length, easingMode, cancellationToken);
        }

        public NavigationResponse SetActive<TWindow>(object data, GenericAnimation genericAnimation = GenericAnimation.Fade, float length = 0.3F, 
            EasingMode easingMode = EasingMode.Linear, CancellationToken cancellationToken = default) where TWindow : IWindow
        {
            return SetActiveWindowInternal<TWindow>(data, WidgetAnimationRef.FromGeneric(genericAnimation), length, easingMode, cancellationToken);
        }
        
        public NavigationResponse SetActive<TWindow>(in WidgetAnimationRef animationRef, float length, EasingMode easingMode = EasingMode.Linear,
            CancellationToken cancellationToken = default) where TWindow : IWindow
        {
            return SetActiveWindowInternal<TWindow>(null, in animationRef, length, easingMode, cancellationToken);
        }

        public NavigationResponse SetActive<TWindow>(object data, in WidgetAnimationRef animationRef, float length, EasingMode easingMode = EasingMode.Linear,
            CancellationToken cancellationToken = default) where TWindow : IWindow
        {
            return SetActiveWindowInternal<TWindow>(data, in animationRef, length, easingMode, cancellationToken);
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

        private bool TryGetWindow<TWindow>(out IWindow window, out int index) where TWindow : IWindow
        {
            if (_windows.Dictionary.TryGetValue(typeof(TWindow), out window))
            {
                for (int i = 0; i < _windows.Array.Length; i++)
                {
                    if (_windows.Array[i] == window)
                    {
                        index = i;
                        return true;
                    }
                }
            }
            index = -1;
            return false;
        }
        
        private bool TryGetWindow(int index, out IWindow window)
        {
            if (_windows.Array.IsValidIndex(index))
            {
                window = _windows.Array[index];
                return true;
            }
            window = null;
            return false;
        }
        
        private NavigationResponse SetActiveWindowInternal<TWindow>(object data, in WidgetAnimationRef animationRef, float length, EasingMode easingMode, 
            CancellationToken cancellationToken)
            where TWindow : IWindow
        {
            if (TryGetWindow<TWindow>(out IWindow window, out int index) && _activeTabWindow != window)
            {
                if(data != null)
                    window.SetData(data);
                Awaitable awaitable = SetActiveInternal(_activeTabWindow, window, in animationRef,  length, easingMode, cancellationToken);
                return new NavigationResponse(true, _activeTabWindow, awaitable);
            }
            return new NavigationResponse(false, _activeTabWindow, null);
        }

        private NavigationResponse SetActiveIndexInternal(int index, object data, in WidgetAnimationRef animationRef, float length, EasingMode easingMode, 
            CancellationToken cancellationToken)
        {
            if (TryGetWindow(index, out IWindow window) && _activeTabWindow != window)
            {
                if(data != null)
                    window.SetData(data);
                Awaitable awaitable = SetActiveInternal(_activeTabWindow, window, in animationRef,  length, easingMode, cancellationToken);
                return new NavigationResponse(true, _activeTabWindow, awaitable);
            }
            return new NavigationResponse(false, _activeTabWindow, null);
        }

        private Awaitable SetActiveInternal(IWindow current, IWindow next, in WidgetAnimationRef animationRef, float length, EasingMode easingMode,
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
                    current.AnimateVisibility(WidgetVisibility.Hidden, hideAnimation.Playable(length, PlaybackMode.Forward, easingMode), 
                        InterruptBehavior.Immediate, cancellationToken),
                    next.AnimateVisibility(WidgetVisibility.Visible, showAnimation.Playable(length, PlaybackMode.Forward, easingMode), 
                        InterruptBehavior.Immediate, cancellationToken),
                };
                _activeTabWindow = next;
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