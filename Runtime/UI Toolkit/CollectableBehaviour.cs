using System;

using UnityEngine;
using UnityEngine.Extension;
using UnityEngine.Serialization;

namespace UIFramework.UIToolkit
{
    [Serializable]
    public struct CollectableBehaviour<BehaviourType> where BehaviourType : Widget
    {
        public string Identifier { get { return _identifier; } }
        public SubclassOf<BehaviourType> Type { get { return _type; } }

        [SerializeField, FormerlySerializedAs("_name")] private string _identifier;
        [SerializeField] private SubclassOf<BehaviourType> _type;

        public CollectableBehaviour(string name, SubclassOf<BehaviourType> type)
        {
            _identifier = name;
            _type = type;
        }
    }
}
