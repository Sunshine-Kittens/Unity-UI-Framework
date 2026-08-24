using System;
using System.Collections.Generic;
using System.Threading;

using UIFramework.Animation;
using UIFramework.Core.Interfaces;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Core
{
    public abstract class WidgetBase<TWidget> : MonoBehaviour, IWidget where TWidget : WidgetBase<TWidget>
    {
        private sealed class VisibilityAnimationHandle
        {
            private static readonly Stack<VisibilityAnimationHandle> _pool = new();

            // Nested in a generic, so this pool is per closed TWidget — the name records which one. Uses
            // FullName because the backends both declare a type called Widget.
            static VisibilityAnimationHandle()
                => PoolRegistry.Register($"VisibilityAnimationHandle<{typeof(TWidget).FullName}>",
                    () => _pool.Count, () => _pool.Clear());

            public static VisibilityAnimationHandle Get(AnimationPlayer animationPlayer, CancellationToken cancellationToken)
            {
                VisibilityAnimationHandle handle = _pool.Count > 0 ? _pool.Pop() : new VisibilityAnimationHandle();
                handle.Initialize(animationPlayer, cancellationToken);
                return handle;
            }

            public AnimationPlayer.PlaybackData PlaybackData => _animationPlayer.Data;
            public Awaitable AnimationAwaitable => _animationCompletionSource.Awaitable;
            public Awaitable CompletedAwaitable => _completedCompletionSource.Awaitable;

            private readonly AwaitableCompletionSource _animationCompletionSource = new();
            private readonly AwaitableCompletionSource _completedCompletionSource = new();
            private AnimationPlayer _animationPlayer;
            private CancellationTokenRegistration _cancellationRegistration;
            private bool _isCanceled;
            private bool _isComplete;

            public bool IsComplete => _isComplete || _isCanceled;

            private VisibilityAnimationHandle() { }

            private void Initialize(AnimationPlayer animationPlayer, CancellationToken cancellationToken)
            {
                _animationPlayer = animationPlayer ?? throw new ArgumentNullException(nameof(animationPlayer));
                _isCanceled = false;
                _isComplete = false;
                _animationCompletionSource.Reset();
                _completedCompletionSource.Reset();
                _animationPlayer.OnComplete += OnAnimationComplete;
                _cancellationRegistration = cancellationToken.Register(CancelCompletionSource);
            }

            public void Release()
            {
                _cancellationRegistration.Dispose();
                _cancellationRegistration = default;
                _pool.Push(this);
            }

            private void CancelCompletionSource()
            {
                _animationCompletionSource.TrySetCanceled();
                _completedCompletionSource.TrySetCanceled();
            }

            // Every completion source below is set with Try*: the cancellation registration can complete both
            // sources at any point, and the non-Try setters throw on an already-settled source.
            private void OnAnimationComplete(IAnimation animation)
            {
                _animationCompletionSource.TrySetResult();
            }

            public void CancelAnimation()
            {
                if (_animationPlayer != null)
                {
                    _animationPlayer.OnComplete -= OnAnimationComplete;
                    if (_animationPlayer.IsPlaying)
                    {
                        _animationPlayer.Stop();
                        _animationCompletionSource.TrySetCanceled();
                    }
                    _animationPlayer.Release();
                    _animationPlayer = null;
                }
            }

            public void Cancel()
            {
                CancelAnimation();
                _isCanceled = true;
                _completedCompletionSource.TrySetCanceled();
            }

            public void CompleteAnimation()
            {
                if (_animationPlayer != null)
                {
                    _animationPlayer.OnComplete -= OnAnimationComplete;
                    if (_animationPlayer.IsPlaying)
                    {
                        _animationPlayer.Complete();
                        _animationCompletionSource.TrySetResult();
                    }
                    _animationPlayer.Release();
                    _animationPlayer = null;
                }
            }

            public void Complete()
            {
                CompleteAnimation();
                _isComplete = true;
                _completedCompletionSource.TrySetResult();
            }

            public AnimationPlayer DuplicateAnimationPlayer()
            {
                return _animationPlayer != null ? AnimationPlayer.Duplicate(_animationPlayer) : null;
            }
        }

        // IWidget
        [field: SerializeField] public string Identifier { get; private set; } = string.Empty;

        public bool IsInitialized => State == WidgetState.Initialized;
        public WidgetState State { get; private set; } = WidgetState.Uninitialized;

        public bool CanInitialize => State != WidgetState.Initialized;
        public bool CanTerminate => State == WidgetState.Initialized;

        public IWidget Parent => _parent;
        private TWidget _parent;

        public int ChildCount => _children.Count;

        public WidgetVisibility Visibility { get; private set; } = WidgetVisibility.Hidden;

        public bool IsVisible => Visibility == WidgetVisibility.Visible && Opacity > 0.0F;

        public bool IsAnimating => _animationHandle != null && !_animationHandle.IsComplete;

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

        public event WidgetAction Initialized;
        public event WidgetAction Terminating;
        public event WidgetAction Terminated;
        
        public event WidgetAction Showing;
        public event WidgetAction Shown;
        public event WidgetAction Hiding;
        public event WidgetAction Hidden;

        // WidgetBase
        private readonly List<TWidget> _children = new();
        private VisibilityAnimationHandle _animationHandle;
        private CancellationTokenSource _animationCts;
        private CancellationTokenSource _queuedAnimationCts;

        // IWidget
        // Not virtual: the state guard has to run before any backend work, so backends contribute through
        // AcquireResources/ReleaseResources rather than by wrapping this.
        public void Initialize()
        {
            if (!CanInitialize)
                throw new InvalidOperationException($"{GetType().Name} cannot initialize from {State}.");
            
            AcquireResources();
            _isEnabled.OnUpdate += OnIsEnabledUpdated;
            IsInteractableInternal.OnUpdate += OnIsInteractableUpdated;
            SetActive(false);
            Visibility = WidgetVisibility.Hidden;
            State = WidgetState.Initialized;
            for (int i = 0; i < ChildCount; i++)
            {
                // A child may already be live: collectors register nested widgets in their own right, and
                // nothing orders a parent ahead of its children.
                IWidget child = GetChildAt(i);
                if (child.CanInitialize)
                    child.Initialize();
            }
            OnInitialize();
            Initialized?.Invoke(this);
        }

        public void Terminate()
        {
            if (!CanTerminate)
                throw new InvalidOperationException($"{GetType().Name} cannot terminate from {State}.");
            
            // Ahead of the child cascade, so a parent's teardown brackets its children's.
            Terminating?.Invoke(this);
            for (int i = 0; i < ChildCount; i++)
            {
                IWidget child = GetChildAt(i);
                if (child.CanTerminate)
                    child.Terminate();
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
            IsInteractableInternal.Reset(true);
            // Last, so the teardown above still runs against live render handles.
            ReleaseResources();
            State = WidgetState.Terminated;
            OnTerminate();
            Terminated?.Invoke(this);
        }

        IReadOnlyWidget IReadOnlyWidget.GetChildAt(int index) => GetChildAt(index);

        public IWidget GetChildAt(int index)
        {
            if (!_children.IsValidIndex(index)) throw new ArgumentOutOfRangeException(nameof(index));
            return _children[index];
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _children.Count; i++)
            {
                TWidget child = _children[i];
                if (child.gameObject.activeInHierarchy)
                {
                    child.Tick(deltaTime);
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
                        Showing?.Invoke(this);
                        OnShow(null);
                        Shown?.Invoke(this);
                        OnShown();
                        break;
                    case WidgetVisibility.Hidden:
                        Hiding?.Invoke(this);
                        OnHide(null);
                        Hidden?.Invoke(this);
                        OnHidden();
                        break;
                }
            }
            else if (IsAnimating)
            {
                _ = SkipAnimation();
            }
        }

        public bool IsVisibilityState(WidgetVisibility visibility, bool? isAnimating = null)
        {
            bool animationStateMatch = !isAnimating.HasValue || isAnimating.Value == IsAnimating; 
            return visibility == Visibility && animationStateMatch;
        }

        public VisibilityAnimationBuilder AnimateVisibility(WidgetVisibility visibility)
        {
            return new VisibilityAnimationBuilder(this, visibility);
        }

        public async Awaitable AnimateVisibility(WidgetVisibility visibility, AnimationPlayable playable, InterruptBehavior interruptBehavior = InterruptBehavior.Immediate, CancellationToken cancellationToken = default)
        {
            if (visibility == Visibility) return;

            if (IsAnimating && interruptBehavior == InterruptBehavior.Ignore) throw new OperationCanceledException();

            VisibilityAnimationHandle handle = null;
            CancellationTokenSource cts = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : new CancellationTokenSource();

            if (IsAnimating)
            {
                VisibilityAnimationHandle currentHandle = _animationHandle;
                _queuedAnimationCts?.Cancel();
                if (interruptBehavior == InterruptBehavior.Queue)
                {
                    _queuedAnimationCts = cts;
                    await currentHandle.CompletedAwaitable;
                }
                else
                {
                    _animationCts?.Cancel();
                }

                if (interruptBehavior == InterruptBehavior.Rewind)
                {
                    AnimationPlayer animationPlayer = currentHandle.DuplicateAnimationPlayer();
                    animationPlayer.Rewind();
                    handle = VisibilityAnimationHandle.Get(animationPlayer, cts.Token);
                }
            }
            else
            {
                IsInteractableInternal.SetOverrideValue(false);
            }

            if (handle == null)
            {
                AnimationPlayer animationPlayer = AnimationPlayer.PlayAnimation(playable.Animation, playable.StartTime, playable.PlaybackMode, playable.EasingMode, playable.TimeMode, playable.PlaybackSpeed);
                handle = VisibilityAnimationHandle.Get(animationPlayer, cts.Token);
            }
            _animationCts = cts;
            _animationHandle = handle;

            // Check if this should happen before the play animation...
            Visibility = visibility;
            if (Visibility == WidgetVisibility.Visible)
            {
                SetActive(true);
                Showing?.Invoke(this);
                OnShow(handle.PlaybackData);
            }
            else
            {
                Hiding?.Invoke(this);
                OnHide(handle.PlaybackData);
            }

            try
            {
                await handle.AnimationAwaitable;
            }
            catch (OperationCanceledException)
            {
                // Cancelled, not finished: stop the animation rather than completing it, and hand the
                // handle back. The finally still restores interactivity and animated state.
                CancelAnimationHandle(handle);
                throw;
            }
            finally
            {
                ResetAnimatedProperties();
                IsInteractableInternal.SetOverrideValue(true);
            }

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
            CompleteAnimationHandle(handle);
        }
        
        public abstract IAnimation GetDefaultAnimation(WidgetVisibility visibility);
        public abstract IAnimation GetGenericAnimation(GenericAnimation genericAnimation, WidgetVisibility visibility);

        public async Awaitable SkipAnimation()
        {
            if (IsAnimating)
            {
                VisibilityAnimationHandle handle = _animationHandle;
                handle.CompleteAnimation();
                await handle.CompletedAwaitable;
            }
        }

        public async Awaitable RewindAnimation(CancellationToken cancellationToken = default)
        {
            if (IsAnimating)
            {
                WidgetVisibility inverse = Visibility == WidgetVisibility.Visible ? WidgetVisibility.Hidden : WidgetVisibility.Visible;
                await AnimateVisibility(inverse, default(AnimationPlayable), InterruptBehavior.Rewind, cancellationToken);
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

        public void SortInlineWith(IWidget target)
        {
            SortAgainst(target, 0);
        }

        public abstract void SetLocalSortOrder(int sortOrder);
        public abstract void SetGlobalSortOrder(int sortOrder);
        public abstract void SetRenderSortOrder(int sortOrder);
        public abstract void SetOpacity(float opacity);

        public virtual bool IsValidData(object data)
        {
            return false;
        }

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

        // The backend's handles onto the render system: uGUI's parent Canvas and root transform, UI Toolkit's
        // VisualElement. Acquired before the widget goes live and released as it comes down.
        protected abstract void AcquireResources();
        protected abstract void ReleaseResources();

        protected virtual void OnInitialize() { }

        protected virtual void OnUpdate(float deltaTime) { }

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

        private void CompleteAnimationHandle(VisibilityAnimationHandle handle)
        {
            handle.Complete();
            ReleaseAnimationHandle(handle);
        }

        private void CancelAnimationHandle(VisibilityAnimationHandle handle)
        {
            handle.Cancel();
            ReleaseAnimationHandle(handle);
        }

        // Clearing the active handle is what makes IsAnimating false again; releasing returns it to the pool.
        // Both must happen on every exit path, or the widget stays stuck animating and the handle leaks.
        private void ReleaseAnimationHandle(VisibilityAnimationHandle handle)
        {
            if (handle == _animationHandle)
                _animationHandle = null;
            handle.Release();
        }
    }
}