using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class Annotate2D : UnityMolAnnotation {

    /// posPercent defines the position based on the percentage from bottom/left to top/right of the screen
    /// 0/0 means bottom/left and 1/1 means top/right
    public Vector2 posPercent = Vector2.zero;//Bottom right
    public float scale = 1.0f;
    public string content;
    public Color colorText = new Color(0.0f, 0.0f, 0.5f, 1.0f);
    public Canvas screenspaceCan = null;

    public override void Create() {
        GameObject gocan = GameObject.Find("CanvasScreenspace");
        if (gocan == null) {
            gocan = new GameObject("CanvasScreenspace");
            gocan.layer =  LayerMask.NameToLayer("Default");
            screenspaceCan = gocan.AddComponent<Canvas>();
            // screenspaceCan.renderMode = RenderMode.ScreenSpaceOverlay;
            screenspaceCan.renderMode = RenderMode.ScreenSpaceCamera;
            screenspaceCan.worldCamera = Camera.main;
        }
        else {
            screenspaceCan = gocan.GetComponent<Canvas>();
        }

        go = new GameObject("Text2D");
        go.transform.SetParent(gocan.transform);
        go.transform.localPosition = Vector3.zero;
        Text t = go.AddComponent<Text>();
        t.text = content;
        Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        t.font = ArialFont;
        t.material = ArialFont.material;
        t.color = colorText;

        t.fontSize = 140;


        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        posPercent.x = Mathf.Clamp(posPercent.x, 0.0f, 1.0f);
        posPercent.y = Mathf.Clamp(posPercent.y, 0.0f, 1.0f);

        (go.transform as RectTransform).anchorMin = posPercent;
        (go.transform as RectTransform).anchorMax = posPercent;

        (go.transform as RectTransform).pivot = Vector2.one * 0.5f;

        go.transform.localScale = Vector3.one * 0.1f * scale;
        (go.transform as RectTransform).anchoredPosition = Vector3.zero;

    }

    public override void Update() {
    }

    public override void UnityUpdate() {
    }

    public override void Delete() {
        if (go != null)
            GameObject.Destroy(go);
    }

    public override void Show(bool show = true) {
        if (go != null) {
            isShown = show;
            go.SetActive(show);
        }
    }

    public override SerializedAnnotation Serialize() {
        SerializedAnnotation san = new SerializedAnnotation();
        san.color = colorText;
        san.posPercent = posPercent;
        san.size = scale;
        san.content = content;
        san.color = colorText;
        fillSerializedAtoms(san);
        return san;
    }

    public override int toAnnoType(){
        return 1;
    }
}
}