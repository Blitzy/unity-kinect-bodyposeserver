using UnityEngine;
using UnityEditor;

namespace SessionSocketClient {
    public static class SessionSocketClientEditorUtils {

        [MenuItem("GameObject/Session Socket Client/Client", false, 10)]
        public static void CreateSocketManager() {
            var socketManager = GameObject.FindObjectOfType<SocketManager>();
            if (socketManager != null) {
                Debug.LogError("Socket Manager already exists, remove it if you want to create a new one with this command.", socketManager);
                return;
            }

            var socketHeartbeat = GameObject.FindObjectOfType<SocketHeartbeat>();
            if (socketHeartbeat != null) {
                Debug.LogError("Socket Heartbeat already exists, remove it if you want to create a new one with this command.", socketManager);
                return;
            }

            var sessionDataManager = GameObject.FindObjectOfType<SessionDataManager>();
            if (socketHeartbeat != null) {
                Debug.LogError("Session Data Manager already exists, remove it if you want to create a new one with this command.", socketManager);
                return;
            }
            
            var gameObject = new GameObject("SessionSocketClient");
            gameObject.AddComponent<SocketManager>();
            gameObject.AddComponent<SocketHeartbeat>();
            gameObject.AddComponent<SessionDataManager>();
        }
    }
}