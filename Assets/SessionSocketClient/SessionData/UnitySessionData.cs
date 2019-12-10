using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace SessionSocketClient {
    [System.Serializable]
    public abstract class UnitySessionData : MonoBehaviour, ISessionData
    {
        public static bool DebugEnabled = false;

        [SerializeField]
        [Tooltip("This is the id of the session data. DO NOT CHANGE THIS FROM THE EDITOR INSPECTOR AT RUNTIME... Use the Id property instead.")]
        private string _id;

        private UnitySessionDataPermissions _permissions;
        private int _eventListenersState = -1;
        private bool _hasStarted;

        public string Id { 
            get {
                return _id;
            }
            set {
                _ChangeId(value);
            }
        }

        protected void Start() {
            if (string.IsNullOrEmpty(Id))
            {
                Debug.LogException(new UnassignedReferenceException("SessionData must have an id assigned to it."), this);
                return;
            }

            _permissions = GetComponent<UnitySessionDataPermissions>();

            Init();
            _IdInit();

            _hasStarted = true;
        }

        private void _IdInit() {
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
            _InternalEnableEventListeners(true);
            
            // Listen for changes to our data coming in to the Session Data Manager.
            SessionDataManager.Instance.SubscribeToUpdates(this);
        }

        private bool _ChangeId(string newId, bool force = false) {
            if (_id.Equals(newId) && !force) {
                // Id is the same, do nothing.
                return false;
            }

            if (_hasStarted) {
                // Need to unsubscribe from updates using old id.
                SessionDataManager.Instance.UnsubscribeFromUpdates(this);
                _InternalEnableEventListeners(false);
            }

            _id = newId;
            _IdInit();

            return true;
        }

        protected void OnDestroy() {
            // This component is being destroyed, no longer need to listen for changes to our data.
            SessionDataManager.Instance.UnsubscribeFromUpdates(this);
            _InternalEnableEventListeners(false);
        }

        /// <summary>
        /// Update the state of data in this class with the provided json.
        /// </summary>
        public void UpdateData(string json) {
            if (_permissions != null && !_permissions.canReceiveUpdates) {
                // If permissions say that receiving updates is not allowed, then ignore this method call.
                return;
            }

            // Unhook from event listeners so that we dont get an infinite feedback loop when updating local ui elements with incoming data.
            _InternalEnableEventListeners(false);

            JsonUtility.FromJsonOverwrite(json, this);
            if (DebugEnabled) {
                Debug.Log("[SessionData " + Id + "] Update Local From Data");
            }
            UpdateLocalFromData();

            // Hook event listeners back up now that the local ui elements have been updated.
            _InternalEnableEventListeners(true);
        }
        
        /// <summary>
        /// Save the current state of data in this class to the SessionDataManager.
        /// </summary>
        protected void SaveData() {
            if (_permissions != null && !_permissions.canSendUpdates) {
                // If permissions say that sending updates is not allowed, then ignore this method call.
                return;
            }

            var json = JsonUtility.ToJson(this);
            // Debug.Log("[SessionData " + id + "] Save Data:\n" + json);
            SessionDataManager.Instance.UpdateData(Id, json, true);
        }

        private void _InternalEnableEventListeners(bool enable) {
            if (enable) {   
                if (_permissions != null && !_permissions.canSendUpdates) {
                    // If permissions say that sending updates is not allowed, then don't enable event listeners.
                    return;
                }
                
                if (_eventListenersState == -1 || _eventListenersState == 0) {
                    HookupEventListeners();
                    _eventListenersState = 1;
                }
            } else {
                if (_eventListenersState == 1) {
                    UnhookEventListeners();
                    _eventListenersState = 0;
                }
            }
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