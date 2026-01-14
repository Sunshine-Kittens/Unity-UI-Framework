using System.Collections.Generic;

using UnityEngine;

namespace UIFramework
{
    public sealed class ScreenCollector : MonoBehaviour
    {
        private readonly List<IScreen> _screens = new List<IScreen>(); 
        
        public IEnumerable<IScreen> Collect()
        {
            GetComponentsInChildren(true, _screens);
            return _screens;
        }
    }
}
