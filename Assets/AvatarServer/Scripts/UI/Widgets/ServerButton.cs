using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ServerButton : MonoBehaviour {
    public string connectScreenName = "ServerConnectScreen";

    private Button _button;
    private ServerConnectScreen _serverConnectScreen;

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
        if (_serverConnectScreen == null) {
            _serverConnectScreen = UnityUtils.FindObjectOfType<ServerConnectScreen>(true);
        }
        
        if (_serverConnectScreen != null) {
            _serverConnectScreen.gameObject.SetActive(true);
        } else {
            Debug.LogError("[ServerButton] Could not find Server Connect Screen.");
        }
    }
}