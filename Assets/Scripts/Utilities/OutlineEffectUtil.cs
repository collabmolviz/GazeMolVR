using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UMol {

[RequireComponent(typeof(PostProcessVolume))]
public class OutlineEffectUtil : MonoBehaviour {
    public bool isOn = false;
    PostProcessVolume postpV;

    [SerializeField]
    private EdgeDetect_BeforeTransparent OutlineEffect;
    
    public PostProcessResources postProcessResources;

    public void Init() {
        if (GetComponent<PostProcessVolume>() != null) {
            postpV = GetComponent<PostProcessVolume>();
            postpV.profile.TryGetSettings(out OutlineEffect);
        }
    }
    void Start() {
        Init();
    }

    public void disableOutline() {
        if (postpV == null) {
            Init();
        }
        if (postpV != null) {
            OutlineEffect.enabled.value = false;
        }
        isOn = false;

    }
    public void enableOutline() {
        if (postpV == null) {
            Init();
        }
        if (postpV != null) {
            OutlineEffect.enabled.value = true;
            isOn = true;
        }
    }

    public void setThickness(float v) {
        if (postpV == null) {
            Init();
        }
        if (postpV != null) {
            OutlineEffect.sampleDist.value = v;
        }
    }
    public float getThickness(){
        if (postpV != null) {
            return OutlineEffect.sampleDist.value;
        }
        return 0.0f;
    }

    public void setColor(Color c) {
        if (postpV == null) {
            Init();
        }
        if (postpV != null) {
            OutlineEffect.outlineColor.value = c;
        }
    }
    public Color getColor(){
        if (postpV != null) {
            return OutlineEffect.outlineColor.value;
        }
        return Color.black;
    }

}
}

