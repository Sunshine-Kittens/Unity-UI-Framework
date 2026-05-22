using System;
using System.Collections.Generic;

using UIFramework.Collectors;
using UIFramework.Core;
using UIFramework.Core.Interfaces;
using UIFramework.Groups;

using UnityEngine.Extension;

namespace UIFramework.Controllers.Interfaces
{
    public interface IScreenGroupController : IUpdatable
    {
        public bool IsInitialized { get; }
        public InitializationState State { get; }

        public bool IsVisible { get; }
        public float Opacity { get; }

        public IReadOnlyList<IScreenGroup> Groups { get; }

        public IScalarFlag IsEnabled { get; }
        public IScalarFlag IsInteractable { get; }

        public TimeMode TimeMode { get; }

        public event Action Entering;
        public event Action Entered;
        public event Action Exiting;
        public event Action Exited;

        public IScreenGroup AddGroup(IEnumerable<WidgetCollector<IScreen>> collectors);

        public void Initialize();
        public void Terminate();

        public void SetOpacity(float opacity);
    }
}
