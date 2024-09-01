using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;


namespace UMol {
public class BondRepresentationHbonds : BondRepresentation {

    public List<GameObject> meshesGO;
    public Mesh curMesh;
    public Dictionary<UnityMolAtom, List<GameObject>> atomToGo;
    public Dictionary<UnityMolAtom, List<Mesh>> atomToMeshes;
    public Dictionary<UnityMolAtom, List<int>> atomToVertices;

    public UnityMolBonds hbonds;
    private GameObject newRep;
    private Material hbondMat;

    public bool isCustomHbonds = false;

    /// If customHbonds is true, use the bonds from the selection,
    /// else run hbond detection algorithm
    public BondRepresentationHbonds(int idF, string structName, UnityMolSelection sel, bool customHbonds = false) {

        colorationType = colorType.full;
        
        representationParent = UnityMolMain.getRepStructureParent(structName).transform;


        newRep = new GameObject("BondHbondRepresentation");
        newRep.transform.parent = representationParent;
        representationTransform = newRep.transform;

        if(hbondMat == null)
            hbondMat = Resources.Load("Materials/hbondsTransparentUnlit") as Material;

        AnimateHbonds anim = newRep.AddComponent<AnimateHbonds>();
        anim.hbondMat = hbondMat;

        selection = sel;
        idFrame = idF;

        if (!customHbonds) {
            hbonds = HbondDetection.DetectHydrogenBonds(sel, idFrame, selection.atomToIdInSel);
        }
        else {
            hbonds = sel.bonds;
            isCustomHbonds = true;
        }

        DisplayHBonds(newRep.transform);

        newRep.transform.localPosition = Vector3.zero;
        newRep.transform.localRotation = Quaternion.identity;
        newRep.transform.localScale = Vector3.one;

        //Don't do that to avoid updating the representation every time showSelection is called
        // nbBonds = hbonds.Count;
        nbBonds = sel.bonds.Count;
    }

    public void DisplayHBonds(Transform repParent) {

        int nbSticks = hbonds.Length;


        meshesGO = new List<GameObject>();
        atomToMeshes = new Dictionary<UnityMolAtom, List<Mesh>>();
        atomToVertices = new Dictionary<UnityMolAtom, List<int>>();
        atomToGo = new Dictionary<UnityMolAtom, List<GameObject>>();

        if (nbSticks == 0)
            return;

        int countBond = 0;

        UnityMolModel curM = selection.atoms[0].residue.chain.model;

        HashSet<int2> doneBonds = new HashSet<int2>();
        int2 k, invk;
        foreach (int ida in hbonds.bonds.Keys) {
            UnityMolAtom atom1 = curM.allAtoms[ida];
            foreach (int idb in hbonds.bonds[ida]) {
                if (idb != -1) {
                    k.x = ida; invk.x = idb;
                    k.y = idb; invk.y = ida;
                    if (doneBonds.Contains(k) || doneBonds.Contains(invk))
                        continue;

                    doneBonds.Add(k);
                    UnityMolAtom atom2 = curM.allAtoms[idb];

                    // GameObject currentGO = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/HbondPrefab"));
                    // currentGO.name = "BondHBond_" + countBond + "_" + atom1.number + "/" + atom2.number;
                    GameObject currentGO = new GameObject("BondHBond_" + "BondHBond_" + countBond + "_" + atom1.number + "/" + atom2.number);
                    currentGO.transform.parent = repParent;
                    currentGO.transform.localRotation = Quaternion.identity;
                    currentGO.transform.localPosition = Vector3.zero;
                    currentGO.transform.localScale = Vector3.one;

                    Mesh curMesh = createQuadMesh(atom1, atom2);

                    if (!atomToGo.ContainsKey(atom1)) {
                        atomToGo[atom1] = new List<GameObject>();
                    }

                    if (!atomToGo.ContainsKey(atom2)) {
                        atomToGo[atom2] = new List<GameObject>();
                    }

                    atomToGo[atom1].Add(currentGO);
                    atomToGo[atom2].Add(currentGO);



                    MeshFilter mf = currentGO.AddComponent<MeshFilter>();
                    mf.sharedMesh = curMesh;
                    MeshRenderer mr = currentGO.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = hbondMat;


                    meshesGO.Add(currentGO);
                    countBond++;
                }
            }
        }
    }



    Mesh createQuadMesh(UnityMolAtom atom1, UnityMolAtom atom2, float lineWidth = 0.5f) {

        Vector3[] newVertices = new Vector3[4];
        Vector2[] newUV = new Vector2[4];
        Color32[] newColors = new Color32[4];
        int[] newTriangles = new int[6];

        Vector3 start = atom1.position;
        Vector3 end   = atom2.position;
        if (idFrame != -1) {
            int iida = selection.atomToIdInSel[atom1];
            start = selection.extractTrajFramePositions[idFrame][iida];
            iida = selection.atomToIdInSel[atom2];
            end = selection.extractTrajFramePositions[idFrame][iida];
        }


        Vector3 normal = Vector3.Cross(start, end);
        Vector3 side = Vector3.Cross(normal, end - start);
        side.Normalize();

        Vector3 a = start + side * (lineWidth / 2);
        Vector3 b = start - side * (lineWidth / 2);
        Vector3 c = end + side * (lineWidth / 2);
        Vector3 d = end - side * (lineWidth / 2);


        //A quad per bond

        int ida = 0;
        newVertices[0] = a;
        newVertices[1] = b;
        newVertices[2] = c;
        newVertices[3] = d;

        newTriangles[0] = 0;
        newTriangles[1] = 1; //b
        newTriangles[2] = 2; //c

        newTriangles[3] = 2;
        newTriangles[4] = 1; //c
        newTriangles[5] = 3; //d

        newUV[0] = Vector2.zero;
        newUV[1] = new Vector2(0, 1);
        newUV[2] = new Vector2(1, 0);
        newUV[3] = Vector2.one;


        newColors[0] = Color.white;
        newColors[1] = Color.white;
        newColors[2] = Color.white;
        newColors[3] = Color.white;



        Mesh curMesh = new Mesh();

        curMesh.vertices = newVertices;
        curMesh.triangles = newTriangles;
        curMesh.colors32 = newColors;
        curMesh.uv = newUV;
        curMesh.RecalculateNormals();


        if (atomToMeshes.ContainsKey(atom1)) {
            atomToMeshes[atom1].Add(curMesh);
            atomToVertices[atom1].Add(ida);
            atomToVertices[atom1].Add(ida + 1);

        }
        else {
            atomToMeshes[atom1] = new List<Mesh>();
            atomToVertices[atom1] = new List<int>();
            atomToMeshes[atom1].Add(curMesh);
            atomToVertices[atom1].Add(ida);
            atomToVertices[atom1].Add(ida + 1);

        }

        if (atomToMeshes.ContainsKey(atom2)) {
            atomToMeshes[atom2].Add(curMesh);
            atomToVertices[atom2].Add(ida + 2);
            atomToVertices[atom2].Add(ida + 3);

        }
        else {
            atomToMeshes[atom2] = new List<Mesh>();
            atomToVertices[atom2] = new List<int>();
            atomToMeshes[atom2].Add(curMesh);
            atomToVertices[atom2].Add(ida + 2);
            atomToVertices[atom2].Add(ida + 3);

        }
        return curMesh;
    }

    public override void Clean() {
        if (meshesGO != null) {//Already destroyed by the manager
            foreach (GameObject go in meshesGO) {
                GameObject.Destroy(go.GetComponent<MeshFilter>().sharedMesh);
                GameObject.Destroy(go);
            }
            meshesGO.Clear();
        }

        //Don't do that because the material is not dynamically created, this is an asset
        //GameObject.Destroy(hbondMat);
        hbondMat = null;
        meshesGO = null;

        atomToGo.Clear();
        atomToMeshes.Clear();
        atomToVertices.Clear();
    }

    public void recompute() {
        //Clean without destroying material
        if (meshesGO != null) {
            foreach (GameObject go in meshesGO) {
                GameObject.Destroy(go.GetComponent<MeshFilter>().sharedMesh);
                GameObject.Destroy(go);
            }
            meshesGO.Clear();
        }

        atomToGo.Clear();
        atomToMeshes.Clear();
        atomToVertices.Clear();

        if (!isCustomHbonds)
            hbonds = HbondDetection.DetectHydrogenBonds(selection, idFrame, selection.atomToIdInSel);
        else
            hbonds = selection.bonds;

        DisplayHBonds(newRep.transform);
    }
    public void recomputeLight() {
        //Clean without destroying material
        if (meshesGO != null) {
            foreach (GameObject go in meshesGO) {
                GameObject.Destroy(go.GetComponent<MeshFilter>().sharedMesh);
                GameObject.Destroy(go);
            }
            meshesGO.Clear();
        }

        atomToGo.Clear();
        atomToMeshes.Clear();
        atomToVertices.Clear();

        DisplayHBonds(newRep.transform);
    }
}
}