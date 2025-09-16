using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace UIFramework.UIToolkit
{
    [RequireComponent(typeof(UIDocument))]
    public class WidgetDocumentSource : MonoBehaviour
    {
        public UIDocument Document
        {
            get
            {
                if (_document == null)
                {
                    _document = GetComponent<UIDocument>();        
                }
                return _document;
            }
        }
        private UIDocument _document;

        public event Action Enabled;
        public event Action Disabled;
        public event Action Destroyed;

        private void OnEnable()
        {
            Enabled?.Invoke();
        }

        private void OnDisable()
        {
            Disabled?.Invoke();
        }

        private void OnDestroy()
        {
            Destroyed?.Invoke();
        }
    }
}
