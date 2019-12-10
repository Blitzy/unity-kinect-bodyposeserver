using UnityEngine;
using UnityEngine.UI;
using com.rfilkov.components;
using System;

[RequireComponent(typeof(Toggle))]
public class ToggleGroundedFeet : MonoBehaviour {
    public StageAvatarController avatarController;

    private Toggle _toggle;

    private void Start() {
        if (avatarController == null) {
            avatarController = FindObjectOfType<StageAvatarController>();
        }
        _toggle = GetComponent<Toggle>();
        _toggle.SetIsOnWithoutNotify(avatarController.groundedFeet);
        _toggle.onValueChanged.AddListener(_OnToggleValueChanged);
    }

    private void OnDestroy() {
        _toggle.onValueChanged.RemoveListener(_OnToggleValueChanged);
    }

    private void _OnToggleValueChanged(bool isOn) {
        avatarController.groundedFeet = isOn;
    }    
}