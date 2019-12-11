using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using SessionSocketClient;

public class AvatarServerManager : MonoBehaviour {
    
    public static AvatarServerManager Instance { get; private set; }

    public GameObject hudMain;
    public GameObject hudStageAvatar;
    public ServerConfiguration serverConfig;
    public ServerConnectScreen serverConnectScreen;
    public ServerConfigScreen serverConfigScreen;

    public DeviceConsole DeviceConsole { get; private set; }

    [SerializeField]
    private DeviceConsole _deviceConsolePrefab;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }

        if (_deviceConsolePrefab != null) {
            DeviceConsole = GameObject.Instantiate(_deviceConsolePrefab);
            DeviceConsole.transform.SetParent(transform);
            DeviceConsole.SetVisible(false);

            AppConsoleCommands.Setup();
        }

        serverConfig = new ServerConfiguration();

        hudMain.gameObject.SetActive(true);
        hudStageAvatar.gameObject.SetActive(true);
        serverConfigScreen.gameObject.SetActive(false);
        serverConnectScreen.gameObject.SetActive(false);
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Start() {

    }

    private void Update() {
    }
}
