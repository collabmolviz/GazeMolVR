/*
In this script, I am using "customRaycastAtomBurst" technqiue which is implemented in UnityMol.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using TMPro;
using UMol;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;


public class EyeCustomRaycaster : MonoBehaviour
{
    [SerializeField] private GameObject gazeCues;

    public CustomRaycastBurst raycaster;
    private static Ray testRay;

    private static EyeData_v2 eyeData = new EyeData_v2();
    private static bool eye_callback_registered = false;

    void Start()
    {
        raycaster = UnityMolMain.getCustomRaycast();

        if (!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        bool valid;

        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING) return;

        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = true;
        }

        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }

        if (eye_callback_registered)
        {
            if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out testRay, eyeData)) valid = true;
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out testRay, eyeData)) valid = true;
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out testRay, eyeData)) valid = true;
            else return;

            if (valid)
            {
                Profiler.BeginSample("RaycastInfo");
                RaycastInfo();
                Profiler.EndSample();
            }
        }
        else
        {
            if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out testRay)) valid = true;
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out testRay)) valid = true;
            else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out testRay)) valid = true;
            else return;

            if (valid)
            {
                Profiler.BeginSample("RaycastInfo");
                RaycastInfo();
                Profiler.EndSample();
            }
        }
    }

    private void RaycastInfo()
    {
        Ray rayGlobal = new Ray(Camera.main.transform.TransformPoint(testRay.origin), Camera.main.transform.TransformDirection(testRay.direction));
        Vector3 p = Vector3.zero;
        bool isExtrAtom = false;
        UnityMolAtom a = raycaster.customRaycastAtomBurst(
                             rayGlobal.origin,
                             rayGlobal.direction,
                             ref p, ref isExtrAtom, true);

        if (a != null)
        {
            //Debug.Log("residue name is:" + a.residue.name);
            gazeCues.transform.position = p;
        }
    }

    private void OnDisable()
    {
        Release();
    }

    void OnApplicationQuit()
    {
        Release();
    }

    private static void Release()
    {
        if (eye_callback_registered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }
    }

    internal class MonoPInvokeCallbackAttribute : System.Attribute
    {
        public MonoPInvokeCallbackAttribute() { }
    }

    [MonoPInvokeCallback]
    private static void EyeCallback(ref EyeData_v2 eye_data)
    {
        eyeData = eye_data;
    }
}