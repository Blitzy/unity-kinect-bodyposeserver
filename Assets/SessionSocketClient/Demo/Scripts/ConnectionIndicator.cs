using UnityEngine;
using UnityEngine.UI;
using SessionSocketClient;

public class ConnectionIndicator : MonoBehaviour {
    public Image lightIcon;
    public Text messageText;

    private void Update() {
        if (SocketManager.Instance == null) {
            return;
        }

        if (SocketManager.Instance.IsClientRunning && SocketManager.Instance.Connected) {
            // Connected.
            lightIcon.color = Color.green;
            messageText.text = "Connected";
        } else {
            // Not connected.
            lightIcon.color = Color.red;
            messageText.text = "Not connected.";
        }
    }
}