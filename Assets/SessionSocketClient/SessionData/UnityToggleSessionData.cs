using UnityEngine;
using UnityEngine.UI;

namespace SessionSocketClient {
    [RequireComponent(typeof(Toggle))]
    [System.Serializable]
    public class UnityToggleSessionData : UnitySessionData
    {
        [SerializeField]
        [HideInInspector]
        private bool _isOn;

        private Toggle _toggle;

        protected override void Init()
        {
            _toggle = GetComponent<Toggle>();
        }

        protected override void HookupEventListeners()
        {
            _toggle.onValueChanged.AddListener(HandleToggleValueChanged);
        }

        protected override void UnhookEventListeners()
        {
            _toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }

        protected override void UpdateDataFromLocal()
        {
            _isOn = _toggle.isOn;
        }

        protected override void UpdateLocalFromData()
        {
            _toggle.isOn = _isOn;
        }

        private void HandleToggleValueChanged(bool isOn)
        {
            this._isOn = isOn;
            SaveData();
        }
    }
}