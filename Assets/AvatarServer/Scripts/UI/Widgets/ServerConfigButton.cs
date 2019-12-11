using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ServerConfigButton : MonoBehaviour {
    public string configScreenName = "ServerConnectScreen";

    private Button _button;
    private ServerConfigScreen _serverConfigScreen;

    private void Awake() {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(_OnButtonClick);
    }

    private void OnDestroy() {
        if (_button != null) {
            _button.onClick.RemoveListener(_OnButtonClick);
        }
    }

    private void _OnButtonClick() {
        if (_serverConfigScreen == null) {
            _serverConfigScreen = UnityUtils.FindObjectOfType<ServerConfigScreen>(true);
        }
        
        if (_serverConfigScreen != null) {
            _serverConfigScreen.gameObject.SetActive(true);
        } else {
            Debug.LogError("[ServerConfigButton] Could not find Server Config Screen.");
        }
    }
}