using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Text;

using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Utility;

using HTC.UnityPlugin.Pointer3D;

namespace UMol {
public class PointerMoveUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    public Pointer3DRaycaster raycasterLeft;
    public Pointer3DRaycaster raycasterRight;
    public bool grabbedUI = false;
    Transform UIT;
    GameObject grabbedGo;
    Transform savedParent;

    bool isPointerEntered = false;
    bool left = false;

    GameObject prevOverL = null;
    GameObject prevOverR = null;

    ControllerGrabAndScale cgsL = null;
    ControllerGrabAndScale cgsR = null;

    public bool moveParent = false;

    void Awake() {
        try {
            cgsL = GameObject.Find("LeftHand").GetComponent<ControllerGrabAndScale>();
            cgsR = GameObject.Find("RightHand").GetComponent<ControllerGrabAndScale>();
        }
        catch {}
    }

    void Update()
    {
        if (grabbedUI) {
            if (left && ViveInput.GetPressUpEx(HandRole.LeftHand, ControllerButton.Trigger)) {
                releaseMenu();
            }
            if (!left && ViveInput.GetPressUpEx(HandRole.RightHand, ControllerButton.Trigger)) {
                releaseMenu();
            }
        }
        else {
            if (ViveInput.GetPressDownEx(HandRole.RightHand, ControllerButton.Trigger) && isPointerEntered)
            {
                left = false;
                if (moveParent)
                    grabMenu(gameObject.transform.parent.gameObject);
                else
                    grabMenu(gameObject);
            }
            if (!grabbedUI && ViveInput.GetPressDownEx(HandRole.LeftHand, ControllerButton.Trigger) && isPointerEntered)
            {
                left = true;
                if (moveParent)
                    grabMenu(gameObject.transform.parent.gameObject);
                else
                    grabMenu(gameObject);
            }
        }
        if (!isPointerEntered) {
            prevOverL = null;
            prevOverR = null;
        }
        else if (raycasterLeft != null && !grabbedUI) {
            var resultL = raycasterLeft.FirstRaycastResult();
            if (resultL.isValid) {
                int lid = resultL.gameObject.GetInstanceID();
                int prevlid = (prevOverL != null ? prevOverL.GetInstanceID() : 0);

                if (prevlid != lid &&
                        (resultL.gameObject.TryGetComponent(out Button b) ||
                         resultL.gameObject.TryGetComponent(out InputField ipf) ||
                         resultL.gameObject.TryGetComponent(out Toggle t)))  {


                    ViveInput.TriggerHapticPulse(HandRole.LeftHand, 1000);
                    prevOverL = resultL.gameObject;
                }
                else if (prevlid != lid)
                    prevOverL = null;
            }
            else
                prevOverL = null;
        }
        if (raycasterRight != null && !grabbedUI && isPointerEntered) {
            var resultR = raycasterRight.FirstRaycastResult();
            if (resultR.isValid) {
                int rid = resultR.gameObject.GetInstanceID();
                int prevrid = (prevOverR != null ? prevOverR.GetInstanceID() : 0);
                if ( prevrid != rid &&
                        (resultR.gameObject.TryGetComponent(out Button b) ||
                         resultR.gameObject.TryGetComponent(out InputField ipf) ||
                         resultR.gameObject.TryGetComponent(out Toggle t))) {


                    ViveInput.TriggerHapticPulse(HandRole.RightHand, 1000);
                    prevOverR = resultR.gameObject;
                }
                else if (prevrid != rid)
                    prevOverR = null;
            }
            else {
                prevOverR = null;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        isPointerEntered = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        isPointerEntered = false;
    }

    private void grabMenu(GameObject toGrab) {
        //Don't grab UI when we already grabbed a molecule
        if (left && cgsL.grabbedMolecule != null)
            return;
        if (!left && cgsR.grabbedMolecule != null)
            return;

        grabbedGo = toGrab;
        savedParent = grabbedGo.transform.parent;
        if (left)
            grabbedGo.transform.SetParent(UnityMolMain.getLeftController().transform, true);
        else
            grabbedGo.transform.SetParent(UnityMolMain.getRightController().transform, true);

        grabbedUI = true;
        // ViveInput.TriggerHapticPulseEx(curRole.roleType, curRole.roleValue, 100);
    }

    private void releaseMenu() {
        Vector3 savedP = grabbedGo.transform.position;
        Quaternion savedR = grabbedGo.transform.rotation;

        grabbedGo.transform.SetParent(savedParent, false);
        grabbedUI = false;
        grabbedGo.transform.position = savedP;
        grabbedGo.transform.rotation = savedR;

        // ViveInput.TriggerHapticPulseEx(curRole.roleType, curRole.roleValue, 100);
    }
}
}