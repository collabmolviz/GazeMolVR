using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class NormalsVisualizer : MonoBehaviour {

    private Mesh mesh;
    private Mesh debugMesh;
    private GameObject curGo;

    void Start() {
        curGo = new GameObject("DebugNormals");
        curGo.transform.SetParent(transform);
        MeshRenderer mr = curGo.AddComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
        debugMesh = new Mesh();
        debugMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        curGo.AddComponent<MeshFilter>().sharedMesh = debugMesh;

        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        if (mf != null) {
            mesh = mf.sharedMesh;
            createNormalLines(mesh);
        }
    }

    void createNormalLines(Mesh m) {
        if (debugMesh == null) {
            debugMesh = new Mesh();
            debugMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        Vector3[] normals = m.normals;
        Vector3[] verts = m.vertices;

        int N = m.vertexCount;
        Vector3[] newVerts = new Vector3[N * 2];
        Color[] newcols = new Color[N * 2];
        int[] newTris = new int[N * 2];

        for (int i = 0; i < m.vertexCount; i++) {

            newVerts[i * 2] = transform.TransformPoint(verts[i]);
            newVerts[i * 2 + 1] = transform.TransformPoint(verts[i]) + transform.TransformVector(normals[i]);

            newTris[i * 2] = i * 2;
            newTris[i * 2 + 1] = i * 2 + 1;

            Color col = Color.white;
            if (i % 3 == 1) {
                col = Color.yellow;
            }
            if (i % 3 == 2)
                col = Color.blue;

            newcols[i * 2] = col;
            newcols[i * 2 + 1] = col;

        }
        debugMesh.SetVertices(newVerts);
        debugMesh.SetColors(newcols);
        debugMesh.SetIndices(newTris, MeshTopology.Lines, 0);
    }
    void OnDestroy() {
        if (debugMesh != null) {
            GameObject.Destroy(curGo.GetComponent<MeshRenderer>().sharedMaterial);
            GameObject.Destroy(debugMesh);
        }
    }
}