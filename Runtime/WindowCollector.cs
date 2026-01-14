using System.Collections.Generic;

using UnityEngine;

namespace UIFramework
{
    public sealed class WindowCollector : MonoBehaviour
    {
        private readonly List<IWindow> _windows = new List<IWindow>(); 
        
        public IEnumerable<IWindow> Collect()
        {
            GetComponentsInChildren(true, _windows);
            for (int i = 0; i < _windows.Count; i++)
            {
                if (_windows[i] is IScreen)
                {
                    _windows.RemoveAt(i);
                }
            }
            return _windows;
        }
    }
}
