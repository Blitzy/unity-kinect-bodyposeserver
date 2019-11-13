using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Net.Sockets;
using System.Text;
using System.Net;

namespace SessionSocketClient {

    [Serializable]
    public class StringEvent : UnityEvent<string> {
    }

    [Serializable]
    public class ExceptionEvent : UnityEvent<Exception> {
    }

    [Serializable]
    public class SessionDataUpdateEvent : UnityEvent<SessionDataUpdateEventInfo> {
    }

    public class SocketManager : MonoBehaviour
    {
        public const string Version = "0.8.0";
        public static bool DebugEnabled = false;

        public static SocketManager Instance { get; private set; }

        private const float ConnectTimeoutLength = 5.0f;

        private UdpClient _client;
        private IPEndPoint _hostEndPoint;
        private IPAddress _serverIp;
        private Queue<ISocketEvent> _socketEventQueue = new Queue<ISocketEvent>();

        public UnityEvent onConnected = new UnityEvent();
        public UnityEvent onDisconnected = new UnityEvent();
        public UnityEvent onConnectTimeout = new UnityEvent();
        public StringEvent onEventReceived = new StringEvent();
        public StringEvent onSessionDataGetReceived = new StringEvent();
        public SessionDataUpdateEvent onSessionDataUpdateReceived = new SessionDataUpdateEvent();
        public ExceptionEvent onSocketException = new ExceptionEvent();

        /// <summary>
        /// Wether or not the network client connected to the server.
        /// </summary>
        public bool IsClientRunning
        {
            get
            {
                if (_client != null && _client.Client != null)
                    return _client.Client.Connected;
                else
                    return false;
            }
        }

        public string ClientIPAddress
        {
            get
            {
                return _client.Client.LocalEndPoint.ToString();
            }
        }

        /// <summary>
        /// Did we receive server connect response from the server?
        /// </summary>
        public bool Connected { get; private set; }

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            }

            Debug.Log("[SocketManager] Version: " + Version);

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("[SocketManager] Device is Offline");
            }
        }

        public void Connect(IPAddress address, int port)
        {
            if (IsClientRunning) {
                Debug.LogWarning("[SocketManager] Already connected to a server. Disconnect from current server before calling connect.");
                return;
            }
            
            Debug.Log("[SocketManager] Connecting");

            _client = new UdpClient();
            _client.ExclusiveAddressUse = false;
            _client.EnableBroadcast = true;
            
            _hostEndPoint = new IPEndPoint(address, port);
            Debug.Log("[SocketManager] Server IP: " + _hostEndPoint);

            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.Connect(_hostEndPoint);
            Debug.Log("[SocketManager] Client IP: " + ClientIPAddress);

            _client.BeginReceive(new AsyncCallback(ProcessDgram), _client);

            // Send JOIN message to server and wait for it to respond back.
            SendDgram("JOIN");
            
            // Invoke the connection timeout method in specified amount of time.
            // This invokation will be canceled if connection becomes established.
            Invoke("OnConnectTimeout", ConnectTimeoutLength);
        }

        public void Disconnect()
        {
            Debug.Log("[SocketManager] Disconnecting");

            // Send QUIT message to server.
            SendDgram("QUIT");

            if (_client != null) {
                _client.Close();
                _client = null;
            }

            Connected = false;
        }

        private void OnConnected() {
            // Cancel timeout invokation.
            CancelInvoke("OnConnectTimeout");

            Debug.Log("[SocketManager] Connected");

            Connected = true;

            if (onConnected != null) {
                onConnected.Invoke();
            }
        }

        private void OnConnectTimeout() {
            Debug.Log("[SocketManager] Connect timeout");
            Disconnect();

            if (onConnectTimeout != null) {
                onConnectTimeout.Invoke();
            }
        }

        void OnDisconnected() {
            Debug.Log("[SocketManager] Disconnected");
            Connected = false;

            if (onDisconnected != null) {
                onDisconnected.Invoke();
            }
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        /// <summary>
        /// Send server keep alive message, this tells the server we are still connceted and resets the timer on the server
        /// </summary>
        public void SendKeepAlive()
        {
            SendDgram("KEEP");
        }

        public void SendSessionDataUpdateEvent(SessionDataUpdateEventInfo sdatEventInfo)
        {
            string jsonString = JsonUtility.ToJson(sdatEventInfo);
            string message = "UDAT" + jsonString;
            SendDgram(message);

            if (DebugEnabled) {
                Debug.Log("[SocketManager] Sending Session Data Event: " + jsonString);
            }
        }

        public void SendSessionDataRequestEvent()
        {
            SendDgram("RDAT");
        }

        private void SendDgram(string msg)
        {
            if (_client != null)
            {
                byte[] dgram = Encoding.UTF8.GetBytes(msg);
                _client.Send(dgram, dgram.Length);
            }
        }

        private void OnSendEnd(IAsyncResult res)
        {
            _client.EndSend(res);
        }

        private void ProcessDgram(IAsyncResult res)
        {
            try
            {
                byte[] recieved = _client.EndReceive(res, ref _hostEndPoint);

                if (DebugEnabled) {
                    Debug.Log("[SocketManager] recieved: " + Encoding.UTF8.GetString(recieved));
                }

                string packet = Encoding.UTF8.GetString(recieved);

                string packetType = packet.Substring(0, 4);
                string packetData = packet.Remove(0, 4);

                switch (packetType) 
                {
                    case "EVNT":
                        lock(_socketEventQueue) {
                            ISocketEvent socketEvent = new StringUnitySocketEvent(onEventReceived, packetData);
                            _socketEventQueue.Enqueue(socketEvent);
                        }
                        break;
                    case "UDAT":
                        lock(_socketEventQueue) {
                            SessionDataUpdateEventInfo info = JsonUtility.FromJson<SessionDataUpdateEventInfo>(packetData);
                            ISocketEvent socketEvent = new SessionDataUpdateUnitySocketEvent(onSessionDataUpdateReceived, info);
                            _socketEventQueue.Enqueue(socketEvent);
                        }
                        break;
                    case "GDAT":
                        lock(_socketEventQueue) {
                            ISocketEvent socketEvent = new StringUnitySocketEvent(onSessionDataGetReceived, packetData);
                            _socketEventQueue.Enqueue(socketEvent);
                        }
                        break;
                    case "JRES":
                        lock(_socketEventQueue) {
                            ISocketEvent socketEvent = new ActionSocketEvent(OnConnected);
                            _socketEventQueue.Enqueue(socketEvent);
                        }
                        break;
                    case "QUIT":
                        lock(_socketEventQueue) {
                            ISocketEvent socketEvent = new ActionSocketEvent(OnDisconnected);
                            _socketEventQueue.Enqueue(socketEvent);
                        }
                        break;
                    default:
                        Debug.Log("[SocketManager] Recieved Unknown Message: /n"+ packet);
                        break;

                }
                _client.BeginReceive(new AsyncCallback(ProcessDgram), _client);

            }
            catch (Exception ex)
            {
                lock(_socketEventQueue) {
                    ISocketEvent socketEvent = new ExceptionUnitySocketEvent(onSocketException, ex);
                    _socketEventQueue.Enqueue(socketEvent);
                }
            }
        }

        private void Update()
        {
            lock (_socketEventQueue) {
                while(_socketEventQueue.Count != 0) {
                    ISocketEvent socketEvent = _socketEventQueue.Dequeue();
                    socketEvent.Execute();
                }
            }
        }
    }
}