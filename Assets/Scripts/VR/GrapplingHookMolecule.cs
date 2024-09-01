using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using HTC.UnityPlugin.Vive;
using HTC.UnityPlugin.Utility;

namespace UMol {
[RequireComponent(typeof(ViveRoleSetter))]
public class GrapplingHookMolecule : MonoBehaviour {


    ControllerGrabAndScale cgs;
    public Vector2 axisDeadzone = new Vector2(0.2f, 0.2f);
    public float speedScale = 0.05f;

    private Vector2 currentAxis = Vector2.zero;
    protected bool isChanging;

    ViveRoleProperty curRole;


    void OnEnable() {

        curRole = GetComponent<ViveRoleSetter>().viveRole;

        cgs = GetComponent<ControllerGrabAndScale>();
        isChanging = false;

        if (curRole != null) {
            ViveInput.AddPressDown((HandRole)curRole.roleValue, ControllerButton.PadTouch, TouchpadTouchStart);
            ViveInput.AddPressUp((HandRole)curRole.roleValue, ControllerButton.PadTouch, TouchpadTouchEnd);
        }
    }

    void Update() {

        if (isChanging && cgs.grabbedMolecule != null) {
            Vector2 actualAxis = ViveInput.GetPadAxis(curRole);
            currentAxis = actualAxis;

            if (OutsideDeadzone(currentAxis.y, axisDeadzone.y) || currentAxis.y == 0f) {
                Vector3 localPos = cgs.grabbedMolecule.localPosition;

                localPos.z += currentAxis.y * speedScale;
                cgs.grabbedMolecule.localPosition = localPos;

                Vector3 cogPosWorld = cgs.grabbedMolecule.TransformPoint(cgs.grabbedCentroid);
                Vector3 CM = cogPosWorld - transform.position;
                if (CM.z < 0.0f) {
                    cgs.grabbedMolecule.Translate(-CM, Space.World);
                }
                // if(localPos.z < 0.0f){
                //  localPos.z = 0.0f;
                // }
            }
        }
    }

    protected virtual void TouchpadTouchStart()
    {
        isChanging = true;
    }


    protected virtual void TouchpadTouchEnd()
    {
        currentAxis = Vector2.zero;
        isChanging = false;
    }

    protected virtual bool OutsideDeadzone(float axisValue, float deadzoneThreshold)
    {
        return (axisValue > deadzoneThreshold || axisValue < -deadzoneThreshold);
    }
}
}