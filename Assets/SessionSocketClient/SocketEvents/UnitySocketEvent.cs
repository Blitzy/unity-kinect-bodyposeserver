using UnityEngine;
using UnityEngine.Events;

namespace SessionSocketClient {
    public class UnitySocketEvent : ISocketEvent {
        private UnityEvent _unityEvent;

        public UnitySocketEvent(UnityEvent unityEvent) {
            _unityEvent = unityEvent;
        }

        public void Execute() {
            if (_unityEvent != null) {
                _unityEvent.Invoke();
            }
        }
    }
}
