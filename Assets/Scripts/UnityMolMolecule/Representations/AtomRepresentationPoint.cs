using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Text;
using System.Collections.Generic;


namespace UMol {
public class AtomRepresentationPoint : AtomRepresentation {


    public GameObject meshGO;
    public Dictionary<UnityMolAtom, int> atomToId;
    // public List<Color> atomColors;
    // public bool withShadow = true;


    public AtomRepresentationPoint(int idF, string structName, UnityMolSelection sel) {
        colorationType = colorType.atom;

        representationParent = UnityMolMain.getRepStructureParent(structName).transform;
        
        GameObject newRep = new GameObject("AtomRepresentationPoint");
        newRep.transform.parent = representationParent;
        representationTransform = newRep.transform;

        selection = sel;
        idFrame = idF;

        atomToId = new Dictionary<UnityMolAtom, int>();

        DisplayPoints(newRep.transform);

        newRep.transform.localPosition = Vector3.zero;
        newRep.transform.localRotation = Quaternion.identity;
        newRep.transform.localScale = Vector3.one;

        nbAtoms = selection.atoms.Count;

    }
    void DisplayPoints(Transform repParent) {

        meshGO = new GameObject("AtomRepPoints");
        meshGO.transform.parent = repParent;

        var mesh = createMeshForPoints();
        var meshFilter = meshGO.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;


        var meshRenderer = meshGO.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Point Cloud/Disk"));        
    }

    Mesh createMeshForPoints() {
        var mesh = new Mesh();
        int N = selection.atoms.Count;
        mesh.indexFormat = selection.atoms.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

        Vector3[] vertices = new Vector3[N];
        Color32[] colors = new Color32[N];
        int[] indices = new int[N];

        if (idFrame != -1) {
            for (int j = 0; j < N; j++) {
                vertices[j] = selection.extractTrajFramePositions[idFrame][j];
                colors[j] = selection.atoms[j].color;
                indices[j] = j;
                atomToId[selection.atoms[j]] = j;
            }
        }

        else {
            for (int i = 0; i < N; i++) {
                vertices[i] = selection.atoms[i].position;
                colors[i] = selection.atoms[i].color;
                indices[i] = i;
                atomToId[selection.atoms[i]] = i;
            }
        }
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetIndices(indices, MeshTopology.Points, 0);

        return mesh;
    }

    public override void Clean() {
        if(meshGO != null){
            GameObject.Destroy(meshGO.GetComponent<MeshFilter>().sharedMesh);
            GameObject.Destroy(meshGO.GetComponent<MeshRenderer>().sharedMaterial);
        }
        if(atomToId != null)
            atomToId.Clear();
        atomToId = null;
    }
}
}
