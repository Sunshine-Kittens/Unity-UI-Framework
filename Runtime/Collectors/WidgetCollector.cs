using System;
using System.Collections.Generic;

using UIFramework.Core.Interfaces;

using UnityEngine;

namespace UIFramework.Collectors
{
    public abstract class WidgetCollector<TWidget> : MonoBehaviour, IWidgetCollector<TWidget> where TWidget : class, IWidget
    {
        public Type WidgetType => typeof(TWidget);
        
        public abstract IEnumerable<TWidget> Collect();
    }
}
