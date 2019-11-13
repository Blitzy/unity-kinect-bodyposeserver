using UnityEngine;

namespace SessionSocketClient {
    /// <summary>
    /// This class provides optional permissions to a UnitySessionData component.
    /// </summary>
    public class UnitySessionDataPermissions : MonoBehaviour {
        [Tooltip("Session Data can receive updates.")]
        public bool canReceiveUpdates = true;

        [Tooltip("Session Data can send updates.")]
        public bool canSendUpdates = true;
    }
}