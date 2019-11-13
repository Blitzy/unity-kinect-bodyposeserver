using UnityEngine;
using SessionSocketClient;
using System.Net;

public class DemoController : MonoBehaviour {
    public string ip = "127.0.0.1";
    public int port = 5556;
    public bool debugSocketManager;

    private void Start() {
        _ConnectToServer();
    }

    private void Update() {
        SocketManager.DebugEnabled = debugSocketManager;
    }

    private void _ConnectToServer() {
        IPAddress address;
        if (!IPAddress.TryParse(ip, out address)) {
            Debug.LogError("Invalid ip address.");
            return;
        }

        // Listen for connected and connect timeout events.
        SocketManager.Instance.onConnected.AddListener(_OnConnected);
        SocketManager.Instance.onConnectTimeout.AddListener(_OnConnectTimeout);

        // Try to connect to the session socket server with the given ip address and port.
        SocketManager.Instance.Connect(address, port);
    }

    private void _OnConnectTimeout() {
        // Don't need to listen to these anymore.
        SocketManager.Instance.onConnected.RemoveListener(_OnConnected);
        SocketManager.Instance.onConnectTimeout.RemoveListener(_OnConnectTimeout);

        Debug.LogError("Server connect timeout.");
    }

    private void _OnConnected() {
        // Don't need to listen to these anymore.
        SocketManager.Instance.onConnected.RemoveListener(_OnConnected);
        SocketManager.Instance.onConnectTimeout.RemoveListener(_OnConnectTimeout);

        Debug.Log("Connected to server.");

        // Send session data request event.
        // This will ask the server for the full set of session data that it currently has in memory.
        // The session data manager will automatically receive this and update all ISessionData objects that are currently subscribed to it.
        SocketManager.Instance.SendSessionDataRequestEvent();
    }
}
