using System.Threading;

using UIFramework.Animation;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework.Core.Interfaces
{
    public enum WidgetState
    {
        Uninitialized,
        Initialized,
        Terminated
    }
    
    public enum WidgetVisibility
    {
        Visible,
        Hidden
    }
    
    public enum InterruptBehavior
    {
        Immediate,
        Queue,
        Rewind,
        Ignore
    }

    
    public delegate void WidgetAction(IWidget widget);
    
    public interface IReadOnlyWidget
    {
        public string Identifier { get; }
        
        public bool IsInitialized { get; }
        public WidgetState State { get; }
        
        public IWidget Parent { get; }
        public int ChildCount { get; }
        
        public WidgetVisibility Visibility { get; }
        public bool IsVisible { get; }
        public bool IsAnimating { get; }
        
        public int LocalSortOrder { get; }
        public int GlobalSortOrder { get; }
        public int RenderSortOrder { get; }
        public float Opacity { get; }
        
        IReadOnlyScalarFlag IsEnabled { get; }
        IReadOnlyScalarFlag IsInteractable { get; }

        public IReadOnlyWidget GetChildAt(int index);
        
        public bool IsValidData(object data);
    }

    public interface IWidget : IReadOnlyWidget
    {
        public new IScalarFlag IsEnabled { get; }
        public new IScalarFlag IsInteractable { get; }

        public event WidgetAction Initialized;
        public event WidgetAction Terminated;
        
        public event WidgetAction Showing;
        public event WidgetAction Shown;
        public event WidgetAction Hiding;
        public event WidgetAction Hidden;
        
        public void Initialize();
        public void Terminate();
        
        public new IWidget GetChildAt(int index);
        public void UpdateWidget(float deltaTime);
        
        public void SetVisibility(WidgetVisibility visibility);
        
        public bool IsVisibilityState(WidgetVisibility visibility, bool? isAnimating = null);
        
        public VisibilityAnimationBuilder AnimateVisibility(WidgetVisibility visibility);
        
        public Awaitable AnimateVisibility(WidgetVisibility visibility, AnimationPlayable playable, 
            InterruptBehavior interruptBehavior = InterruptBehavior.Immediate, CancellationToken cancellationToken = default);
        
        public IAnimation GetDefaultAnimation(WidgetVisibility visibility);
        public IAnimation GetGenericAnimation(GenericAnimation genericAnimation, WidgetVisibility visibility);
        
        public Awaitable SkipAnimation();
        public Awaitable RewindAnimation(CancellationToken cancellationToken = default);
        public void ResetAnimatedProperties();

        public void SortAbove(IWidget target);
        public void SortBelow(IWidget target);
        public void SortInlineWith(IWidget target);
        public void SetLocalSortOrder(int sortOrder);
        public void SetGlobalSortOrder(int sortOrder);
        public void SetRenderSortOrder(int sortOrder);
        public void SetOpacity(float opacity);
        
        public void SetData(object data);
    }
}
