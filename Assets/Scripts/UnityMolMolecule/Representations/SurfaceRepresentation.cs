using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UMol
{
    public class SurfaceRepresentation : ISurfaceRepresentation
    {

        public SurfMethod surfMethod;
        public bool isCutSurface;
        public bool isCutByChain;
        public float probeRadius = 1.4f;

        ///Store computed surface info to avoid allocations in trajectory
        private List<MeshData> allMeshData;

        public SurfaceRepresentation(int idF, string structName, UnityMolSelection sel, bool cutByChain = true,
                                     bool AO = true, bool cutSurface = true, SurfMethod method = SurfMethod.EDTSurf, float probeRad = 1.4f)
        {

            colorationType = colorType.full;
            meshesGO = new List<GameObject>();
            meshColors = new Dictionary<GameObject, Color32[]>();
            chainToGo = new Dictionary<UnityMolChain, GameObject>();
            chainToIdSubSel = new Dictionary<UnityMolChain, int>();

            colorByAtom = new Color32[sel.atoms.Count];
            normalMat = new Material(Shader.Find("Custom/SurfaceVertexColor"));
            normalMat.enableInstancing = true;


            selection = sel;
            surfMethod = method;
            useAO = AO;
            isCutByChain = cutByChain;
            isCutSurface = cutSurface;
            probeRadius = probeRad;
            idFrame = idF;

#if (!UNITY_EDITOR_WIN) && (!UNITY_STANDALONE_WIN)
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore) {
            //Desactivate AO for non windows bc of a bug in the AO implementation
            useAO = false;
        }
#endif

            //Disable AO for quicksurf by default
            if (surfMethod == SurfMethod.QUICKSURF)
            {
                useAO = false;
            }

            normalMat.SetFloat("_Glossiness", 0.0f);
            normalMat.SetFloat("_Metallic", 0.0f);
            if (useAO)
            {
                normalMat.SetFloat("_AOIntensity", 8.0f);
            }
            else
            {
                normalMat.SetFloat("_AOIntensity", 0.0f);
            }

            currentMat = normalMat;


            representationParent = UnityMolMain.getRepStructureParent(structName).transform;

            newRep = new GameObject("AtomSurfaceRepresentation");
            newRep.transform.parent = representationParent;
            representationTransform = newRep.transform;

            //TODO: implement this!
            if (sel.extractTrajFrame && isCutSurface)
            {
                Debug.LogWarning("Cutting surfaces for frames extracted from a trajectory is not supported for now");
                isCutSurface = false;
            }

            if (isCutByChain)
            {
                subSelections = cutSelection(selection);
            }
            else
            {
                subSelections = new List<UnityMolSelection>() { selection };
            }

            allMeshData = new List<MeshData>(subSelections.Count);
            vertToAtom = new List<int[]>(subSelections.Count);


            //Check if we try to compute the surface for ligand or hetero atoms
            if (!sel.extractTrajFrame && isCutSurface)
            {

                if (checkHeteroOrLigand(selection))
                {
                    isCutSurface = false;
                    Debug.LogWarning("Forcing cut surface off, the selection contains only ligand or hetero atoms");
                }
                if (!selection.sameModel())
                {
                    isCutSurface = false;
                    Debug.LogWarning("Forcing cut surface off, the selection contains atoms from different models");
                }
            }

            float timerSES = Time.realtimeSinceStartup;

            int idSubSel = 0;
            foreach (UnityMolSelection s in subSelections)
            {
                int[] atomIdPerVert = null;
                bool success = displaySurfaceMesh(s.name, s, newRep.transform, ref atomIdPerVert, idSubSel, false);
                if (success)
                {
                    if (atomIdPerVert == null)
                    {//Shouldn't happen as all surf methods generate atomIdPerVert array
                        atomIdPerVert = computeNearestVertexPerAtom(meshesGO.Last(), s);
                        vertToAtom.Add(atomIdPerVert);
                    }
                }
                idSubSel++;
            }

#if UNITY_EDITOR
            Debug.Log("Total time : " + (1000.0f * (Time.realtimeSinceStartup - timerSES)).ToString("f3") + " ms");
#endif


            if (meshesGO.Count > 0 && useAO)
            {
                try
                {
                    GameObject aoGo = new GameObject("tmpAO");
                    geoAO aoScript = aoGo.AddComponent<geoAO>();
                    aoScript.ComputeAO(meshesGO);
                    GameObject.Destroy(aoGo);
                }
                catch
                {
                    Debug.LogWarning("Could not compute AO");
                }
            }

            getMeshColors();

            Color32 white = Color.white;
            int id = 0;
            foreach (UnityMolAtom a in selection.atoms)
            {
                colorByAtom[id++] = white;
            }

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

            for (int i = 0; i < newRep.transform.childCount; i++)
            {
                Mesh atomSurfacemesh = newRep.transform
                    .GetChild(i)
                    .gameObject.GetComponent<MeshFilter>()
                    .mesh;
                if (atomSurfacemesh != null)
                {
                    MeshCollider atomSurfacemeshMeshCollider = newRep.transform
                        .GetChild(i)
                        .transform.gameObject.AddComponent<MeshCollider>();
                    atomSurfacemeshMeshCollider.sharedMesh = atomSurfacemesh;
                }
                newRep.transform.GetChild(i).gameObject.tag = "protein";
            }

        }

        private bool displaySurfaceMesh(string name, UnityMolSelection selection,
                                        Transform repParent, ref int[] atomIdPerVert,
                                        int idSubSel, bool isTraj)
        {

            if (selection.atoms.Count == 0)
            {
                return false;
            }

            UnityMolSelection sel = selection;
            MeshData meshd = null;

            if (isTraj && idSubSel < allMeshData.Count)
            {
                meshd = allMeshData[idSubSel];
            }


            if (!sel.extractTrajFrame)
            {

                if (isCutSurface && isCutByChain)
                {
                    sel = selection.atoms[0].residue.chain.ToSelection(false);
                    sel.isAlterable = true;
                }
                else if (isCutSurface && !isCutByChain)
                {
                    sel = selection.atoms[0].residue.chain.model.structure.ToSelection();
                    sel.isAlterable = true;
                }
            }

            string keyPrecomputedRep = sel.atoms[0].residue.chain.model.structure.name + "_" + sel.atoms[0].residue.chain.name + "_" + surfMethod.ToString();

            if (sel.atoms.Count <= 10)
            {
                Debug.LogWarning("Forcing MSMS surface for small selections");
                MSMSWrapper.createMSMSSurface(idFrame, repParent, name, sel, ref meshd, probeRad: probeRadius);
                atomIdPerVert = meshd.atomByVert;

                if (!sel.extractTrajFrame && meshd != null && meshd.vertices != null)
                { //Save the surface as precomputed
                    UnityMolMain.getPrecompRepManager().precomputedRep[keyPrecomputedRep] = meshd;
                    UnityMolMain.getPrecompRepManager().precomputedSurfAsso[keyPrecomputedRep] = atomIdPerVert;
                }
            }
            else
            {

                bool alreadyComputed = UnityMolMain.getPrecompRepManager().ContainsRep(keyPrecomputedRep);
                if (sel.extractTrajFrame)
                {
                    alreadyComputed = false;
                }

                if (isCutSurface && isCutByChain)
                { //Use precomputed surface

                    if (alreadyComputed)
                    { //Already pre-computed => use the saved mesh
                        meshd = UnityMolMain.getPrecompRepManager().precomputedRep[keyPrecomputedRep];
                        //Restore association between atoms and vertices
                        atomIdPerVert = UnityMolMain.getPrecompRepManager().precomputedSurfAsso[keyPrecomputedRep];
                    }
                    else
                    { //Not precomputed => compute it
                        if (surfMethod == SurfMethod.MSMS)
                        {
                            MSMSWrapper.createMSMSSurface(idFrame, repParent, name, sel, ref meshd, probeRad: probeRadius);
                        }
                        else if (surfMethod == SurfMethod.EDTSurf)
                        {
                            EDTSurfWrapper.createEDTSurface(idFrame, repParent, name, sel, ref meshd, currentMat);
                        }
                        else if (surfMethod == SurfMethod.QUICKSES)
                        {
                            WrapperCudaSES.createCUDASESSurface(idFrame, repParent, name, sel, ref meshd);
                        }

                        else if (surfMethod == SurfMethod.QUICKSURF)
                        {
                            WrapperCudaQuickSurf.createSurface(idFrame, repParent, name, sel, ref meshd);
                        }

                        if (meshd != null && meshd.vertices != null)
                        { //Save the surface as precomputed
                            atomIdPerVert = meshd.atomByVert;
                            UnityMolMain.getPrecompRepManager().precomputedRep[keyPrecomputedRep] = meshd;
                            UnityMolMain.getPrecompRepManager().precomputedSurfAsso[keyPrecomputedRep] = atomIdPerVert;
                        }
                    }
                }

                else
                { //Need to compute the surface because isCutSurface = false or isCutByChain = false
                    if (surfMethod == SurfMethod.MSMS)
                    {
                        MSMSWrapper.createMSMSSurface(idFrame, repParent, name, sel, ref meshd, probeRad: probeRadius);
                    }
                    else if (surfMethod == SurfMethod.EDTSurf)
                    {
                        EDTSurfWrapper.createEDTSurface(idFrame, repParent, name, sel, ref meshd, currentMat);
                    }
                    else if (surfMethod == SurfMethod.QUICKSES)
                    {
                        WrapperCudaSES.createCUDASESSurface(idFrame, repParent, name, sel, ref meshd);
                    }
                    else if (surfMethod == SurfMethod.QUICKSURF)
                    {
                        WrapperCudaQuickSurf.createSurface(idFrame, repParent, name, sel, ref meshd);
                    }
                    if (meshd != null)
                        atomIdPerVert = meshd.atomByVert;

                }
            }

            if (meshd != null)
            {

                if (isTraj)
                {
                    if (idSubSel >= allMeshData.Count)
                    {
                        allMeshData.Add(meshd);
                    }
                    if (idSubSel >= meshesGO.Count)
                    {//Need to create the gameobject
                        GameObject go = createUnityMesh(meshd, repParent, name);
                        meshesGO.Add(go);
                        chainToGo[selection.atoms[0].residue.chain] = go;
                        chainToIdSubSel[selection.atoms[0].residue.chain] = idSubSel;
                    }
                    GameObject curGo = meshesGO[idSubSel];
                    Mesh m = curGo.GetComponent<MeshFilter>().sharedMesh;

                    m.Clear();
                    m.SetVertices(meshd.vertices, 0, meshd.nVert);
                    m.SetIndices(meshd.triangles, 0, meshd.nTri * 3, MeshTopology.Triangles, 0, false, 0);
                    m.SetColors(meshd.colors, 0, meshd.nVert);

                    if (meshd.normals == null || meshd.normals[0] == Vector3.zero)
                    {
                        if (UnityMolMain.raytracingMode)
                            m.RecalculateNormals(60.0f);
                        else
                            m.RecalculateNormals();
                    }
                    else
                        m.SetNormals(meshd.normals, 0, meshd.nVert);
                }
                else
                {
                    GameObject go = createUnityMesh(meshd, repParent, name);
                    meshesGO.Add(go);
                    chainToGo[selection.atoms[0].residue.chain] = go;
                    chainToIdSubSel[selection.atoms[0].residue.chain] = idSubSel;
                }

                if (atomIdPerVert != null)
                {
                    vertToAtom.Add(atomIdPerVert);
                }


                if (isCutSurface)
                { //Remove triangles for atoms not in selection
                    if (sel.atoms.Count == selection.atoms.Count)
                    {
                        return true;
                    }


                    HashSet<int> vertIdToDel = new HashSet<int>();
                    HashSet<UnityMolAtom> selectionhs = new HashSet<UnityMolAtom>(selection.atoms);

                    for (int i = 0; i < meshd.nVert; i++)
                    {
                        int aId = atomIdPerVert[i];
                        if (aId >= sel.atoms.Count || aId < 0 || !selectionhs.Contains(sel.atoms[aId]))
                        {
                            vertIdToDel.Add(i);
                        }
                    }


                    //Update the mesh => recreate a triangle array
                    Mesh m = meshesGO[idSubSel].GetComponent<MeshFilter>().sharedMesh;
                    Vector3[] vertices = m.vertices;
                    int[] triangles = m.triangles;
                    List<int> newTri = new List<int>(triangles.Length);
                    for (int t = 0; t < triangles.Length - 3; t += 3)
                    {
                        if (vertIdToDel.Contains(triangles[t]))
                        {
                            continue;
                        }
                        if (vertIdToDel.Contains(triangles[t + 1]))
                        {
                            continue;
                        }
                        if (vertIdToDel.Contains(triangles[t + 2]))
                        {
                            continue;
                        }
                        newTri.Add(triangles[t]);
                        newTri.Add(triangles[t + 1]);
                        newTri.Add(triangles[t + 2]);
                    }

                    m.SetTriangles(newTri, 0);
                    if (newTri.Count == 0)
                    {
                        Debug.LogWarning("The surface might not show because every triangles have been hidden");
                    }

                }

                return true;
            }
            return false;
        }

        public override void recompute(bool isTraj = false)
        {

            // getMeshColorsPerAtom();

            Clear();

            vertToAtom.Clear();

            if (isCutByChain)
            {
                subSelections = cutSelection(selection);
            }
            else
            {
                subSelections = new List<UnityMolSelection>() { selection };
            }

            int idSubSel = 0;
            foreach (UnityMolSelection sel in subSelections)
            {
                int[] atomIdPerVert = null;
                bool success = displaySurfaceMesh(sel.name, sel, newRep.transform, ref atomIdPerVert, idSubSel, isTraj);

                if (success)
                {
                    if (atomIdPerVert == null)
                    {//Shouldn't happen as all surf methods generate atomIdPerVert array
                        atomIdPerVert = computeNearestVertexPerAtom(meshesGO.Last(), sel);
                    }
                }
                idSubSel++;
            }

            if (meshesGO.Count > 0)
            {
                foreach (GameObject m in meshesGO)
                {
                    m.GetComponent<MeshRenderer>().sharedMaterial = currentMat;
                    if (isTraj)
                        m.GetComponent<Renderer>().sharedMaterial.SetFloat("_AOIntensity", 0.0f);
                }
            }

            if (meshesGO.Count > 0 && useAO && !isTraj)
            {
                try
                {
                    GameObject aoGo = new GameObject("tmpAO");
                    geoAO aoScript = aoGo.AddComponent<geoAO>();
                    aoScript.ComputeAO(meshesGO);
                    GameObject.Destroy(aoGo);
                }
                catch
                {
                    Debug.LogWarning("Could not compute AO");
                }
            }
            getMeshColors();

            restoreColorsPerAtom();
        }

        GameObject createUnityMesh(MeshData mData, Transform meshPar, string name)
        {

            GameObject newMeshGo = new GameObject(name);
            newMeshGo.transform.parent = meshPar;
            newMeshGo.transform.localPosition = Vector3.zero;
            newMeshGo.transform.localRotation = Quaternion.identity;
            newMeshGo.transform.localScale = Vector3.one;

            Mesh newMesh = new Mesh();
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            newMesh.SetVertices(mData.vertices, 0, mData.nVert);
            newMesh.SetColors(mData.colors, 0, mData.nVert);
            newMesh.SetIndices(mData.triangles, 0, mData.nTri * 3, MeshTopology.Triangles, 0, false, 0);


            if (mData.normals == null || mData.normals[0] == Vector3.zero)
            {
                if (UnityMolMain.raytracingMode)
                    newMesh.RecalculateNormals(60.0f);
                else
                    newMesh.RecalculateNormals();
            }
            else
                newMesh.SetNormals(mData.normals, 0, mData.nVert);

            MeshFilter mf = newMeshGo.AddComponent<MeshFilter>();
            mf.sharedMesh = newMesh;

            MeshRenderer mr = newMeshGo.AddComponent<MeshRenderer>();

            if (normalMat == null)
            {
                normalMat = new Material(Shader.Find("Custom/SurfaceVertexColor"));
                normalMat.enableInstancing = true;
            }
            normalMat.SetFloat("_Glossiness", 0.0f);
            normalMat.SetFloat("_Metallic", 0.0f);
            if (useAO)
            {
                normalMat.SetFloat("_AOIntensity", 8.0f);
            }
            else
            {
                normalMat.SetFloat("_AOIntensity", 0.0f);
            }
            mr.sharedMaterial = normalMat;
            currentMat = normalMat;

            return newMeshGo;
        }

        bool checkHeteroOrLigand(UnityMolSelection sel)
        {

            MDAnalysisSelection selec = new MDAnalysisSelection("ligand or ions", sel.atoms);
            UnityMolSelection ret = selec.process();
            HashSet<UnityMolAtom> ligAtoms = new HashSet<UnityMolAtom>(ret.atoms);

            foreach (UnityMolAtom a in sel.atoms)
            {
                if (!a.isHET && !ligAtoms.Contains(a))
                {
                    return false;
                }
            }
            return true;
        }

        public override void Clean()
        {
            Clear();

            colorByAtom = null;

            meshColors.Clear();

            if (normalMat != null)
                GameObject.Destroy(normalMat);
            normalMat = null;

            if (transMat != null)
                GameObject.Destroy(transMat);
            transMat = null;

            if (transMatShadow != null)
                GameObject.Destroy(transMatShadow);
            transMatShadow = null;

            if (wireMat != null)
                GameObject.Destroy(wireMat);
            wireMat = null;
        }
    }
}
