using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Text;
using System.Collections.Generic;

namespace UMol {

public class AtomRepresentationSugarRibbons : AtomRepresentation {

    public List<GameObject> meshesGO;
    public Dictionary<UnityMolAtom, List<int>> atomToVertBB;
    public bool createPlanes;
    private Material ribbonMat;
    private Material ribbonMatPlane;
    private string structureName;
    private GameObject newRep;

    public AtomRepresentationSugarRibbons(int idF, string structName, UnityMolSelection sel, bool planes = true) {

        colorationType = colorType.atom;
        meshesGO = new List<GameObject>();

        if (ribbonMat == null)
            ribbonMat = new Material(Shader.Find("Custom/SurfaceVertexColor"));
        if (ribbonMatPlane == null)
            ribbonMatPlane = new Material(Shader.Find("Custom/SurfaceVertexColorTransparent"));
        ribbonMatPlane.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);

        structureName = structName;
        selection = sel;
        createPlanes = planes;
        idFrame = idF;

        representationParent = UnityMolMain.getRepStructureParent(structName).transform;

        newRep = new GameObject("AtomSugarRibbonsRepresentation");
        newRep.transform.parent = representationParent;
        representationTransform = newRep.transform;

        displaySugarRibbonsMesh(structName, newRep.transform);

        newRep.transform.localPosition = Vector3.zero;
        newRep.transform.localRotation = Quaternion.identity;
        newRep.transform.localScale = Vector3.one;

        nbAtoms = selection.atoms.Count;
    }

    public void displaySugarRibbonsMesh(string structName, Transform repParent, float ribbonThick = 0.2f,
                                        float ribbonHeight = 0.35f, bool isTraj = false) {

        List<Mesh> meshes = SugarRibbons.createSugarRibbons(selection, idFrame,
                            ref atomToVertBB, ribbonThick, ribbonHeight, createPlanes);

        if (meshes.Count == 2) {

            GameObject goPlane = new GameObject(structName + "_SugarRibbonsPlanesMesh");
            MeshFilter mfp = goPlane.AddComponent<MeshFilter>();

            mfp.sharedMesh = meshes[0];
            goPlane.AddComponent<MeshRenderer>().sharedMaterial = ribbonMatPlane;
            goPlane.transform.parent = repParent;
            goPlane.transform.localRotation = Quaternion.identity;
            goPlane.transform.localPosition = Vector3.zero;
            goPlane.transform.localScale = Vector3.one;

            meshesGO.Add(goPlane);

            GameObject go = new GameObject(structName + "_SugarRibbonsMesh");
            MeshFilter mf = go.AddComponent<MeshFilter>();

            mf.sharedMesh = meshes[1];
            go.AddComponent<MeshRenderer>().sharedMaterial = ribbonMat;
            go.transform.parent = repParent;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            meshesGO.Add(go);

        }
    }

    public void recompute(bool isNewModel = false) {

        if (meshesGO != null) {
            for (int i = 0; i < meshesGO.Count; i++) {
                GameObject.Destroy(meshesGO[i].GetComponent<MeshFilter>().sharedMesh);
                GameObject.Destroy(meshesGO[i]);
            }
            meshesGO.Clear();
        }

        displaySugarRibbonsMesh(structureName, newRep.transform);

    }
    public override void Clean() {

        if (meshesGO != null) {
            for (int i = 0; i < meshesGO.Count; i++) {
                GameObject.Destroy(meshesGO[i].GetComponent<MeshFilter>().sharedMesh);
                GameObject.Destroy(meshesGO[i]);
            }
            meshesGO.Clear();
        }
        GameObject.Destroy(ribbonMat);
        GameObject.Destroy(ribbonMatPlane);
        atomToVertBB.Clear();
    }
}
}