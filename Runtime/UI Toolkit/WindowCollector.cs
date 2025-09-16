using System;

using UnityEngine;
using UnityEngine.Serialization;

namespace UIFramework.UIToolkit
{
    [RequireComponent(typeof(WidgetDocumentSource))]
    public class WindowCollector : UIFramework.WindowCollector
    {
        [SerializeField, FormerlySerializedAs("_collectableDefinitions")] private CollectableBehaviour<Window>[] _definitions = new CollectableBehaviour<Window>[0];
        private WidgetDocumentSource _widgetDocumentSource = null;
        private IWindow[] _cachedWindows = null;

        private WidgetDocumentSource GetUIBehaviourDocument()
        {
            if (_widgetDocumentSource == null)
            {
                _widgetDocumentSource = GetComponent<WidgetDocumentSource>();
            }
            return _widgetDocumentSource;
        }

        public override IWindow[] Collect()
        {
            if (_cachedWindows != null)
            {
                return _cachedWindows;
            }

            WidgetDocumentSource behaviourDocumentSource = GetUIBehaviourDocument();
            if (behaviourDocumentSource == null)
            {
                throw new InvalidOperationException("UIBehaviourDocument is null.");
            }

            if (_definitions == null)
            {
                throw new InvalidOperationException("collectableDefinitions is null");
            }

            _cachedWindows = new IWindow[_definitions.Length];
            for (int i = 0; i < _definitions.Length; i++)
            {
                IWindow window = _definitions[i].Type.CreateInstance(behaviourDocumentSource, _definitions[i].Identifier);
                _cachedWindows[i] = window;
            }
            return _cachedWindows;
        }
    }
}
