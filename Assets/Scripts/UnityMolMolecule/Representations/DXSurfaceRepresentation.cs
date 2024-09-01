using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class DXSurfaceRepresentation : ISurfaceRepresentation {

    private DXReader dxR;
    public float isoValue = 0.0f;
    public MarchingCubesWrapper mcWrapper;

    public DXSurfaceRepresentation(string structName, UnityMolSelection sel, DXReader dx, float iso) {
        colorationType = colorType.full;
        useAO = false;
        if (dx == null) {
            throw new System.Exception("No DX map loaded");
        }
        mcWrapper = new MarchingCubesWrapper();

        isStandardSurface = false;

        dxR = dx;
        isoValue = iso;
        selection = sel;

        meshesGO = new List<GameObject>();
        meshColors = new Dictionary<GameObject, Color32[]>();
        colorByAtom = new Color32[sel.atoms.Count];
        chainToGo = new Dictionary<UnityMolChain, GameObject>();

        representationParent = UnityMolMain.getRepStructureParent(structName).transform;

        newRep = new GameObject("AtomDXSurfaceRepresentation");
        newRep.transform.parent = representationParent;
        representationTransform = newRep.transform;

        subSelections = new List<UnityMolSelection>(1) {selection};

        vertToAtom = new List<int[]>(subSelections.Count);

        mcWrapper.Init(dxR.densityValues, dxR.gradient,
                       dxR.gridSize, dxR.origin, dxR.deltaS, dxR.cellDir,
                       dxR.cellIdToAtomId, selection.structures[0]);

        foreach (UnityMolSelection s in subSelections) {
            displayDXSurfaceMesh(s.name + "_DXSurface", s, newRep.transform);
            // if (meshesGO.Count > 0) {
            //     computeNearestVertexPerAtom(meshesGO.Last(), s);
            // }
        }

        getMeshColors();

        Color32 white = Color.white;
        for (int i = 0; i < selection.atoms.Count; i++) {
            colorByAtom[i] = white;
        }

        newRep.transform.localPosition = Vector3.zero;
        newRep.transform.localRotation = Quaternion.identity;
        newRep.transform.localScale = Vector3.one;

        nbAtoms = selection.atoms.Count;

    }

    private void displayDXSurfaceMesh(string name, UnityMolSelection selection, Transform repParent) {

        GameObject go = createDXSurface(name, repParent);
        if (go != null) {
            meshesGO.Add(go);

            foreach (UnityMolAtom a in selection.atoms) {
                if (!chainToGo.ContainsKey(a.residue.chain)) {
                    chainToGo[a.residue.chain] = go;
                }
            }
        }
    }

    GameObject createDXSurface(string name, Transform repParent) {

        // MeshData mdata = MarchingCubesWrapper.callMarchingCubes(dxR.densityValues, dxR.gridSize, isoValue);
        MeshData mdata = mcWrapper.computeMC(isoValue);
        // MeshData mdata = null;

        if (mdata != null) {

            // if (mcWrapper.mcMode != MarchingCubesWrapper.MCType.CJOB) {
            //     mdata.Scale(dxR.delta);
            //     mdata.InvertX();
            //     mdata.Offset(dxR.origin);
            // }

            Mesh newMesh = new Mesh();
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            newMesh.vertices = mdata.vertices;
            newMesh.triangles = mdata.triangles;
            newMesh.normals = mdata.normals;
            newMesh.colors32 = mdata.colors;

            // newMesh.RecalculateNormals();

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(go.GetComponent<BoxCollider>());
            go.GetComponent<MeshFilter>().sharedMesh = newMesh;
            go.transform.SetParent(repParent);

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            go.name = name;
            if (normalMat == null) {
                normalMat = new Material(Shader.Find("Custom/SurfaceVertexColor"));
                normalMat.SetFloat("_Glossiness", 0.0f);
                normalMat.SetFloat("_Metallic", 0.0f);
                normalMat.SetFloat("_AOIntensity", 0.0f);
                currentMat = normalMat;
            }
            go.GetComponent<MeshRenderer>().sharedMaterial = currentMat;

            vertToAtom.Add(mdata.atomByVert);
            return go;
        }
        else {
            return null;
        }
    }


    public override void recompute(bool isTraj = false) {

        Clear();

        vertToAtom.Clear();

        foreach (UnityMolSelection sel in subSelections) {
            displayDXSurfaceMesh(sel.name, sel, newRep.transform);
        }

        getMeshColors();

        restoreColorsPerAtom();
    }

    public override void Clean() {

        vertToAtom.Clear();
        Clear();
        colorByAtom = null;
        meshColors.Clear();

        if (mcWrapper != null) {
            mcWrapper.FreeMC();
        }
        mcWrapper = null;
        if (normalMat != null)
            GameObject.Destroy(normalMat);
        if (transMat != null)
            GameObject.Destroy(transMat);
        if (wireMat != null)
            GameObject.Destroy(wireMat);
        if (transMatShadow != null)
            GameObject.Destroy(transMatShadow);
    }
}
}
