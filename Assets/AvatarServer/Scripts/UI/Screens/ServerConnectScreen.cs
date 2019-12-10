using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System;

public class ServerConnectScreen : MonoBehaviour {
    
    public GameObject connectingIndicator;
    public Text feedbackText;
    public Button connectButton;
    public Button disconnectButton;
    public Button cancelButton;
    public Button configButton;

    private ConnectState _curState;
    
    private enum ConnectState {
        Disconnected,
        Connected,
        Connecting,
        ConnectTimeout,
        InvalidAddress
    }
    
    private void OnEnable() {
        AvatarServerManager.Instance.serverConfig.onServerConfigrationSet += _OnServerConfigurationSet;

        // Run the event handler for server configuartion set manually to set up initial state of screen.
        _OnServerConfigurationSet(AvatarServerManager.Instance.serverConfig);
    }

    private void OnDisable() {
        if (AvatarServerManager.Instance != null) {
            AvatarServerManager.Instance.serverConfig.onServerConfigrationSet -= _OnServerConfigurationSet;

            AvatarServerManager.Instance.socketManager.onConnected.RemoveListener(_OnConnected);
            AvatarServerManager.Instance.socketManager.onConnectTimeout.RemoveListener(_OnConnectTimeout);
        }
    }

    public void Connect() {
        if (AvatarServerManager.Instance.serverConfig.IsConfigured) {
            Disconnect();

            _curState = ConnectState.Connecting;
            _UpdateScreenConfiguration();
            
            IPAddress address = AvatarServerManager.Instance.serverConfig.Address;
            int port = AvatarServerManager.Instance.serverConfig.Port;

            AvatarServerManager.Instance.socketManager.onConnected.AddListener(_OnConnected);
            AvatarServerManager.Instance.socketManager.onConnectTimeout.AddListener(_OnConnectTimeout);
            
            AvatarServerManager.Instance.socketManager.Connect(address, port);
        } else {
            _curState = ConnectState.InvalidAddress;
            _UpdateScreenConfiguration();
        }
    }

    public void Disconnect() {
        AvatarServerManager.Instance.socketManager.Disconnect();

        _curState = ConnectState.Disconnected;
        _UpdateScreenConfiguration();
    }

    public void Cancel() {
        if (_curState == ConnectState.Connecting) {
            Disconnect();
        } else {
            gameObject.SetActive(false);
        }
    }

    private void _OnServerConfigurationSet(ServerConfiguration serverConfig) {
        var socketManager = AvatarServerManager.Instance.socketManager;
        if (socketManager.IsClientRunning) {
            if (socketManager.Connected) {
                _curState = ConnectState.Connected;
            } else {
                _curState = ConnectState.Connecting;
            }
        } else {
            _curState = ConnectState.Disconnected;
        }

        _UpdateScreenConfiguration();
    }

    private void _OnConnected() {
        _curState = ConnectState.Connected;
        _UpdateScreenConfiguration();
        
        AvatarServerManager.Instance.socketManager.onConnected.RemoveListener(_OnConnected);
        AvatarServerManager.Instance.socketManager.onConnectTimeout.RemoveListener(_OnConnectTimeout);

        gameObject.SetActive(false);
    }

    private void _OnConnectTimeout() {
        _curState = ConnectState.ConnectTimeout;
        _UpdateScreenConfiguration();
        
        AvatarServerManager.Instance.socketManager.onConnected.RemoveListener(_OnConnected);
        AvatarServerManager.Instance.socketManager.onConnectTimeout.RemoveListener(_OnConnectTimeout);

        connectButton.gameObject.SetActive(true);
        connectingIndicator.SetActive(false);
    }

    private void _UpdateScreenConfiguration() {
        if (_curState == ConnectState.Connected) {

            IPAddress address = AvatarServerManager.Instance.serverConfig.Address;
            int port = AvatarServerManager.Instance.serverConfig.Port;
            
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = string.Format("Connected to {0}:{1}", address.ToString(), port);

            connectButton.gameObject.SetActive(false);
            disconnectButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(true);
            configButton.gameObject.SetActive(false);
            connectingIndicator.SetActive(false);

        } else if (_curState == ConnectState.Disconnected) {
            
            feedbackText.gameObject.SetActive(true);

            if (AvatarServerManager.Instance.serverConfig.IsConfigured) {
                IPAddress address = AvatarServerManager.Instance.serverConfig.Address;
                int port = AvatarServerManager.Instance.serverConfig.Port;

                feedbackText.gameObject.SetActive(true);
                feedbackText.text = string.Format("Connect to {0}:{1}?", address.ToString(), port);
                
                connectButton.gameObject.SetActive(true);
                disconnectButton.gameObject.SetActive(false);
                cancelButton.gameObject.SetActive(true);
                configButton.gameObject.SetActive(true);
                connectingIndicator.SetActive(false);
            } else {
                feedbackText.gameObject.SetActive(true);
                feedbackText.text = string.Format("Please configure your server address.");

                connectButton.gameObject.SetActive(false);
                disconnectButton.gameObject.SetActive(false);
                cancelButton.gameObject.SetActive(true);
                configButton.gameObject.SetActive(true);
                connectingIndicator.SetActive(false);
            }

        } else if (_curState == ConnectState.Connecting) {

            IPAddress address = AvatarServerManager.Instance.serverConfig.Address;
            int port = AvatarServerManager.Instance.serverConfig.Port;
            
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = string.Format("Connecting to {0}:{1}", address.ToString(), port);

            connectButton.gameObject.SetActive(false);
            disconnectButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(true);
            configButton.gameObject.SetActive(false);
            connectingIndicator.SetActive(true);
            
        } else if (_curState == ConnectState.ConnectTimeout) {
        
            IPAddress address = AvatarServerManager.Instance.serverConfig.Address;
            int port = AvatarServerManager.Instance.serverConfig.Port;
            
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = string.Format("Could not connect to {0}:{1}", address.ToString(), port);

            connectButton.gameObject.SetActive(true);
            disconnectButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(true);
            configButton.gameObject.SetActive(true);
            connectingIndicator.SetActive(false);

        } else if (_curState == ConnectState.InvalidAddress) {
            
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = "Invalid server address.\nNeed to configure the server address.";

            connectButton.gameObject.SetActive(false);
            disconnectButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(true);
            configButton.gameObject.SetActive(true);
            connectingIndicator.SetActive(false);
            
        } else {

            Debug.LogError("[ServerConnectScreen] Connect state " + _curState.ToString() + " is not implemented.");

        }
    }
}