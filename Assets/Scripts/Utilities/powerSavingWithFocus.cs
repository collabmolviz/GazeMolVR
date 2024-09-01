using UnityEngine;
using UnityEngine.Rendering;

namespace UMol {
public class powerSavingWithFocus : MonoBehaviour
{
    Camera mainCam;
    Camera uiCam;
#if !UNITY_EDITOR
    bool isPaused = false;

    void OnGUI()
    {
        if (isPaused)
            GUI.Label(new Rect(100, 100, 100, 100), "<size=20>UnityMol is paused</size>");
    }

    void Start() {
        if (mainCam == null) {
            mainCam = Camera.main;
        }
        if (mainCam != null) {
            uiCam = mainCam.gameObject.GetComponentInChildren<Camera>();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        isPaused = !hasFocus;

        if (mainCam == null) {
            mainCam = Camera.main;
            if (mainCam != null) {
                uiCam = mainCam.gameObject.GetComponentInChildren<Camera>();
            }
        }

        if (!UnityMolMain.inVR()) {
            if (isPaused) {
                Debug.Log("Losing focus");

                if (mainCam != null) {
                    mainCam.enabled = false;
                }
                if (uiCam != null) {
                    uiCam.enabled = false;
                }

                QualitySettings.vSyncCount = 0;  // VSync must be disabled
                Application.targetFrameRate = 5;
                // OnDemandRendering.renderFrameInterval = 10;
                Physics.autoSimulation = false;


            }
            else {
                Debug.Log("Gaining focus");

                if (mainCam != null) {
                    mainCam.enabled = true;
                }
                if (uiCam != null) {
                    uiCam.enabled = true;
                }

                Physics.autoSimulation = true;
                // OnDemandRendering.renderFrameInterval = 1;
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = Screen.currentResolution.refreshRate;
            }
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        isPaused = pauseStatus;
    }
#endif
}
}