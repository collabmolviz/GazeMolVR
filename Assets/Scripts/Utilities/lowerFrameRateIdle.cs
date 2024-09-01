using UnityEngine;
using UnityEngine.Rendering;

using UMol.API;

namespace UMol {
public class lowerFrameRateIdle : MonoBehaviour {


    public int idleTimeInSec = 5;
    public bool isIdle = false;
    float lastIdleTime;
#if !UNITY_EDITOR
    ManipulationManager mm = null;
    DockingManager dm = null;

    void Start() {
        mm = APIPython.getManipulationManager();
        dm = UnityMolMain.getDockingManager();
    }
    public bool idleCheck() {
        if(!UnityMolMain.allowIDLE)
            return false;
        if (UnityMolMain.inVR() || UnityMolMain.raytracingMode)
            return false;
        if (mm != null && mm.isRotating)
            return false;
        if (RecordManager.busy)
            return false;
        if(dm.isRunning)
            return false;
        if(UnityMolMain.IMDRunning)
            return false;
        if(APIPython.isATrajectoryPlaying())
            return false;
        #if UNITY_STANDALONE_OSX && !UNITY_2020_1_OR_NEWER
        return false;
        #endif
        return Time.time - lastIdleTime > idleTimeInSec;
    }

    void enterIDLE() {
        QualitySettings.vSyncCount = 0;  // VSync must be disabled
        Application.targetFrameRate = 5;
        Physics.autoSimulation = false;
        AudioListener.pause = true;
    }
    void exitIDLE() {
        Physics.autoSimulation = true;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = Screen.currentResolution.refreshRate;
        AudioListener.pause = false;
    }

    void Update() {

        if (Input.anyKey || Input.mouseScrollDelta != Vector2.zero) {
            if (isIdle) {
                isIdle = false;
                exitIDLE();
            }
            lastIdleTime = Time.time;
        }
        else if (idleCheck() && !isIdle) {
            isIdle = true;
            enterIDLE();
        }
    }
#endif

}
}


