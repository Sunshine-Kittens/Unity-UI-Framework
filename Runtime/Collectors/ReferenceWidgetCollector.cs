using System.Collections.Generic;

using UnityEngine;

namespace UIFramework.Collectors
{
    public abstract class ReferenceWidgetCollector<TWidget> : WidgetCollector<TWidget> where TWidget : Component, IWidget
    {
        [SerializeField] private List<TWidget> _widgetReferences = new();

        public override IEnumerable<TWidget> Collect()
        {
            return _widgetReferences;
        }
    }
}
