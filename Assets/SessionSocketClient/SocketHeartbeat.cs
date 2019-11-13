using System;
using UnityEngine;

namespace SessionSocketClient {
    public class SocketHeartbeat : MonoBehaviour
    {
        public float heartbeatInterval = 2.0f;

        private bool _sendHeartbeats;
        private float _timer;
        
        private void Start()
        {
            SocketManager.Instance.onConnected.AddListener(HandleConnected);
            SocketManager.Instance.onDisconnected.AddListener(HandleDisconnected);
            SocketManager.Instance.onSocketException.AddListener(HandleSocketException);
        }

        private void OnDestroy()
        {
            SocketManager.Instance.onConnected.RemoveListener(HandleConnected);
            SocketManager.Instance.onDisconnected.RemoveListener(HandleDisconnected);
            SocketManager.Instance.onSocketException.RemoveListener(HandleSocketException);
        }

        private void Update()
        {
            if (_sendHeartbeats)
            {
                _timer += Time.deltaTime;

                if (_timer >= heartbeatInterval)
                {
                    SocketManager.Instance.SendKeepAlive();
                    _timer = 0.0f;
                }
            }
        }

        private void EnableHeartbeats(bool enable) {
            if (_sendHeartbeats != enable) {
                _sendHeartbeats = enable;

                if (enable) {
                    Debug.Log("[SocketHeartbeat] Start sending heartbeats");
                } else {
                    Debug.Log("[SocketHeartbeat] Stop sending heartbeats");
                }
            }
        }
        
        private void HandleConnected()
        {
            EnableHeartbeats(true);
        }

        private void HandleSocketException(Exception e)
        {
            EnableHeartbeats(false);
        }

        private void HandleDisconnected()
        {
            EnableHeartbeats(false);
        }
    }
}
