using UnityEngine.Extension;

using System;

namespace UIFramework
{
    public class TabController : IWidget
    {
        public WidgetState State { get; private set; } = WidgetState.Uninitialized;

        private ObjectTypeMap<IWindow> _windows = null;

        public IWindow ActiveTabWindow { get { return _activeTabWindow; } }
        private IWindow _activeTabWindow = null;

        public int ActiveTabIndex { get { return _activeTabIndex; } }
        private int _activeTabIndex = 0;
        
        private TabController() { }

        public TabController(IWindow[] windows, int activeTabIndex = 0)
        {
            Populate(windows, activeTabIndex);
            Initialize();
        }

        public bool IsValid()
        {
            return _windows != null;
        }

        public void Initialize() 
        {
            State = WidgetState.Initialized;
        }

        public void UpdateWidget(float deltaTime)
        {
            for (int i = 0; i < _windows.Array.Length; i++)
            {
                if (_windows.Array[i].IsVisible)
                {
                    _windows.Array[i].UpdateWidget(deltaTime);
                }
            }
        }

        public void Terminate() 
        {
            for (int i = 0; i < _windows.Array.Length; i++)
            {
                _windows.Array[i].Terminate();
            }
            State = WidgetState.Terminated;
        }

        public void Clear()
        {
            if (_activeTabWindow != null)
            {
                _activeTabWindow.Close();
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
                    _windows.Array[i].Open();
                    _activeTabWindow = _windows.Array[i];
                    _activeTabIndex = i;
                }
            }

            if (_activeTabWindow == null && _windows.Array.Length > 0)
            {
                _activeTabWindow = _windows.Array[0];
                _activeTabWindow.Open();
            }
        }

        public IWindow SetActive<WindowType>(GenericAnimation type = GenericAnimation.Fade, float length = 0.5F, EasingMode easingMode = EasingMode.EaseInOut) where WindowType : IWindow
        {
            return SetActiveInternal<WindowType>(new AccessAnimationParams(type, length, easingMode));
        }

        public IWindow SetActive<WindowType>(in AccessAnimationParams accessPlayable) where WindowType : IWindow
        {
            return SetActiveInternal<WindowType>(in accessPlayable);
        }

        public IWindow SetActive<WindowType>(object data, GenericAnimation type = GenericAnimation.Fade, float length = 0.5F, EasingMode easingMode = EasingMode.EaseInOut) where WindowType : IWindow
        {
            IWindow window = SetActiveInternal<WindowType>(new AccessAnimationParams(type, length, easingMode));
            window.SetData(data);
            return window;
        }

        public IWindow SetActive<WindowType>(object data, in AccessAnimationParams accessPlayable) where WindowType : IWindow
        {
            IWindow window = SetActiveInternal<WindowType>(in accessPlayable);
            window.SetData(data);
            return window;
        }

        public IWindow SetActiveIndex(int index, GenericAnimation type = GenericAnimation.Fade, float length = 0.5F, EasingMode easingMode = EasingMode.EaseInOut)
        {
            return SetActiveIndexInternal(index, new AccessAnimationParams(type, length, easingMode));
        }

        public IWindow SetActiveIndex(int index, in AccessAnimationParams accessPlayable)
        {
            return SetActiveIndexInternal(index, in accessPlayable);
        }

        public IWindow SetActiveIndex(int index, object data, GenericAnimation type = GenericAnimation.Fade, float length = 0.5F, EasingMode easingMode = EasingMode.EaseInOut)
        {
            IWindow window = SetActiveIndexInternal(index, new AccessAnimationParams(type, length, easingMode));
            window.SetData(data);
            return window;
        }

        public IWindow SetActiveIndex(int index, object data, in AccessAnimationParams accessPlayable)
        {
            IWindow window = SetActiveIndexInternal(index, in accessPlayable);
            window.SetData(data);
            return window;
        }

        private IWindow SetActiveInternal<WindowType>(in AccessAnimationParams accessPlayable) where WindowType : IWindow
        {
            if (_activeTabWindow != null)
            {
                Type windowType = typeof(WindowType);
                if (windowType != _activeTabWindow.GetType())
                {
                    IWindow targetWindow;
                    if (_windows.Dictionary.TryGetValue(windowType, out targetWindow))
                    {
                        for (int i = 0; i < _windows.Array.Length; i++)
                        {
                            if (_windows.Array[i] == targetWindow)
                            {
                                _activeTabIndex = i;
                                break;
                            }
                        }

                        AccessAnimationPlayable closeAnimationPlayable;
                        if (_activeTabWindow.AccessAnimationPlayable.Animation != null)
                        {
                            closeAnimationPlayable = _activeTabWindow.AccessAnimationPlayable.CreateInverse();
                        }
                        else
                        {
                            closeAnimationPlayable = _activeTabWindow.GetDefaultAccessAnimation().CreatePlayable(AccessOperation.Close, accessPlayable.Length, accessPlayable.EasingMode);
                        }
                        _activeTabWindow.Close(in closeAnimationPlayable);
                        _activeTabWindow = targetWindow;
                        _activeTabWindow.Open(accessPlayable.CreatePlayable(_activeTabWindow, AccessOperation.Open));
                    }
                }
            }
            return _activeTabWindow;
        }

        private IWindow SetActiveIndexInternal(int index, in AccessAnimationParams accessPlayable)
        {
            if (index != ActiveTabIndex)
            {
                if (_activeTabWindow != null)
                {
                    if (index >= 0 && index < _windows.Array.Length)
                    {
                        _activeTabIndex = index;
                        IWindow targetWindow = _windows.Array[index];

                        AccessAnimationPlayable closeAnimationPlayable;
                        if (_activeTabWindow.AccessAnimationPlayable.Animation != null)
                        {
                            closeAnimationPlayable = _activeTabWindow.AccessAnimationPlayable.CreateInverse();
                        }
                        else
                        {
                            closeAnimationPlayable = _activeTabWindow.GetDefaultAccessAnimation().CreatePlayable(AccessOperation.Close, accessPlayable.EasingMode, TimeMode.Scaled);
                        }
                        _activeTabWindow.Close(in closeAnimationPlayable);
                        _activeTabWindow = targetWindow;
                        _activeTabWindow.Open(accessPlayable.CreatePlayable(_activeTabWindow, AccessOperation.Open));
                    }
                }
            }
            return _activeTabWindow;
        }
    }
}