using UnityEngine;
using UnityEngine.UI;
using com.rfilkov.components;
using System;

[RequireComponent(typeof(Toggle))]
public class ToggleVerticalMovement : MonoBehaviour {
    public AvatarController avatarController;

    private Toggle _toggle;

    private void Start() {
        _toggle = GetComponent<Toggle>();
        _toggle.SetIsOnWithoutNotify(avatarController.verticalMovement);
        _toggle.onValueChanged.AddListener(_OnToggleValueChanged);
    }

    private void OnDestroy() {
        _toggle.onValueChanged.RemoveListener(_OnToggleValueChanged);
    }

    private void _OnToggleValueChanged(bool isOn) {
        avatarController.verticalMovement = isOn;
    }    
}