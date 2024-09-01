/*

In this script, I am using "SRanipal_Eye_v2.Focus" for raycasting on an object.

Regarding "protein" tag:

newRep.transform.GetChild(i).gameObject.tag = "protein";

Assets/Scripts/UnityMolMolecule/Representations/AtomRepresentationOptihb.cs
Assets/Scripts/UnityMolMolecule/Representations/BondRepresentationOptihs.cs
Assets/Scripts/UnityMolMolecule/Representations/CartoonRepresentation.cs
Assets/Scripts/UnityMolMolecule/Representations/SurfaceRepresentation.cs

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
using Photon.Pun;

public class SRanipalEyeRaycaster : MonoBehaviourPunCallbacks
{
    public GameObject gazeCues;
    public bool lookingAtProtein;

    private static EyeData_v2 eyeData = new EyeData_v2();
    private static bool eye_callback_registered = false;

    private static Ray testRay;
    private static FocusInfo focusInfo;

    private Queue<Vector3> gazePositions = new Queue<Vector3>();
    private int smoothingWindowSize = 3; // Number of samples to average for smoothing

    void Start()
    {
        lookingAtProtein = false;
        if (!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            enabled = false;
            return;
        }
    }

    public void Update()
    {
        //SRanipal framework setup for eye-gaze data at 120Hz
        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING)
            return;

        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback)
            );
            eye_callback_registered = true;
        }
        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback)
            );
            eye_callback_registered = false;
        }

        if (eye_callback_registered)
        {
            if (SRanipal_Eye_v2.Focus(GazeIndex.COMBINE, out testRay, out focusInfo, eyeData))
            {

                /*
                if (focusInfo.collider.gameObject.CompareTag("protein")) // In all "Representations" files, I have assigned "protein" tag while loading
                {
                    gazeCues.transform.position = focusInfo.point;
                    lookingAtProtein = true;
                }
                else
                {
                    lookingAtProtein = false;
                }
                */

                ProcessGaze(focusInfo);
            }
        }
        else
        {
            if (SRanipal_Eye_v2.Focus(GazeIndex.COMBINE, out testRay, out focusInfo))
            {
                /*
                if (focusInfo.collider.gameObject.CompareTag("protein"))
                {
                    gazeCues.transform.position = focusInfo.point;
                    lookingAtProtein = true;
                }
                else
                {
                    lookingAtProtein = false;
                }
                */

                ProcessGaze(focusInfo);
            }
        }
    }

    private void ProcessGaze(FocusInfo focusInfo)
    {
        if (focusInfo.collider.gameObject.CompareTag("protein"))  // In all "Representations" files, I have assigned "protein" tag while loading
        {
            gazePositions.Enqueue(focusInfo.point);
            if (gazePositions.Count > smoothingWindowSize)
            {
                gazePositions.Dequeue();
            }

            gazeCues.transform.position = GetSmoothedPosition();
            lookingAtProtein = true;
        }
        else
        {
            lookingAtProtein = false;
        }
    }

    private Vector3 GetSmoothedPosition()
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 pos in gazePositions)
        {
            sum += pos;
        }
        return sum / gazePositions.Count;
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
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback)
            );
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