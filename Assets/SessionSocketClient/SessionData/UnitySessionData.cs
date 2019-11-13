using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace SessionSocketClient {
    [System.Serializable]
    public abstract class UnitySessionData : MonoBehaviour, ISessionData
    {
        public static bool DebugEnabled = false;

        [SerializeField]
        private string _id;

        private UnitySessionDataPermissions _permissions;

        public string Id { get { return _id; } }

        protected void Start()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Debug.LogException(new UnassignedReferenceException("SessionData must have an id assigned to it."), this);
                return;
            }

            _permissions = GetComponent<UnitySessionDataPermissions>();

            Init();

            // Load this session data with data json that is stored in SessionDataManager.
            string json = SessionDataManager.Instance.GetData(Id);

            if (json == null)
            {
                if (DebugEnabled) {
                    Debug.Log("[SessionData " + Id + "] Update Data From Local.");
                }

                UpdateDataFromLocal();
            }
            else
            {
                UpdateData(json);
            }
            
            // Hookup event listeners now that data is current.
            HookupEventListeners();
            
            // Listen for changes to our data coming in to the Session Data Manager.
            SessionDataManager.Instance.SubscribeToUpdates(this);
        }

        protected void OnDestroy()
        {
            // This component is being destroyed, no longer need to listen for changes to our data.
            SessionDataManager.Instance.UnsubscribeFromUpdates(this);
            UnhookEventListeners();
        }

        /// <summary>
        /// Update the state of data in this class with the provided json.
        /// </summary>
        public void UpdateData(string json)
        {
            if (_permissions != null && !_permissions.canReceiveUpdates) {
                // If permissions say that receiving updates is not allowed, then ignore this method call.
                return;
            }

            // Unhook from event listeners so that we dont get an infinite feedback loop when updating local ui elements with incoming data.
            UnhookEventListeners();

            JsonUtility.FromJsonOverwrite(json, this);
            if (DebugEnabled) {
                Debug.Log("[SessionData " + Id + "] Update Local From Data");
            }
            UpdateLocalFromData();

            // Hook event listeners back up now that the local ui elements have been updated.
            HookupEventListeners();
        }
        
        /// <summary>
        /// Save the current state of data in this class to the SessionDataManager.
        /// </summary>
        protected void SaveData()
        {
            if (_permissions != null && !_permissions.canSendUpdates) {
                // If permissions say that sending updates is not allowed, then ignore this method call.
                return;
            }

            var json = JsonUtility.ToJson(this);
            // Debug.Log("[SessionData " + id + "] Save Data:\n" + json);
            SessionDataManager.Instance.UpdateData(Id, json, true);
        }

        // These functions are really the only ones that need to be implemented in derived classes.
        // ===============
        //  1. Init -> Use to initialize the session data component with other components it is designed to watch.
        //  2. HookupEventListeners -> Subscribe to events the session data cares about to modify its own data.
        //  3. UnhookEventListeners -> Unsubscribe from events the session data is listening to.
        //  4. UpdateDataFromLocal -> Use to update the session data stored in this component from the other components it watches.
        //  5. UpdateLocalFromData -> Use to update the watched components with the data stored in this component.
        protected abstract void Init();
        protected abstract void HookupEventListeners();
        protected abstract void UnhookEventListeners();
        protected abstract void UpdateDataFromLocal();
        protected abstract void UpdateLocalFromData();
    }
}