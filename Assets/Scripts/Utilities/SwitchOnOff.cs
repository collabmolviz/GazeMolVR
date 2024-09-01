using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace UMol {
public class SwitchOnOff : MonoBehaviour {

    public void switchOnOff() {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }

    public void updateLayoutParent(Transform t) {

        Canvas.ForceUpdateCanvases();
        if (t.parent != null && t.parent.GetComponent<RectTransform>() != null) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(t.parent.GetComponent<RectTransform>());
        }
    }

    public void updateLayoutParentParent(Transform t) {

        Canvas.ForceUpdateCanvases();
        if (t.parent != null && t.parent.parent != null && t.parent.parent.GetComponent<RectTransform>() != null) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(t.parent.parent.GetComponent<RectTransform>());
        }
    }

    public void scrollToCenter(RectTransform target) {
        if(target != null && target.gameObject.activeInHierarchy)
            StartCoroutine(SetScrollValue(target));

    }
    IEnumerator SetScrollValue(RectTransform target) {
        ScrollRect scr = gameObject.GetComponentInParent<ScrollRect>();
        yield return new WaitForEndOfFrame();
        if (scr != null)
            UIExtensions.ScrollToCenter(scr, target);
        yield return null;
    }

}
}