using System;
using System.Collections.Generic;

namespace UIFramework.Collectors
{
    public interface IWidgetCollector
    {
        Type WidgetType { get; } 
    }
    
    public interface IWidgetCollector<out TWidget> : IWidgetCollector where TWidget : IWidget
    {
        new IEnumerable<TWidget> Collect();
    }
}
