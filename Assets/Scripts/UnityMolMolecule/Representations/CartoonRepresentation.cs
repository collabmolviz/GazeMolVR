using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UMol
{
    public class CartoonRepresentation : AtomRepresentation
    {

        public List<GameObject> meshesGO;
        public Dictionary<UnityMolResidue, GameObject> residueToGo;
        public List<UnityMolAtom> savedAtoms = new List<UnityMolAtom>(); //Used to Save/Restore cartoon colors

        public Dictionary<UnityMolResidue, List<int>> residueToVert;

        public int totalVertices = 0;
        public float customTubeSize = 1.0f;
        public bool tube = false; //Draw as tube
        public bool bfactortube = false;//When tube is true, use bfactors to set sizes

        public Dictionary<GameObject, List<Color32>> meshColors;
        public List<Color32[]> savedColors = new List<Color32[]>();
        public Color32[] colorByRes = null;

        private List<List<UnityMolResidue>> residuesPerChain;

        private string structureName;
        private List<Segment> segments;
        private GameObject newRep;
        public Material ribbonMat;
        public Material transMat;
        private bool useHET = false;
        private bool useWAT = false;
        private bool isFullMesh = false;



        public CartoonRepresentation(int idF, string structName, UnityMolSelection sel,
                                     bool useHetatm = false, bool useWat = false,
                                     bool onlyTube = false, bool bfacTube = false, float coilTubeSize = 1.0f)
        {

            colorationType = colorType.defaultCartoon;
            meshesGO = new List<GameObject>();
            residueToGo = new Dictionary<UnityMolResidue, GameObject>();
            meshColors = new Dictionary<GameObject, List<Color32>>();

            if (ribbonMat == null)
            {
                ribbonMat = new Material(Shader.Find("Custom/SurfaceVertexColor"));
                ribbonMat.enableInstancing = true;
            }

            structureName = structName;
            selection = sel;
            useHET = useHetatm;
            useWAT = useWat;
            idFrame = idF;

            customTubeSize = coilTubeSize;
            tube = onlyTube;
            bfactortube = bfacTube;

            representationParent = UnityMolMain.getRepStructureParent(structName).transform;

            newRep = new GameObject("AtomCartoonRepresentation");
            newRep.transform.parent = representationParent;
            representationTransform = newRep.transform;


            if (!selection.sameModel())
            {

                segments = cutSelectionInSegments(selection);

                displayCartoonMesh(selection, structName, segments, newRep.transform);

            }
            else
            {
                UnityMolSelection biggerSel = getAllStructureSelection(selection);

                segments = cutSelectionInSegments(biggerSel);

                displayCartoonMesh(biggerSel, structName, segments, newRep.transform);

                if (biggerSel.Count != selection.atoms.Count)
                {
                    // Remove mesh parts not in selection
                    removeMeshParts(biggerSel);
                }
            }

            getMeshColors();

            newRep.transform.localPosition = Vector3.zero;
            newRep.transform.localRotation = Quaternion.identity;
            newRep.transform.localScale = Vector3.one;

            // // Generate periodic images
            // Vector3 per = selection.structures[0].periodic;
            // for (int i = -1; i < 2; i++) {
            //     for (int j = -1; j < 2; j++) {
            //         for (int k = -1; k < 2; k++) {
            //             if (i == 0 && j == 0 && k == 0)
            //                 continue;
            //             Vector3 cur = new Vector3(i, j, k);
            //             foreach (GameObject m in meshesGO) {
            //                 GameObject newM = GameObject.Instantiate(m);
            //                 newM.transform.parent = newRep.transform;
            //                 newM.transform.localScale = Vector3.one;
            //                 newM.transform.localRotation = Quaternion.identity;
            //                 newM.transform.localPosition = Vector3.Scale(per, cur);
            //             }
            //         }
            //     }
            // }


            // // Symmetry stored in the PDB
            // List<Matrix4x4> matrices = sel.structures[0].symMatrices;
            // int id = 0;
            // foreach (Matrix4x4 m in matrices) {
            //     for (int i = 0; i < meshesGO.Count; i++) {
            //         GameObject newMeshGo = GameObject.Instantiate(meshesGO[i]);
            //         newMeshGo.transform.parent = newRep.transform;

            //         newMeshGo.transform.localRotation = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
            //         newMeshGo.transform.localPosition = m.GetColumn(3);
            //         newMeshGo.transform.localScale = Vector3.one;
            //     }
            //     id++;
            // }

            nbAtoms = selection.atoms.Count;

            savedAtoms.Clear();
            foreach (UnityMolAtom a in selection.atoms)
            {
                savedAtoms.Add(a);
            }

            // To avoid vertex with NaN value
            for (int i = 0; i < newRep.transform.childCount; i++)
            {
                Mesh cartoonMesh = newRep.transform.GetChild(i).gameObject.GetComponent<MeshFilter>().mesh;
                Transform newRepTransform = newRep.transform;

                // Correct non-finite vertices in the mesh
                if (cartoonMesh != null)
                {
                    //CorrectNonFiniteVertices(cartoonMesh, newRepTransform); // Ensure non-finite values are corrected

                    MeshCollider cartoonMeshCollider = newRep.transform.GetChild(i).transform.gameObject.AddComponent<MeshCollider>();
                    cartoonMeshCollider.sharedMesh = cartoonMesh; // Assign the corrected mesh to the collider
                }

                newRep.transform.GetChild(i).gameObject.tag = "protein";
            }
        }

        /*
        // I am doing interpolation between prev and next vetex w.r.t. the NaN vertex
        void CorrectNonFiniteVertices(Mesh mesh, Transform parentTransform)
        {
            Vector3[] vertices = mesh.vertices;
            if (vertices.Length > 0)
            {
                // Iterate over vertices to find and correct non-finite values.
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (float.IsNaN(vertices[i].x) || float.IsNaN(vertices[i].y) || float.IsNaN(vertices[i].z))
                    {
                        // Non-finite vertex found, search for the next valid vertex for interpolation.
                        int nextValidIndex = FindNextValidVertexIndex(vertices, i + 1);

                        if (i > 0 && nextValidIndex > i)
                        {
                            // Both previous and next valid vertices are available, perform interpolation.
                            Vector3 prevValidVertex = vertices[i - 1];
                            Vector3 nextValidVertex = vertices[nextValidIndex];
                            vertices[i] = Vector3.Lerp(prevValidVertex, nextValidVertex, 0.5f); // Simple midpoint interpolation.
                        }
                        else if (i > 0)
                        {
                            // Only previous valid vertex is available, use it directly.
                            vertices[i] = vertices[i - 1];
                        }
                        else if (nextValidIndex > i)
                        {
                            // Only next valid vertex is available (happens at the start of the array), use it directly.
                            vertices[i] = vertices[nextValidIndex];
                        }
                        // Note: If neither previous nor next valid vertices are available, this leaves the vertex as-is,
                        // which shouldn't happen since we're ensuring at least one valid "next" vertex is found.

                        // Instantiate a debug sphere at the corrected position
                        Vector3 correctedPositionWorldSpace = parentTransform.TransformPoint(vertices[i]); // Convert to world space
                        GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        debugSphere.transform.position = correctedPositionWorldSpace;
                        debugSphere.transform.localScale = Vector3.one * 0.03f; // Adjust size as needed

                        Renderer sphereRenderer = debugSphere.GetComponent<Renderer>();
                        sphereRenderer.material.color = Color.red;

                        // Optionally, set the sphere's parent to keep the hierarchy clean
                        debugSphere.transform.SetParent(parentTransform);
                    }
                }

                // Apply corrected vertices back to the mesh.
                mesh.vertices = vertices;
                mesh.RecalculateBounds();
            }
        }

        // Helper method to find the next valid vertex index, returns -1 if not found.
        int FindNextValidVertexIndex(Vector3[] vertices, int startIndex)
        {
            for (int i = startIndex; i < vertices.Length; i++)
            {
                if (!float.IsNaN(vertices[i].x) && !float.IsNaN(vertices[i].y) && !float.IsNaN(vertices[i].z))
                {
                    return i; // Next valid vertex index found.
                }
            }
            return -1; // No valid vertex found.
        }
        */


        UnityMolSelection getAllStructureSelection(UnityMolSelection oriSel)
        {
            if (oriSel.structures.Count == 1)
            {
                return oriSel.structures[0].ToSelection();
            }
            List<UnityMolAtom> atoms = new List<UnityMolAtom>();

            foreach (UnityMolStructure s in oriSel.structures)
            {
                atoms.AddRange(s.currentModel.allAtoms);
            }

            UnityMolSelection res = new UnityMolSelection(atoms, null, "allStructCartoonSel", "");
            return res;
        }

        List<Segment> cutSelectionInSegments(UnityMolSelection sele)
        {
            List<Segment> res = new List<Segment>();
            Dictionary<string, Segment> segDict = new Dictionary<string, Segment>();

            for (int i = 0; i < sele.atoms.Count; i++)
            {
                UnityMolAtom atom = sele.atoms[i];
                if (!useHET && atom.isHET)
                {
                    continue;
                }
                if (!useWAT && WaterSelection.waterResidues.Contains(atom.residue.name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                string segKey = atom.residue.chain.model.structure.name + "_" +
                                atom.residue.chain.model.name + "_" +
                                atom.residue.chain.name;
                Segment curSeg;
                if (segDict.TryGetValue(segKey, out curSeg))
                {
                    curSeg.residues.Add(atom.residue);
                }
                else
                {
                    curSeg = new Segment(atom.residue);
                    res.Add(curSeg);
                    segDict[segKey] = curSeg;
                }
            }
            for (int seg = 0; seg < res.Count; seg++)
            {
                res[seg].residues = res[seg].residues.Distinct().ToList();
            }

            return res;
        }

        private void removeMeshParts(UnityMolSelection bigSel)
        {

            if (isFullMesh)
            {
                return;
            }
            Dictionary<GameObject, HashSet<int>> vertIdToDel = new Dictionary<GameObject, HashSet<int>>();
            HashSet<UnityMolResidue> oriRes = new HashSet<UnityMolResidue>();

            foreach (UnityMolAtom a in selection.atoms)
            {
                oriRes.Add(a.residue);
            }
            HashSet<UnityMolResidue> residuesToDel = new HashSet<UnityMolResidue>();

            //Get all the residues to delete
            foreach (UnityMolAtom a in bigSel.atoms)
            {
                if (!useWAT && WaterSelection.waterResidues.Contains(a.residue.name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!useHET && a.isHET)
                {
                    continue;
                }
                if (!oriRes.Contains(a.residue))
                {
                    residuesToDel.Add(a.residue);
                }
            }
            if (residuesToDel.Count == 0)
            {
                isFullMesh = true;
                return;
            }


            int totalToDel = 0;
            //For each residue to delete, record the vertices to hide
            foreach (UnityMolResidue r in residuesToDel)
            {
                List<int> listVertId;
                GameObject curGo = null;

                if (residueToGo.TryGetValue(r, out curGo) && residueToVert.TryGetValue(r, out listVertId))
                {
                    if (!vertIdToDel.ContainsKey(curGo))
                    {
                        vertIdToDel[curGo] = new HashSet<int>();
                    }
                    foreach (int c in listVertId)
                    {
                        vertIdToDel[curGo].Add(c);
                        totalToDel++;
                    }
                }
            }

            if (totalToDel == 0)
            {
                isFullMesh = true;
                return;
            }


            //Hide the triangles that contains at least one of the vertex to hide
            foreach (GameObject go in vertIdToDel.Keys)
            {

                Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
                Vector3[] vertices = m.vertices;
                int[] triangles = m.triangles;
                List<int> newTri = new List<int>(triangles.Length);

                for (int t = 0; t < triangles.Length - 3; t += 3)
                {
                    if (vertIdToDel[go].Contains(triangles[t]))
                    {
                        continue;
                    }
                    if (vertIdToDel[go].Contains(triangles[t + 1]))
                    {
                        continue;
                    }
                    if (vertIdToDel[go].Contains(triangles[t + 2]))
                    {
                        continue;
                    }
                    newTri.Add(triangles[t]);
                    newTri.Add(triangles[t + 1]);
                    newTri.Add(triangles[t + 2]);
                }

                m.SetTriangles(newTri, 0);
            }

        }


        public static string getPreComputedKey(UnityMolChain c)
        {
            return c.model.structure.name + "_" + c.model.name + "_" + c.name + "_Cartoon";
        }

        public void displayCartoonMesh(UnityMolSelection sel, string structName, List<Segment> segments, Transform repParent, float ribbonWidth = 4.5f,
                                       float bRad = 0.3f, int bRes = 10, bool useBspline = true, bool isTraj = false)
        {


            // float start = Time.realtimeSinceStartup;
            savedColors = new List<Color32[]>();
            residueToVert = new Dictionary<UnityMolResidue, List<int>>();

            for (int seg = 0; seg < segments.Count; seg++)
            {

                Dictionary<UnityMolResidue, List<int>> segResidueToVert = new Dictionary<UnityMolResidue, List<int>>();
                int nbRes = segments[seg].residues.Count;
                if (nbRes < 2)
                {
                    continue;
                }

                bool isProt = isSegmentProt(segments[seg].residues);
                bool isNucleic = isSegmentNucleic(segments[seg].residues);

                MeshData mesh = null;
                string nameCartoonMesh = null;
                UnityMolResidue r = segments[seg].residues[0];
                string keyPrecomputedRep = getPreComputedKey(r.chain);
                bool alreadyComputed = UnityMolMain.getPrecompRepManager().ContainsRep(keyPrecomputedRep);

                if (selection.extractTrajFrame)
                {
                    alreadyComputed = false;
                }

                if (alreadyComputed && !tube && customTubeSize == 1.0f)
                {

                    mesh = UnityMolMain.getPrecompRepManager().precomputedRep[keyPrecomputedRep];
                    Dictionary<UnityMolResidue, List<int>> tmpResVert = UnityMolMain.getPrecompRepManager().precomputedCartoonAsso[keyPrecomputedRep];
                    foreach (UnityMolResidue res in tmpResVert.Keys)
                    {
                        segResidueToVert[res] = tmpResVert[res];
                    }
                }
                else
                {
                    if (isNucleic)
                    {
                        if (segments[seg].residues[0].chain.model.structure.structureType ==
                                UnityMolStructure.MolecularType.Martini)
                        {
                            Debug.Log("Martini DNA");
                            mesh = RibbonMeshMartiniDNA.createChainMesh(idFrame, sel, segments[seg].residues, ref segResidueToVert, isTraj);
                        }
                        else
                        {
                            mesh = RibbonMeshDNA.createChainMesh(idFrame, sel, segments[seg].residues, ref segResidueToVert, isTraj);
                        }
                    }
                    else
                    { //Try with standard cartoon / Martini cartoon
                        if (segments[seg].residues[0].chain.model.structure.structureType ==
                                UnityMolStructure.MolecularType.Martini)
                        {
                            mesh = RibbonMeshMartini.createChainMesh(idFrame, sel, segments[seg].residues, ref segResidueToVert, isTraj);
                        }
                        else
                        {
                            mesh = RibbonMesh.createChainMesh(idFrame, sel, segments[seg].residues, ref segResidueToVert, customTubeSize, tube, bfactortube, isTraj);
                        }
                    }

                    if (!selection.extractTrajFrame && mesh != null && mesh.vertices != null && !tube && customTubeSize == 1.0f)
                    {
                        UnityMolMain.getPrecompRepManager().precomputedRep[keyPrecomputedRep] = mesh;
                        UnityMolMain.getPrecompRepManager().precomputedCartoonAsso[keyPrecomputedRep] = segResidueToVert;
                    }
                }
                if (mesh != null && mesh.vertices != null && mesh.vertices.Length > 3 && !float.IsNaN(mesh.vertices[0].x))
                {
                    nameCartoonMesh = structName + "_" + segments[seg].residues[0].chain.name + "_CartoonMesh";
                    createUnityMesh(segments[seg], repParent, nameCartoonMesh, mesh, ribbonMat);
                    totalVertices += mesh.vertices.Length;
                }

                foreach (UnityMolResidue res in segResidueToVert.Keys)
                {
                    residueToVert[res] = segResidueToVert[res];
                }

            }
            // Debug.Log("Time for cartoon: " + (1000.0f * (Time.realtimeSinceStartup - start)).ToString("f3") + " ms");
        }

        bool isSegmentProt(List<UnityMolResidue> residues)
        {
            int cptProt = 0;
            foreach (UnityMolResidue r in residues)
            {
                if (MDAnalysisSelection.isProtein(r))
                {
                    cptProt++;
                }
                if (cptProt >= residues.Count / 3)
                {
                    return true;
                }
            }
            return false;
        }

        bool isSegmentNucleic(List<UnityMolResidue> residues)
        {
            int cptNucl = 0;
            foreach (UnityMolResidue r in residues)
            {
                if (MDAnalysisSelection.isNucleic(r))
                {
                    cptNucl++;
                }
                if (cptNucl >= Mathf.Min(10, residues.Count))
                {
                    return true;
                }
            }
            return false;
        }



        /*
        void createUnityMesh(Segment seg, Transform parent, string name,
                             Vector3[] vertices, Vector3[] normals, int[] triangles, Color32[] colors, Material ribbonMat)
        {

            //Resources.Load("Materials/standardColorSpecular") as Material;
            if (ribbonMat == null)
            {
                ribbonMat = new Material(Shader.Find("Custom/SurfaceVertexColor"));
                ribbonMat.enableInstancing = true;
            }
            Mesh m = new Mesh();
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            m.vertices = vertices;

            m.triangles = triangles;
            m.colors32 = colors;

            savedColors.Add(colors);

            if (normals != null)
                m.normals = normals;
            else
            {
                m.RecalculateNormals();
                //High quality smoothing normals
                // m.RecalculateNormals(60);
            }

            GameObject go = new GameObject(name);
            MeshFilter mf = go.AddComponent<MeshFilter>();

            mf.sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = ribbonMat;

            go.transform.parent = parent;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            meshesGO.Add(go);
            foreach (UnityMolResidue r in seg.residues)
            {
                if (!residueToGo.ContainsKey(r))
                    residueToGo.Add(r, go);
            }
        }
        */

        void createUnityMesh(Segment seg, Transform parent, string name,
                     Vector3[] vertices, Vector3[] normals, int[] triangles, Color32[] colors, Material ribbonMat)
        {
            // Initialize lists to hold valid data
            List<Vector3> validVertices = new List<Vector3>();
            List<Vector3> validNormals = normals != null ? new List<Vector3>() : null;
            List<Color32> validColors = colors != null ? new List<Color32>() : null;
            List<int> validTriangles = new List<int>();

            // Mapping from old vertex indices to new ones
            Dictionary<int, int> oldToNewIndices = new Dictionary<int, int>();

            // Filter out NaN vertices and update normals and colors accordingly
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!float.IsNaN(vertices[i].x) && !float.IsNaN(vertices[i].y) && !float.IsNaN(vertices[i].z))
                {
                    oldToNewIndices[i] = validVertices.Count;
                    validVertices.Add(vertices[i]);

                    if (validNormals != null) validNormals.Add(normals[i]);
                    if (validColors != null) validColors.Add(colors[i]);
                }
            }

            // Update triangles to reference the new indices
            foreach (int tri in triangles)
            {
                if (oldToNewIndices.TryGetValue(tri, out int newTriIndex))
                {
                    validTriangles.Add(newTriIndex);
                }
            }

            // Check if material is set, if not, create a new material
            if (ribbonMat == null)
            {
                ribbonMat = new Material(Shader.Find("Custom/SurfaceVertexColor"));
                ribbonMat.enableInstancing = true;
            }

            // Create the mesh with the validated data
            Mesh m = new Mesh();
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = validVertices.ToArray();
            m.triangles = validTriangles.ToArray();

            // Optional data
            if (validNormals != null && validNormals.Count > 0)
                m.normals = validNormals.ToArray();
            else
                m.RecalculateNormals(); // Recalculate if no normals provided

            if (validColors != null && validColors.Count > 0)
                m.colors32 = validColors.ToArray();

            // Save the processed colors for later use
            savedColors.Add(m.colors32);

            // Setup the GameObject
            GameObject go = new GameObject(name);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = ribbonMat;
            go.transform.parent = parent;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            // Add the new GameObject to the list and update the residue to GameObject mapping
            meshesGO.Add(go);
            foreach (UnityMolResidue r in seg.residues)
            {
                if (!residueToGo.ContainsKey(r))
                    residueToGo.Add(r, go);
            }
        }



        void createUnityMesh(Segment seg, Transform parent, string name, MeshData meshD, Material ribbonMat)
        {
            createUnityMesh(seg, parent, name, meshD.vertices, meshD.normals, meshD.triangles, meshD.colors, ribbonMat);
        }

        private void getMeshColors()
        {
            meshColors.Clear();
            foreach (GameObject go in meshesGO)
            {
                Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
                Color32[] tmpArrayColor = m.colors32;
                if (tmpArrayColor == null || tmpArrayColor.Length == 0)
                {
                    tmpArrayColor = new Color32[m.vertexCount];
                }
                meshColors[go] = tmpArrayColor.ToList();
            }
        }

        private void getMeshColorsPerResidue()
        {
            //Warning: This function expects that getMeshColors() was called before!
            if (colorByRes == null)
            {
                colorByRes = new Color32[residueToGo.Keys.Count];
            }

            int idR = 0;
            foreach (UnityMolResidue r in residueToGo.Keys)
            {
                GameObject curGo = null;
                Color32 col = Color.white;
                List<int> listVertId;

                if (residueToGo.TryGetValue(r, out curGo))
                {
                    List<Color32> colors = meshColors[curGo];
                    if (residueToVert.TryGetValue(r, out listVertId))
                    {
                        if (listVertId.Count != 0 && listVertId[0] >= 0 && listVertId[0] < colors.Count)
                        {
                            col = colors[listVertId[0]];
                        }
                    }
                }

                colorByRes[idR++] = col;
            }
        }

        private void restoreColorsPerResidue()
        {
            if (colorByRes != null && residueToGo != null)
            {

                getMeshColors();
                int idR = 0;

                foreach (UnityMolResidue r in residueToGo.Keys)
                {
                    List<int> listVertId;
                    GameObject curGo = null;
                    Color32 col = colorByRes[idR++];

                    if (residueToGo.TryGetValue(r, out curGo))
                    {
                        List<Color32> colors = meshColors[curGo];

                        if (residueToVert.TryGetValue(r, out listVertId))
                        {
                            foreach (int c in listVertId)
                            {
                                if (c >= 0 && c < colors.Count)
                                {
                                    colors[c] = col;
                                }
                            }
                        }
                    }
                }
                foreach (GameObject go in meshesGO)
                {
                    go.GetComponent<MeshFilter>().sharedMesh.SetColors(meshColors[go]);
                }
            }
        }

        public void recompute(bool isNewModel = false)
        {

            Material curMat = null;

            foreach (GameObject m in meshesGO)
            {
                curMat = m.GetComponent<MeshRenderer>().sharedMaterial;
                break;
            }

            // if (!selection.structures[0].updateSSWithTraj) {
            getMeshColorsPerResidue();
            // }

            //------
            // Clean();
            if (meshesGO != null)
            {
                foreach (GameObject go in meshesGO)
                {
                    GameObject.Destroy(go.GetComponent<MeshFilter>().sharedMesh);
                    GameObject.Destroy(go);
                }
                meshesGO.Clear();
                residueToGo.Clear();
                residueToVert.Clear();
            }
            //------


            if (isNewModel)
            {
                segments = cutSelectionInSegments(selection);
                displayCartoonMesh(selection, structureName, segments, newRep.transform, isTraj: false);
            }
            else
            { //If trajectory then make sure we have a faster way to compute the cartoon.
              // Right now we just lower the cartoon quality settings
                displayCartoonMesh(selection, structureName, segments, newRep.transform, isTraj: true);
            }

            if (meshesGO.Count > 0)
            {
                foreach (GameObject m in meshesGO)
                {
                    m.GetComponent<MeshRenderer>().sharedMaterial = curMat;
                }
            }

            UnityMolSelection biggerSel = getAllStructureSelection(selection);
            if (biggerSel.Count != selection.atoms.Count)
            {
                //Remove mesh parts not in selection
                removeMeshParts(biggerSel);
            }
            // if (!selection.structures[0].updateSSWithTraj) {
            restoreColorsPerResidue();
            // } else {
            // getMeshColors();
            // }
        }

        public override void Clean()
        {
            if (meshesGO != null)
            {
                foreach (GameObject go in meshesGO)
                {
                    GameObject.Destroy(go.GetComponent<MeshFilter>().sharedMesh);
                    GameObject.Destroy(go);
                }
                meshesGO.Clear();
            }
            if (ribbonMat != null)
            {
                GameObject.Destroy(ribbonMat);
                ribbonMat = null;
            }
            if (transMat != null)
            {
                GameObject.Destroy(transMat);
                transMat = null;
            }
            meshesGO = null;
            if (residueToGo != null)
                residueToGo.Clear();
            residueToGo = null;
            if (residueToVert != null)
                residueToVert.Clear();
            residueToVert = null;

            if (savedAtoms != null)
                savedAtoms.Clear();
            savedAtoms = null;
            if (meshColors != null)
                meshColors.Clear();
            meshColors = null;
            if (savedColors != null)
                savedColors.Clear();
            savedColors = null;
            colorByRes = null;
            if (residuesPerChain != null)
                residuesPerChain.Clear();
            residuesPerChain = null;
            if (segments != null)
                segments.Clear();
            segments = null;
        }
    }

    public class Segment
    {
        public List<UnityMolResidue> residues;


        public Segment(UnityMolResidue residue)
        {
            residues = new List<UnityMolResidue>();
            residues.Add(residue);
        }

    }
}