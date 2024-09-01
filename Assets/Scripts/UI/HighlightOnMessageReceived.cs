using UnityEngine;
using UnityEngine.UI;


namespace UMol {
[RequireComponent(typeof(Animation))]
public class HighlightOnMessageReceived : MonoBehaviour {

    Animation curAnim;

    void Start() {
        curAnim = GetComponent<Animation>();
    }

    void OnEnable() {
        Application.logMessageReceived += highlightMenu;
    }
    void OnDisable() {
        Application.logMessageReceived -= highlightMenu;
    }

    void highlightMenu(string condition, string stackTrace, LogType type) {
        if (curAnim != null) {
            if (!curAnim.IsPlaying("highlightButton")) {
                curAnim.Play("highlightButton");
            }
        }
    }
}
}