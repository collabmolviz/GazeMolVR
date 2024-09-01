using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
using Valve.VR;
#endif

public class forceVROff : MonoBehaviour {


	void Start () {
		DisableVR();
	}
    //From https://stackoverflow.com/questions/36702228/enable-disable-vr-from-code
    IEnumerator LoadDevice(string newDevice, bool enable)
    {
        XRSettings.LoadDeviceByName(newDevice);
        yield return null;
        XRSettings.enabled = enable;
    }

    void DisableVR()
    {
#if VIU_STEAMVR_2_0_0_OR_NEWER && UNITY_STANDALONE
        Destroy(FindObjectOfType<SteamVR_Behaviour>());
#endif
        StartCoroutine(LoadDevice("", false));
    }
}
