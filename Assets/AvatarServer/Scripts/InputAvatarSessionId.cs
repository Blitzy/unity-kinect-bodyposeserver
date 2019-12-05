using UnityEngine;
using UnityEngine.UI;
using SessionSocketClient;
using System;

[RequireComponent(typeof(InputField))]
public class InputAvatarSessionId : MonoBehaviour {
    public UnityKinectAvatarSessionData avatarSessionData;

    private InputField _input;

    private void Start() {
        _input = GetComponent<InputField>();
        _input.SetTextWithoutNotify(avatarSessionData.Id);
        _input.onEndEdit.AddListener(_OnEndEdit);
    }

    private void OnDestroy() {
        _input.onEndEdit.RemoveListener(_OnEndEdit);
    }

    private void _OnEndEdit(string inputValue) {
        if (string.IsNullOrEmpty(inputValue)) {
            // Reset the input field back to the current session data id.
            _input.SetTextWithoutNotify(avatarSessionData.Id);
        } else {
            // Set the session data id to the new input field value.
            avatarSessionData.Id = _input.text;
        }
    } 
}