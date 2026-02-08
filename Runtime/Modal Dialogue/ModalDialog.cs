using UIFramework.Core.Interfaces;

using UnityEngine;
using UnityEngine.Extension;

namespace UIFramework
{
    public abstract class ModalDialog : MonoBehaviour, IUpdatable
    {
        public bool Active => gameObject.activeInHierarchy;
        
        protected IWindow DefaultWindow { get; }

        public void ManagedUpdate()
        {
            
        }
    }
}