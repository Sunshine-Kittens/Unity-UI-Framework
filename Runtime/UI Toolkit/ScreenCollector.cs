using System;

using UnityEngine;
using UnityEngine.Serialization;

namespace UIFramework.UIToolkit
{
    [RequireComponent(typeof(WidgetDocumentSource))]
    public class ScreenCollector : UIFramework.ScreenCollector
    {
        [SerializeField, FormerlySerializedAs("_collectableDefinitions")] private CollectableBehaviour<Screen>[] _definitions = new CollectableBehaviour<Screen>[0];
        private WidgetDocumentSource _widgetDocumentSource = null;
        private IScreen[] _cachedScreens = null;

        private WidgetDocumentSource GetUIBehaviourDocument()
        {
            if (_widgetDocumentSource == null)
            {
                _widgetDocumentSource = GetComponent<WidgetDocumentSource>();
            }
            return _widgetDocumentSource;
        }

        public override IScreen[] Collect()
        {
            if (_cachedScreens != null)
            {
                return _cachedScreens;
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

            _cachedScreens = new IScreen[_definitions.Length];
            for (int i = 0; i < _definitions.Length; i++)
            {
                IScreen screen = _definitions[i].Type.CreateInstance(behaviourDocumentSource, _definitions[i].Identifier);
                _cachedScreens[i] = screen;
            }
            return _cachedScreens;
        }
    }
}
