using UnityEngine;
using UnityEngine.Events;

namespace SessionSocketClient {
    public class SessionDataUpdateUnitySocketEvent : ISocketEvent {
        private SessionDataUpdateEvent _updateEvent;
        private SessionDataUpdateEventInfo _info;

        public SessionDataUpdateUnitySocketEvent(SessionDataUpdateEvent updateEvent, SessionDataUpdateEventInfo info) {
            _updateEvent = updateEvent;
            _info = info;
        }

        public void Execute() {
            if (_updateEvent != null) {
                _updateEvent.Invoke(_info);
            }
        }
    }
}