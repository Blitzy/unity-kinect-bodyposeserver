using UnityEngine;
using UnityEngine.UI;

namespace SessionSocketClient {
    [RequireComponent(typeof(Slider))]
    [System.Serializable]
    public class UnitySliderSessionData : UnitySessionData
    {
        [SerializeField]
        [HideInInspector]
        private float _value;

        private Slider _slider;

        protected override void Init()
        {
            _slider = GetComponent<Slider>();
        }

        protected override void HookupEventListeners()
        {
            _slider.onValueChanged.AddListener(HandleSliderValueChanged);
        }

        protected override void UnhookEventListeners()
        {
            _slider.onValueChanged.RemoveListener(HandleSliderValueChanged);
        }

        protected override void UpdateDataFromLocal()
        {
            _value = _slider.value;
        }

        protected override void UpdateLocalFromData()
        {
            _slider.value = _value;
        }

        private void HandleSliderValueChanged(float value)
        {
            _value = value;
            SaveData();
        }
    }
}