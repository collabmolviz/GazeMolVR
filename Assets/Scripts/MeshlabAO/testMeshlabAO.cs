using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using UMol.API;


namespace UMol {
public class testMeshlabAO : MonoBehaviour {
    private List<meshlabAO> maos = new List<meshlabAO>();
    private List<GameObject> meshesGO;
    private meshlabAO mao;
    bool done = false;

    void Start() {
        APIPython.fetch("3eam");
        APIPython.showSelection(APIPython.last().ToSelectionName(), "s", SurfMethod.MSMS);

        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        RepType repType = APIPython.getRepType("s");

        List<UnityMolRepresentation> existingReps = repManager.representationExists(APIPython.last().ToSelectionName(), repType);
        if (existingReps != null) {
            foreach (UnityMolRepresentation existingRep in existingReps) {
                foreach (SubRepresentation sr in existingRep.subReps) {
                    UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;
                    meshesGO = surfM.meshesGO;
                    // foreach (GameObject mgo in surfM.meshesGO) {
                    //     meshlabAO mao = new meshlabAO();
                    //     Mesh m = mgo.GetComponent<MeshFilter>().sharedMesh;
                    //     mao.attachedGo = mgo;
                    //     mao.StartThread(m);
                    //     maos.Add(mao);
                    // }
                }
            }
        }

        if (meshesGO != null) {
            mao = new meshlabAO();
            mao.StartThread(meshesGO);
        }
    }

    void Update() {
        if (!done && !mao._isMeshlabRunning && mao.success) {
            List<float> aos = mao.aoResult;
            int id = 0;
            foreach (GameObject go in meshesGO) {
                Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
                Vector2[] uvs2 = m.uv2;
                if (uvs2 == null || uvs2.Length == 0) {
                    uvs2 = new Vector2[m.vertexCount];
                }
                for (int i = 0; i < uvs2.Length; i++) {
                    if (id < aos.Count) {
                        float curAO = aos[id];
                        uvs2[i] = Vector2.one * curAO;
                    }
                    id++;
                }
                m.uv2 = uvs2;
                go.GetComponent<MeshFilter>().sharedMesh = m;
            }
            UnityEngine.Debug.Log("Done !");
            done = true;
        }
        // if (!done && meshlabsFinished()) {
        //     foreach (meshlabAO mao in maos) {
        //         List<float> aos = mao.aoResult;
        //         GameObject go = mao.attachedGo;
        //         Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
        //         Color32[] cols = m.colors32;
        //         if (cols == null || cols.Length == 0) {
        //             cols = new Color32[m.vertexCount];
        //         }
        //         for (int i = 0; i < cols.Length; i++) {
        //             cols[i] = new Color32((byte)(aos[i] * 255), (byte)(aos[i] * 255), (byte)(aos[i] * 255), 255);
        //         }
        //         m.colors32 = cols;
        //         go.GetComponent<MeshFilter>().sharedMesh = m;
        //     }
        //     UnityEngine.Debug.Log("Done !");
        //     done = true;
        // }
    }

    bool meshlabsFinished() {
        foreach (meshlabAO mao in maos) {
            if (mao._isMeshlabRunning || !mao.success) {
                return false;
            }
        }
        return true;
    }
}
}
