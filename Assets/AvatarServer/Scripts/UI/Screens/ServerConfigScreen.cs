using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Net;

public class ServerConfigScreen : MonoBehaviour {

    public Text feedbackText;
    public InputField inputField;

    private void OnEnable() {
        var serverConfig = AvatarServerManager.Instance.serverConfig;
        if (serverConfig.IsConfigured) {
            inputField.text = string.Format("{0}:{1}", serverConfig.Address.ToString(), serverConfig.Port);
        } else {
            inputField.text = "";
        }
        
        feedbackText.gameObject.SetActive(false);
    }

    private void OnDisable() {
    }

    public void Submit() {
        if (inputField.text.Contains(":")) {
            string[] ipSplit = inputField.text.Split(':');
            if (ipSplit.Length == 2) {
                // Verify that we've got an ip address.
                IPAddress address;
                if (IPAddress.TryParse(ipSplit[0], out address)) {
                    int port;
                    if (int.TryParse(ipSplit[1], out port)) {
                        // Save address and close screen.
                        AvatarServerManager.Instance.serverConfig.SetAndSave(address, port);
                        gameObject.SetActive(false);
                    } else {
                        feedbackText.gameObject.SetActive(true);
                        feedbackText.text = "Invalid ip address port.";
                    }
                } else {
                    feedbackText.gameObject.SetActive(true);
                    feedbackText.text = "Invalid ip address format.";
                }
            } else {
                feedbackText.gameObject.SetActive(true);
                feedbackText.text = "Invalid ip address format.";
            }
        } else { 
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = "Server address needs a port number.";
        }
    }

    public void Cancel() {
        // Close screen.
        gameObject.SetActive(false);
    }
}