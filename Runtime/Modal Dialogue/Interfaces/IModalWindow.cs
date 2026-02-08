using System.Collections.Generic;

using UIFramework.Core.Interfaces;

using UnityEngine;
using UnityEngine.Events;

namespace UIFramework
{
    public interface IModalWindow : IWindow
    {
        public struct ButtonAction
        {
            public string Label { get; private set; }
            public UnityAction Action { get; private set; }

            public ButtonAction(string label, UnityAction action)
            {
                this.Label = label;
                this.Action = action;
            }
        }

        public struct Data
        {
            public string Message { get; private set; }
            public List<ButtonAction> Actions { get; private set; }
            public Color? BackgroundColor { get; private set; }

            public Data(string message, List<ButtonAction> actions, Color? backgroundColor)
            {
                this.Message = message;
                this.Actions = actions;
                this.BackgroundColor = backgroundColor;
            }

            public Data(string message, List<ButtonAction> actions)
            {
                this.Message = message;
                this.Actions = actions;
                this.BackgroundColor = null;
            }
        }

        void Init(Data data);
    }
}