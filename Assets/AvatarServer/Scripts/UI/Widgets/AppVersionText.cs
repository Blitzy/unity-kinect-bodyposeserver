using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class AppVersionText : MonoBehaviour {
    private void Awake() {
        var text = GetComponent<Text>();

        text.text = string.Format("v{0}", Application.version);
    }
}