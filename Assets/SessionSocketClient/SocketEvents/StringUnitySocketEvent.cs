using UnityEngine;
using UnityEngine.Events;

namespace SessionSocketClient {
    public class StringUnitySocketEvent : ISocketEvent {
        private StringEvent _stringEvent;
        private string _string;

        public StringUnitySocketEvent(StringEvent stringEvent, string str) {
            _stringEvent = stringEvent;
            _string = str;
        }

        public void Execute() {
            if (_stringEvent != null) {
                _stringEvent.Invoke(_string);
            }
        }
    }
}