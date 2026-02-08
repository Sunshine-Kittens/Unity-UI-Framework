using System.Collections.Generic;

using UnityEngine;

namespace UIFramework.Collectors
{
    public abstract class HierarchyWidgetCollector<TWidget> : WidgetCollector<TWidget> where TWidget : class, IWidget
    {
        [SerializeField] protected bool _includeInactive = true;
        [SerializeField] protected Transform _customRoot;
        
        public override IEnumerable<TWidget> Collect()
        {
            Transform searchRoot = _customRoot ?? transform;
            return searchRoot.GetComponentsInChildren<TWidget>(_includeInactive);
        }
    }
}
