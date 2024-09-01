
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;

namespace UMol {
public class CameraManager : MonoBehaviour {

    public static bool gridMode = false;


    // [HideInInspector]
    List<GameObject> gridcameras = new List<GameObject>();
    Camera mainCam;
    void Awake() {
        mainCam = Camera.main;
        //  var gos = GameObject.FindGameObjectsWithTag("MainCamera");
        //  foreach (GameObject g in gos) {
        //      Camera c = g.GetComponent<Camera>();
        //      if (c != null) {
        //          cameras.Add(c);
        //      }
        //  }
    }

    public void activateGridMode() {
        if (gridMode) {
            Debug.LogError("Grid mode is already on");
            return;
        }
        if (UnityMolMain.inVR()) {
            Debug.LogError("Cannot activate grid mode in VR");
            return;
        }
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        int N = sm.loadedStructures.Count;
        if (N < 2) {
            Debug.LogError("Cannot activate grid mode with less than 2 loaded molecules");
            return;
        }
        if (N > 16) {
            Debug.LogError("Cannot activate grid mode with more than 16 loaded molecules");
            return;
        }



        Vector2 wh = getGridSize(N);
        int w = (int)wh.x;
        int h = (int)wh.y;

        int i = 0;
        int j = 0;
        for (int id = 1; id <= N; id++) {

            GameObject gcam = GameObject.Instantiate(mainCam.gameObject);
            gridcameras.Add(gcam);
            GameObject.Destroy(gcam.GetComponent<AudioListener>());
            GameObject.Destroy(gcam.GetComponent<CameraManager>());
            GameObject.Destroy(gcam.GetComponent<MouseMeasure>());
            GameObject.Destroy(gcam.GetComponent<ManipulationManager>());
            gcam.transform.position = mainCam.transform.position;
            gcam.transform.rotation = mainCam.transform.rotation;
            gcam.transform.localScale = mainCam.transform.localScale;
            Camera ccam = gcam.GetComponent<Camera>();
            string lname = "Lay" + id;
            LayerMask cullLayer = LayerMask.GetMask("Default", lname);
            LayerMask newLayer = LayerMask.NameToLayer(lname);

            ccam.cullingMask = cullLayer;
            UnityMolStructure s = sm.loadedStructures[id - 1];
            GameObject sgo = sm.structureToGameObject[s.name];
            SetLayerChildren(sgo, newLayer);

            ccam.rect = new Rect(i / (float)w, (h - 1 - j) / (float)h, 1.0f / (float)w, 1.0f / (float)h);

            i++;
            if (i == w) {
                i = 0;
                j++;
            }
        }



        mainCam.enabled = false;

        GL.ClearWithSkybox(false, mainCam);

        gridMode = true;
    }

    public void desactivateGridMode() {

        gridMode = false;
        mainCam.enabled = true;

        for (int i = 0; i < gridcameras.Count; i++) {
            GameObject.Destroy(gridcameras[i]);
        }

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        foreach (GameObject sgo in sm.structureToGameObject.Values) {
            SetLayerChildren(sgo, 1 << 0);
        }

        gridcameras.Clear();


    }

    static void SetLayerChildren(GameObject gameObject, int layer) {
        if (!gameObject) return;

        foreach (Transform c in gameObject.transform) {
            if (c.name == "AtomParent")
                continue;
            foreach (var child in c.gameObject.GetComponentsInChildren(typeof(Transform), true)) {
                child.gameObject.layer = layer;
            }
        }
    }

    Vector2 getGridSize(int N) {
        float sN = Mathf.Sqrt(N);
        int h = (int)Mathf.Max((int)sN, 1);
        int w = Mathf.CeilToInt(N / h);

        while (w * h < N) {
            h++;
        }

        return new Vector2(w, h);

    }

}
}
