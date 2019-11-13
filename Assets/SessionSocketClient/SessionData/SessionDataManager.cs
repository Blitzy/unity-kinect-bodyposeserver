using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace SessionSocketClient {
    public class SessionDataManager : MonoBehaviour
    {
        public static SessionDataManager Instance { get; private set; }

        public static bool DebugEnabled = false;

        /// <summary>
        /// A dictionary of Session Datas in JSON format.
        /// </summary>
        /// <typeparam name="string">Key: Session Data Id</typeparam>
        /// <typeparam name="string">Value: Session Data JSON</typeparam>
        private Dictionary<string, string> _dataDict = new Dictionary<string, string>();

        private Dictionary<string, ISessionData> _activeDataInterfaces = new Dictionary<string, ISessionData>();

        public UnityEvent onSessionDataLoaded;

        private void Awake()
        {
            if (Instance == null) {
                Instance = this;
            }
        }

        private void Start() {
            SocketManager.Instance.onSessionDataGetReceived.AddListener(HandleSessionDataGetEvent);
            SocketManager.Instance.onSessionDataUpdateReceived.AddListener(HandleSessionDataUpdateEvent);
        }

        private void OnDestroy()
        {
            if (SocketManager.Instance != null) {
                SocketManager.Instance.onSessionDataGetReceived.RemoveListener(HandleSessionDataGetEvent);
                SocketManager.Instance.onSessionDataUpdateReceived.RemoveListener(HandleSessionDataUpdateEvent);
            }
        }

        /// <summary>
        /// Update session data with provided JSON.
        /// </summary>
        /// <param name="sessionDataId">Id of the session data.</param>
        /// <param name="sessionDataJSON">JSON of the session data.</param>
        /// <param name="fromLocal">Wether or not this call is from the local machine or not. If from local, we ignore SessionData.OnDataUpdated callback.</param>
        public void UpdateData(string sessionDataId, string sessionDataJSON, bool fromLocal)
        {
            if (DebugEnabled) {
                Debug.Log("[SessionDataManager] UpdateData (fromLocal: " + fromLocal +") -> " + sessionDataJSON);
            }

            _dataDict[sessionDataId] = sessionDataJSON;

            if (!fromLocal)
            {
                // Inform active session data component of updated data.
                ISessionData sessionData;
                if (_activeDataInterfaces.TryGetValue(sessionDataId, out sessionData))
                {
                    sessionData.UpdateData(sessionDataJSON);
                }
            }
            else
            {
                // Send data through socket server.
                if (SocketManager.Instance.IsClientRunning)
                {
                    var sender = SocketManager.Instance.ClientIPAddress;
                    var eventInfo = new SessionDataUpdateEventInfo(sender, sessionDataId, sessionDataJSON);
                    SocketManager.Instance.SendSessionDataUpdateEvent(eventInfo);
                }
            }
        }

        public void RemoveData(string sessionDataId)
        {
            _dataDict.Remove(sessionDataId);
        }
        
        public void RemoveAllData()
        {
            _dataDict = new Dictionary<string, string>();
        }

        public string GetData(string id)
        {
            string json;
            if (_dataDict.TryGetValue(id, out json))
            {
                return json;
            }
            else
            {
                return null;
            }
        }

        public void SubscribeToUpdates(ISessionData sessionData)
        {
            if (_activeDataInterfaces.ContainsKey(sessionData.Id))
            {
                Debug.LogError("SessionData with id '" + sessionData.Id + "' is already active.");
                return;
            }
            _activeDataInterfaces.Add(sessionData.Id, sessionData);
        }

        public void UnsubscribeFromUpdates(ISessionData sessionData)
        {
            _activeDataInterfaces.Remove(sessionData.Id);
        }

        private void HandleSessionDataUpdateEvent(SessionDataUpdateEventInfo udatInfo)
        {
            if (DebugEnabled) {
                Debug.Log("[SessionDataManager] Session Data Update Event Recieved Json:\n" + udatInfo.sessionDataJson);
            }

            UpdateData(udatInfo.sessionDataId, udatInfo.sessionDataJson, false);
        }

        private void HandleSessionDataGetEvent(string gdatJson)
        {
            if (DebugEnabled) {
                Debug.Log("[SessionDataManager] Session Data Get Event Recieved Json:\n" + gdatJson);
            }
            
            // Get data event is a complete overwrite of session data.
            // Remove all existing data.
            RemoveAllData();

            Dictionary<string, object> dict = (Dictionary<string, object>)MiniJSON.Json.Deserialize(gdatJson);
            foreach(var kvp in dict)
            {
                string datJson = MiniJSON.Json.Serialize(kvp.Value);
                datJson = FixJSONFormatting(datJson);
                // Debug.Log("key: " + kvp.Key + "\nvalue: " + datJson);
                UpdateData(kvp.Key, datJson, false);
            }

            if (onSessionDataLoaded != null) {
                onSessionDataLoaded.Invoke();
            }
        }

        private string FixJSONFormatting(string json)
        {
            // HACKY HACK HACK: Sometimes the json string we receive is not properly formatted.
            // if the json beging with "{ then we will run a simple linear process of removing artifacts.
            if (json.StartsWith("\"{"))
            {
                json = json.Replace(@"\", ""); // Remove all escape chars
                json = json.Remove(0, 1); // Remove first quote char
                json = json.Remove(json.Length - 1, 1); // Remove last quote char
            }

            return json;
        }
    }
}