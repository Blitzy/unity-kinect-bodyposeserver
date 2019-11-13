using System;
using UnityEngine;
using UnityEngine.Events;

namespace SessionSocketClient {
    public class ActionSocketEvent : ISocketEvent {
        private Action _action;

        public ActionSocketEvent(Action action) {
            _action = action;
        }

        public void Execute() {
            if (_action != null) {
                _action();
            }
        }
    }
}
