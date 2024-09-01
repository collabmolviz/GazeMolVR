using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UMol {

[RequireComponent(typeof(PostProcessVolume))]
[RequireComponent(typeof(MouseOverSelection))]
public class MouseAutoFocus : MonoBehaviour {

    PostProcessVolume postpV;
    MouseOverSelection mos;

    [SerializeField]
    private DepthOfField DOF;

    private UnityMolAtom curA = null;

    public void Init() {
        if (GetComponent<PostProcessVolume>() != null) {
            postpV = GetComponent<PostProcessVolume>();
            mos = GetComponent<MouseOverSelection>();
            postpV.profile.TryGetSettings(out DOF);
        }
    }
    void Start() {
        Init();
    }

    void Update() {
        if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() ) { //Mouse clicked
            curA = null;
            if (postpV != null && mos != null) {

                Vector3 p = Vector3.zero;
                bool isExtrAtom = false;
                UnityMolAtom a = mos.getAtomPointed(true, ref p, ref isExtrAtom);

                if (a != null) {
                    curA = a;
                    if (UnityMolMain.isDOFOn)
                        API.APIPython.setDOFFocusDistance(Vector3.Distance(GetComponent<Camera>().transform.position, p));
                }
            }
        }
        if (curA != null && UnityMolMain.isDOFOn) {
            API.APIPython.setDOFFocusDistance(Vector3.Distance(GetComponent<Camera>().transform.position, curA.curWorldPosition));
        }
    }
    public void disableDOF() {
        curA = null;
        if (postpV != null) {
            DOF.enabled.value = false;
            UnityMolMain.isDOFOn = false;
        }
        if (mos != null) {
            mos.tempDisable = false;
        }
    }
    public void enableDOF() {
        curA = null;
        if (postpV != null) {
            DOF.enabled.value = true;
            UnityMolMain.isDOFOn = true;
        }
        if (mos != null) {
            mos.tempDisable = true;
        }
    }

    public float getFocusDistance() {
        if (postpV == null) {
            Init();
        }
        if (postpV != null) {
            return DOF.focusDistance.value;
        }
        return -1.0f;
    }
    public void setFocusDistance(float v) {
        if (postpV == null) {
            Init();
        }
        if (postpV != null) {
            FloatParameter newFocusDistance = new FloatParameter { value = v };
            DOF.focusDistance.value = newFocusDistance;
        }
    }

    public void setAperture(float v) {
        if (postpV == null) {
            Init();
        }

        if (postpV != null) {
            FloatParameter newA = new FloatParameter { value = v};
            DOF.aperture.value = newA;
        }

    }
    public float getAperture() {
        if (postpV == null) {
            Init();
        }
        if (postpV != null) {
            return DOF.aperture;
        }
        return -1.0f;
    }
    public void setFocalLength(float v) {
        if (postpV == null) {
            Init();
        }

        if (postpV != null) {
            FloatParameter newF = new FloatParameter { value = v};
            DOF.focalLength.value = newF;
        }

    }
    public float getFocalLength() {
        if (postpV == null) {
            Init();
        }
        if (postpV != null) {
            return DOF.focalLength;
        }
        return -1.0f;
    }
}
}