using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework
{
    public abstract class WidgetBase<TWidget> : MonoBehaviour, IWidget where TWidget : WidgetBase<TWidget>
    {
        private sealed class VisibilityAnimationHandle
        {
            public AnimationPlayer AnimationPlayer { get; private set; } = null;
            public bool ExecutionComplete { get; private set; } = false;

            public bool IsAnimating => (AnimationPlayer != null && AnimationPlayer.IsPlaying) || !ExecutionComplete;
            
            private VisibilityAnimationHandle() { }

            public VisibilityAnimationHandle(AnimationPlayer animationPlayer)
            {
                AnimationPlayer = animationPlayer ?? throw new ArgumentNullException(nameof(animationPlayer));
            }
            
            public void Complete()
            {
                if (AnimationPlayer.IsPlaying)
                {
                    AnimationPlayer.Stop();
                }
                AnimationPlayer.Release();
                AnimationPlayer = null;
                ExecutionComplete = true;
            }
        }

        // IWidget
        public bool IsInitialized => State == WidgetState.Initialized;
        public WidgetState State { get; private set; } = WidgetState.Uninitialized;

        public IWidget Parent => _parent;
        private TWidget _parent;

        public int ChildCount => _children.Count;

        public WidgetVisibility Visibility { get; private set; } = WidgetVisibility.Hidden;

        public bool IsVisible => Visibility == WidgetVisibility.Visible && Opacity > 0.0F;

        public bool IsAnimating => _animationHandle != null && _animationHandle.IsAnimating;

        public abstract int LocalSortOrder { get; }
        public abstract int GlobalSortOrder { get; }
        public abstract int RenderSortOrder { get; }

        public abstract float Opacity { get; }

        IReadOnlyScalarFlag IReadOnlyWidget.IsEnabled => IsEnabled;
        public IScalarFlag IsEnabled => _isEnabled;
        private readonly ScalarFlag _isEnabled = new(true);

        IReadOnlyScalarFlag IReadOnlyWidget.IsInteractable => IsInteractable;
        public IScalarFlag IsInteractable => IsInteractableInternal;
        protected readonly ScalarFlag IsInteractableInternal = new(true);

        public event WidgetAction Shown;
        public event WidgetAction Hidden;

        // WidgetBase
        private readonly List<TWidget> _children = new();
        private VisibilityAnimationHandle _animationHandle;
        private CancellationTokenSource _animationCts;
        private CancellationTokenSource _queuedAnimationCts;

        // IWidget
        public virtual void Initialize()
        {
            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            IsInteractableInternal.OnUpdate += OnIsInteractableUpdated;
            SetActive(false);
            Visibility = WidgetVisibility.Hidden;
            State = WidgetState.Initialized;
            for (int i = 0; i < ChildCount; i++)
            {
                GetChildAt(i).Initialize();
            }
        }

        public virtual void Terminate()
        {
            for (int i = 0; i < ChildCount; i++)
            {
                GetChildAt(i).Terminate();
            }
            _isEnabled.OnUpdate -= OnIsEnabledUpdated;
            IsInteractableInternal.OnUpdate -= OnIsInteractableUpdated;
            _animationCts?.Cancel();
            _animationCts = null;
            _queuedAnimationCts?.Cancel();
            _queuedAnimationCts = null;
            _animationHandle = null;
            SetActive(false);
            Visibility = WidgetVisibility.Hidden;            
            ResetAnimatedProperties();
            _isEnabled.Reset(true);
            _isEnabled.OnUpdate -= OnIsEnabledUpdated;
            IsInteractableInternal.Reset(true);
            IsInteractableInternal.OnUpdate -= OnIsInteractableUpdated;
            State = WidgetState.Terminated;
        }

        IReadOnlyWidget IReadOnlyWidget.GetChildAt(int index) => GetChildAt(index);

        public IWidget GetChildAt(int index)
        {
            if (_children.IsValidIndex(index)) throw new ArgumentOutOfRangeException(nameof(index));
            return _children[index];
        }

        public void UpdateWidget(float deltaTime)
        {
            for (int i = 0; i < _children.Count; i++)
            {
                TWidget child = _children[i];
                if (child.gameObject.activeInHierarchy)
                {
                    child.UpdateWidget(deltaTime);
                }
            }
        }

        public void SetVisibility(WidgetVisibility visibility)
        {
            if (visibility != Visibility)
            {
                if (IsAnimating)
                {
                    IsInteractableInternal.SetOverrideValue(true);
                    _queuedAnimationCts?.Cancel();
                    _animationCts?.Cancel();
                    ResetAnimatedProperties();
                    _animationHandle = null;
                }
                
                Visibility = visibility;
                SetActive(Visibility == WidgetVisibility.Visible);
                switch (Visibility)
                {
                    case WidgetVisibility.Visible:
                        OnShow(null);             
                        Shown?.Invoke(this);
                        OnShown();
                        break;
                    case WidgetVisibility.Hidden:
                        OnHide(null);             
                        Hidden?.Invoke(this);
                        OnHidden();
                        break;
                }
            }
            else if (IsAnimating)
            {
                SkipAnimation();
            }
        }

        public async Awaitable AnimateVisibility(WidgetVisibility visibility, InterruptBehavior interruptBehavior = InterruptBehavior.Immediate, 
            CancellationToken cancellationToken = default)
        {
            IAnimation defaultAnimation = GetDefaultAnimation(visibility);
            if (defaultAnimation == null)
            {
                throw new InvalidOperationException("Cannot animate visibility without default animation");
            }
            await AnimateVisibility(visibility, defaultAnimation.Playable(), interruptBehavior, cancellationToken);
        }

        public async Awaitable AnimateVisibility(WidgetVisibility visibility, AnimationPlaybackParams playbackParams, 
            InterruptBehavior interruptBehavior = InterruptBehavior.Immediate, CancellationToken cancellationToken = default)
        {
            IAnimation defaultAnimation = GetDefaultAnimation(visibility);
            if (defaultAnimation == null)
            {
                throw new InvalidOperationException("Cannot animate visibility without default animation");
            }
            await AnimateVisibility(visibility, defaultAnimation.Playable(in playbackParams), interruptBehavior, cancellationToken);
        }
        
        public async Awaitable AnimateVisibility(WidgetVisibility visibility, AnimationPlayable playable, 
            InterruptBehavior interruptBehavior = InterruptBehavior.Immediate, CancellationToken cancellationToken = default)
        {
            if (visibility != Visibility)
            {
                if (IsAnimating && interruptBehavior == InterruptBehavior.Ignore)
                    throw new OperationCanceledException();
                
                VisibilityAnimationHandle handle = null;
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                
                if (IsAnimating)
                {
                    VisibilityAnimationHandle currentHandle = _animationHandle;
                    _queuedAnimationCts?.Cancel();
                    if (interruptBehavior == InterruptBehavior.Queue)
                    {
                        _queuedAnimationCts = cts;
                        await WaitForAnimationComplete(currentHandle, cts.Token);
                    }
                    else
                    {
                        _animationCts?.Cancel();
                    }
                    
                    if (interruptBehavior == InterruptBehavior.Rewind)
                    {
                        handle = new VisibilityAnimationHandle(AnimationPlayer.Duplicate(currentHandle.AnimationPlayer));
                        handle.AnimationPlayer.Rewind();
                    }
                }
                else
                {
                    IsInteractableInternal.SetOverrideValue(false);
                }
                
                if(handle ==  null)
                {
                    AnimationPlayer player = AnimationPlayer.PlayAnimation(playable.Animation, playable.StartTime, playable.PlaybackMode, 
                        playable.EasingMode, playable.TimeMode, playable.PlaybackSpeed);
                    handle = new VisibilityAnimationHandle(player);
                }
                _animationCts = cts;
                _animationHandle = handle;
                
                Visibility = visibility;
                if (Visibility == WidgetVisibility.Visible)
                {
                    SetActive(true);   
                    OnShow(handle.AnimationPlayer.Data);
                }
                else
                {
                    OnHide(handle.AnimationPlayer.Data);
                }
                
                try
                {
                    await WaitForAnimationComplete(handle, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    handle.Complete();
                    throw;
                }
                
                ResetAnimatedProperties();
                IsInteractableInternal.SetOverrideValue(true);
                
                if (Visibility == WidgetVisibility.Visible)
                {
                    Shown?.Invoke(this);
                    OnShown();
                }
                else
                {
                    Hidden?.Invoke(this);
                    OnHidden();
                }
                handle.Complete();
            }
        }

        public abstract IAnimation GetDefaultAnimation(WidgetVisibility visibility);

        public void SkipAnimation()
        {
            if (IsAnimating)
            {
                _animationHandle.Complete();
            }
        }

        public virtual void ResetAnimatedProperties() { }

        public void SortAbove(IWidget target)
        {
            SortAgainst(target, 1);
        }

        public void SortBelow(IWidget target)
        {
            SortAgainst(target, -1);
        }

        public abstract void SetLocalSortOrder(int sortOrder);
        public abstract void SetGlobalSortOrder(int sortOrder);
        public abstract void SetRenderSortOrder(int sortOrder);
        public abstract void SetOpacity(float opacity);

        public virtual bool IsValidData(object data) { return false; }
        
        public virtual void SetData(object data) { }

        // Unity Messages
#if UNITY_EDITOR
        protected virtual void OnValidate() { }
#endif

        protected virtual void Awake()
        {
            if (transform.parent != null)
            {
                _parent = transform.parent.GetComponentInParent<TWidget>(true);
                if (Parent != null)
                {
                    _parent.AddChild(this as TWidget);
                }
            }
        }

        protected virtual void Start() { }

        protected virtual void OnDestroy()
        {
            if (_parent != null)
            {
                _parent.RemoveChild(this as TWidget);
            }
        }

        protected virtual void OnApplicationFocus(bool hasFocus) { }

        // WidgetBase
        protected abstract void SortAgainst(IWidget target, int direction);
        protected abstract void OnIsEnabledUpdated(bool value);
        protected abstract void OnIsInteractableUpdated(bool value);
        protected abstract void SetActive(bool active);
        
        protected virtual void OnInitialize() { }
        protected virtual void OnShow(AnimationPlayer.PlaybackData? animationPlaybackData) { }
        protected virtual void OnShown() { }

        protected virtual void OnHide(AnimationPlayer.PlaybackData? animationPlaybackData) { }
        protected virtual void OnHidden() { }
        protected virtual void OnTerminate() { }

        private void AddChild(TWidget child)
        {
            if (!_children.Contains(child))
            {
                _children.Add(child);
            }
        }

        private void RemoveChild(TWidget child)
        {
            if (_children.Contains(child))
            {
                _children.Remove(child);
            }
        }
        
        private async Awaitable WaitForAnimationComplete(VisibilityAnimationHandle handle, CancellationToken cancellationToken)
        {
            while (handle.AnimationPlayer.IsPlaying && !handle.ExecutionComplete && !cancellationToken.IsCancellationRequested)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}