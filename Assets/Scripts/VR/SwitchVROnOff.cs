using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
using Valve.VR;
#endif

public class SwitchVROnOff : MonoBehaviour {

    public GameObject room;
    public GameObject floor;
    public GameObject VRMenu;
    public GameObject NotifMenu;

    public bool reactiveVR = false;
    public bool deactiveVR = false;

    string prevLoadedDevice = "";
    // private Vector3 savedCamPos = Vector3.zero;

    void Start() {
        if (room == null) {
            room = GameObject.Find("RoomVR");
        }
        if (floor == null) {
            floor = GameObject.Find("Floor");
        }
        if (VRMenu == null) {
            VRMenu = GameObject.Find("CanvasMainUIVR");
        }
        if (NotifMenu == null) {
            NotifMenu = GameObject.Find("CanvasNotif");
        }
    }

    //From https://stackoverflow.com/questions/36702228/enable-disable-vr-from-code
    IEnumerator LoadDevice(string newDevice, bool enable)
    {
        XRSettings.LoadDeviceByName(newDevice);
        yield return null;
        XRSettings.enabled = enable;
        if (newDevice == "") {
            Camera mc = Camera.main;
            mc.clearFlags = CameraClearFlags.SolidColor;
            // mc.transform.position = savedCamPos;
            mc.transform.position = new Vector3(0, 0, -3);
            GameObject uicam = GameObject.Find("UICamera");
            if(uicam != null){
                uicam.transform.position = mc.transform.position;
                uicam.transform.rotation = mc.transform.rotation;
            }

        }
        else {
            Camera.main.clearFlags = CameraClearFlags.Skybox;
        }
    }

    public void DisableVR()
    {
        // savedCamPos = Camera.main.transform.position;
        if (floor != null)
            floor.SetActive(false);
        if (room != null)
            room.SetActive(false);
        if (VRMenu != null)
            VRMenu.SetActive(false);
        if (NotifMenu != null)
            NotifMenu.SetActive(false);

#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
        Destroy(FindObjectOfType<SteamVR_Behaviour>());
#endif
        prevLoadedDevice = XRSettings.loadedDeviceName;
        StartCoroutine(LoadDevice("", false));
    }
    public void ActivateVR() {
        if (XRSettings.supportedDevices.Length != 0) {
            string deviceToLoad = "";
            if (!string.IsNullOrEmpty(prevLoadedDevice)) {
                deviceToLoad = prevLoadedDevice;
            }
            else {
                deviceToLoad = XRSettings.supportedDevices[0];
                if (deviceToLoad == "None" && XRSettings.supportedDevices.Length > 1) {
                    deviceToLoad = XRSettings.supportedDevices[1];
                }
            }
#if UNITY_EDITOR
            Debug.Log("Loading VR device: " + deviceToLoad);
#endif
            if (floor != null)
                floor.SetActive(true);
            if (room != null)
                room.SetActive(true);
            if (VRMenu != null)
                VRMenu.SetActive(true);
            if (NotifMenu != null)
                NotifMenu.SetActive(true);

#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
            if (deviceToLoad.Contains("OpenVR"))
                SteamVR.Initialize(true);
#endif

            StartCoroutine(LoadDevice(deviceToLoad, true));
        }
#if UNITY_EDITOR
        else {
            Debug.LogError("No VR device to load");
        }
#endif
    }

    public void switchVR() {
        if (XRSettings.enabled) {
            DisableVR();
        }
        else {
            ActivateVR();
        }
    }

    void Update() {
        if (reactiveVR) {
            ActivateVR();
            reactiveVR = false;
        }
        if (deactiveVR) {
            DisableVR();
            deactiveVR = false;
        }
    }
}
