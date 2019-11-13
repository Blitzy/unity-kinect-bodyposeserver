using System;
using UnityEngine;
using UnityEngine.Events;

namespace SessionSocketClient {
    public class ExceptionUnitySocketEvent : ISocketEvent {
        private ExceptionEvent _exceptionEvent;
        private Exception _exception;

        public ExceptionUnitySocketEvent(ExceptionEvent exceptionEvent, Exception exception) {
            _exceptionEvent = exceptionEvent;
            _exception = exception;
        }

        public void Execute() {
            if (_exceptionEvent != null) {
                _exceptionEvent.Invoke(_exception);
            }
        }
    }
}