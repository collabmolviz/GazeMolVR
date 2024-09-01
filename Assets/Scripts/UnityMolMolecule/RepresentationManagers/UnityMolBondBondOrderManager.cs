using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;
using Unity.Mathematics;

namespace UMol {
public class UnityMolBondBondOrderManager : UnityMolGenericRepresentationManager {
    private UnityMolRepresentation rep;
    private BondRepresentationBondOrder bondRep;

    private bool[] texturesToUpdate;
    private bool[] multiBondTexturesToUpdate;
    public float shininess = 0.0f;
    public float shrink = 0.4f;
    public float scaleBond = 1.0f;
    public bool largeBB = false;

    /// <summary>
    /// Initializes this instance of the manager.
    /// </summary>
    public override void Init(SubRepresentation umolRep) {

        if (isInit) {
            return;
        }

        bondRep = (BondRepresentationBondOrder) umolRep.bondRep;
        if (bondRep.meshesGO.Count != 0) {
            texturesToUpdate = new bool[bondRep.paramTextures.Length];
            if (bondRep.multiBondParamTextures != null) {
                multiBondTexturesToUpdate = new bool[bondRep.multiBondParamTextures.Length];
            }
            else {
                multiBondTexturesToUpdate = new bool[0];
            }
        }
        isInit = true;
        isEnabled = true;
        areSideChainsOn = true;
        areHydrogensOn = true;
        isBackboneOn = true;

        shrink = 0.4f;
    }

    public override void InitRT() {
    }

    public override void Clean() {

        if (bondRep.meshesGO != null) {
            for (int i = 0; i < bondRep.meshesGO.Count; i++) {
                GameObject.Destroy(bondRep.meshesGO[i].GetComponent<MeshFilter>().sharedMesh);
                GameObject.Destroy(bondRep.meshesGO[i]);
            }
            bondRep.meshesGO.Clear();
        }
        if (bondRep.multiBondGO != null) {
            for (int i = 0; i < bondRep.multiBondGO.Count; i++) {
                GameObject.Destroy(bondRep.multiBondGO[i].GetComponent<MeshFilter>().sharedMesh);
                GameObject.Destroy(bondRep.multiBondGO[i]);
            }
            bondRep.multiBondGO.Clear();
        }


        if (bondRep.representationTransform != null) {
            GameObject.Destroy(bondRep.representationTransform.gameObject);
        }

        if (bondRep.coordStickTexture != null) {
            bondRep.coordStickTexture.Clear();
        }
        if (bondRep.multiBondCoordStickTexture != null) {
            bondRep.multiBondCoordStickTexture.Clear();
        }
        if (bondRep.atomToDuo != null) {
            bondRep.atomToDuo.Clear();
        }
        texturesToUpdate = null;
        multiBondTexturesToUpdate = null;
        if (bondRep.paramTextures != null) {
            for (int i = 0; i < bondRep.paramTextures.Length; i++) {
                GameObject.Destroy(bondRep.paramTextures[i]);
            }
        }
        if (bondRep.multiBondParamTextures != null) {
            for (int i = 0; i < bondRep.multiBondParamTextures.Length; i++) {
                GameObject.Destroy(bondRep.multiBondParamTextures[i]);
            }
        }

        bondRep.paramTextures = null;
        bondRep.coordStickTexture = null;
        bondRep.multiBondCoordStickTexture = null;
        bondRep.multiBondParamTextures = null;
        bondRep.atomToDuo = null;
        bondRep.meshesGO = null;
        bondRep.multiBondGO = null;
        bondRep = null;

        isInit = false;
        isEnabled = false;
    }

    public void ApplyTextures() {
        if (bondRep.paramTextures != null) {
            for (int i = 0; i < bondRep.paramTextures.Length; i++) {
                if (texturesToUpdate[i]) {
                    bondRep.paramTextures[i].Apply(false, false);
                }
                texturesToUpdate[i] = false;
            }
        }
        if (bondRep.multiBondParamTextures != null) {
            for (int i = 0; i < bondRep.multiBondParamTextures.Length; i++) {
                if (multiBondTexturesToUpdate[i]) {
                    bondRep.multiBondParamTextures[i].Apply(false, false);
                }
                multiBondTexturesToUpdate[i] = false;
            }
        }
    }

    /// <summary>
    /// Disables the renderers for all objects managed by the instance of the manager.
    /// </summary>
    public override void DisableRenderers() {

        for (int i = 0; i < bondRep.meshesGO.Count; i++) {
            bondRep.meshesGO[i].GetComponent<Renderer>().enabled = false;
        }
        for (int i = 0; i < bondRep.multiBondGO.Count; i++) {
            bondRep.multiBondGO[i].GetComponent<Renderer>().enabled = false;
        }
        isEnabled = false;
        // UnityMolMain.getRepresentationManager().UpdateActiveColliders();
    }

    /// <summary>
    /// Enables the renderers for all objects managed by the instance of the manager.
    /// </summary>
    public override void EnableRenderers() {

        for (int i = 0; i < bondRep.meshesGO.Count; i++) {
            bondRep.meshesGO[i].GetComponent<Renderer>().enabled = true;
        }
        for (int i = 0; i < bondRep.multiBondGO.Count; i++) {
            bondRep.multiBondGO[i].GetComponent<Renderer>().enabled = true;
        }
        isEnabled = true;
        // UnityMolMain.getRepresentationManager().UpdateActiveColliders();
    }

    /// <summary>
    /// Resets the positions of all atoms
    /// </summary>
    public void ResetPositions() {
        Vector4 offsetPos = new Vector4(bondRep.offsetPos.x, bondRep.offsetPos.y, bondRep.offsetPos.z, 0.0f);

        Vector4 atom1Pos = Vector4.zero;
        Vector4 atom2Pos = Vector4.zero;
        int3 infoTex;

        UnityMolModel curM = null;

        foreach (AtomDuo d in bondRep.coordStickTexture.Keys) {
            UnityMolStructure s1 = d.a1.residue.chain.model.structure;
            UnityMolStructure s2 = d.a2.residue.chain.model.structure;

            UnityMolModel m1 = curM;
            UnityMolModel m2 = curM;

            if (curM == null || curM.structure != s1 || curM.structure != s2) {
                m1 = s1.currentModel;
                m2 = s2.currentModel;
                curM = m1;
            }

            UnityMolAtom atom1 = m1.allAtoms[d.a1.idInAllAtoms];
            UnityMolAtom atom2 = m2.allAtoms[d.a2.idInAllAtoms];

            atom1Pos.x = atom1.position.x;
            atom1Pos.y = atom1.position.y;
            atom1Pos.z = atom1.position.z;

            atom1Pos += offsetPos;

            atom2Pos.x = atom2.position.x;
            atom2Pos.y = atom2.position.y;
            atom2Pos.z = atom2.position.z;

            atom2Pos += offsetPos;

            infoTex = bondRep.coordStickTexture[d];

            bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 4, atom1Pos);
            bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 5, atom2Pos);
            texturesToUpdate[infoTex.x] = true;

            //Update multibond positions
            if (bondRep.multiBondCoordStickTexture != null && bondRep.multiBondCoordStickTexture.ContainsKey(d)) {

                if (bondRep.doubleBonded.ContainsKey(d)) {
                    Vector3 t = bondRep.computeOffset(bondRep.doubleBonded[d]);
                    Vector4 offset = new Vector4(t.x, t.y, t.z, 0.0f);

                    int3 infoTex2 = bondRep.multiBondCoordStickTexture[d];
                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 4, atom1Pos - offset);
                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 5, atom2Pos - offset);

                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y + 1, 4, atom1Pos + offset);
                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y + 1, 5, atom2Pos + offset);
                    multiBondTexturesToUpdate[infoTex2.x] = true;
                }
                else if (bondRep.tripleBonded.ContainsKey(d)) {
                    Vector3 t = bondRep.computeOffset(bondRep.tripleBonded[d]) * 1.2f;
                    Vector4 offset = new Vector4(t.x, t.y, t.z, 0.0f);

                    int3 infoTex2 = bondRep.multiBondCoordStickTexture[d];
                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 4, atom1Pos - offset);
                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 5, atom2Pos - offset);

                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y + 1, 4, atom1Pos + offset);
                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y + 1, 5, atom2Pos + offset);
                    multiBondTexturesToUpdate[infoTex2.x] = true;
                }
            }
        }

        ApplyTextures();

    }

    /// Set a large bounding box to avoid culling
    public void SetLargeBoundingVolume() {
        if (!largeBB) {
            if (bondRep.meshesGO != null && bondRep.meshesGO.Count != 0) {
                for (int i = 0; i < bondRep.meshesGO.Count; i++) {
                    Bounds b = bondRep.meshesGO[i].GetComponent<MeshFilter>().sharedMesh.bounds;
                    b.size = Vector3.one * 5000.0f;
                    bondRep.meshesGO[i].GetComponent<MeshFilter>().sharedMesh.bounds = b;
                }
            }
            if (bondRep.multiBondGO != null && bondRep.multiBondGO.Count != 0) {
                for (int i = 0; i < bondRep.multiBondGO.Count; i++) {
                    Bounds b = bondRep.multiBondGO[i].GetComponent<MeshFilter>().sharedMesh.bounds;
                    b.size = Vector3.one * 5000.0f;
                    bondRep.multiBondGO[i].GetComponent<MeshFilter>().sharedMesh.bounds = b;
                }
            }
        }
        largeBB = true;
    }


    public void SetTexture(Texture tex) {
        for (int i = 0; i < bondRep.meshesGO.Count; i++) {
            bondRep.meshesGO[i].GetComponent<Renderer>().sharedMaterial.SetTexture("_MatCap", tex);
        }
        for (int i = 0; i < bondRep.multiBondGO.Count; i++) {
            bondRep.multiBondGO[i].GetComponent<Renderer>().sharedMaterial.SetTexture("_MatCap", tex);
        }
    }
    public void ResetTexture() {
        Texture tex = (Texture) Resources.Load("Images/MatCap/daphz05");
        SetTexture(tex);
    }

    public void ShowAtoms(HashSet<UnityMolAtom> atoms, bool show) {
        foreach (AtomDuo duo in bondRep.coordStickTexture.Keys) {
            UnityMolAtom atom1 = duo.a1;
            UnityMolAtom atom2 = duo.a2;
            int3 infoTex = bondRep.coordStickTexture[duo];

            if (atoms.Contains(atom1) || atoms.Contains(atom2)) {
                if (show)
                    bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.one);
                else
                    bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.zero);

                texturesToUpdate[infoTex.x] = true;

                if (bondRep.multiBondCoordStickTexture.ContainsKey(duo)) {
                    int3 infoTex2 = bondRep.multiBondCoordStickTexture[duo];
                    if (show)
                        bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.one);
                    else
                        bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.zero);
                    multiBondTexturesToUpdate[infoTex2.x] = true;
                }

            }
        }
        ApplyTextures();
    }

    public override void ShowHydrogens(bool show) {
        HashSet<UnityMolAtom> toHide = new HashSet<UnityMolAtom>();

        foreach (AtomDuo duo in bondRep.coordStickTexture.Keys) {
            UnityMolAtom atom1 = duo.a1;
            UnityMolAtom atom2 = duo.a2;

            if (atom1.type == "H") {
                if (show && !areSideChainsOn &&
                        MDAnalysisSelection.isSideChain(atom1, bondRep.selection.bonds)) {}
                else {
                    toHide.Add(atom1);
                }
            }
            if (atom2.type == "H") {
                if (show && !areSideChainsOn &&
                        MDAnalysisSelection.isSideChain(atom2, bondRep.selection.bonds)) {}
                else {
                    toHide.Add(atom2);
                }
            }

        }
        ShowAtoms(toHide, show);
        areHydrogensOn = show;

    }

    //TODO refactor this working mess
    public void ShowHide(AtomDuo d, bool show, bool now = true) {
        bool found = false;
        AtomDuo invd = new AtomDuo(d.a2, d.a1);
        if (bondRep.atomToDuo.ContainsKey(d.a1)) {
            foreach (AtomDuo d2 in bondRep.atomToDuo[d.a1]) {
                int3 infoTex;
                if (d.Equals(d2) || invd.Equals(d2)) {
                    if (invd.Equals(d2)) {
                        infoTex = bondRep.coordStickTexture[invd];
                    }
                    else {
                        infoTex = bondRep.coordStickTexture[d];
                    }

                    if (show) {
                        bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.one);
                    }
                    else {
                        bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.zero);
                    }

                    if (bondRep.multiBondCoordStickTexture.ContainsKey(d)) {
                        int3 infoTex2 = bondRep.multiBondCoordStickTexture[d];
                        if (show)
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.one);
                        else
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.zero);
                        multiBondTexturesToUpdate[infoTex2.x] = true;
                    }
                    else if (bondRep.multiBondCoordStickTexture.ContainsKey(invd)) {
                        int3 infoTex2 = bondRep.multiBondCoordStickTexture[invd];
                        if (show)
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.one);
                        else
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.zero);
                        multiBondTexturesToUpdate[infoTex2.x] = true;
                    }

                    texturesToUpdate[infoTex.x] = true;
                    found = true;
                    break;
                }

            }
        }

        if (!found && bondRep.atomToDuo.ContainsKey(d.a2)) {
            foreach (AtomDuo d2 in bondRep.atomToDuo[d.a2]) {
                if (d2 == d || invd == d2) {
                    int3 infoTex;
                    if (invd.Equals(d2)) {
                        infoTex = bondRep.coordStickTexture[invd];
                    }
                    else {
                        infoTex = bondRep.coordStickTexture[d2];
                    }
                    if (show) {
                        bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.one);
                    }
                    else {
                        bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.zero);
                    }
                    if (bondRep.multiBondCoordStickTexture.ContainsKey(d2)) {
                        int3 infoTex2 = bondRep.multiBondCoordStickTexture[d2];
                        if (show)
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.one);
                        else
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.zero);
                        multiBondTexturesToUpdate[infoTex2.x] = true;
                    }
                    else if (bondRep.multiBondCoordStickTexture.ContainsKey(invd)) {
                        int3 infoTex2 = bondRep.multiBondCoordStickTexture[invd];
                        if (show)
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.one);
                        else
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.zero);
                        multiBondTexturesToUpdate[infoTex2.x] = true;
                    }

                    texturesToUpdate[infoTex.x] = true;
                    found = true;
                    break;
                }
            }
        }
        if (!found) {
            // Debug.Log("Not found !");
        }
        if (found && now) {
            ApplyTextures();
        }
    }

    public override void ShowSideChains(bool show) {


        foreach (AtomDuo duo in bondRep.coordStickTexture.Keys) {
            UnityMolAtom atom1 = duo.a1;
            UnityMolAtom atom2 = duo.a2;
            int3 infoTex = bondRep.coordStickTexture[duo];
            if (MDAnalysisSelection.isSideChain(atom1, bondRep.selection.bonds) || MDAnalysisSelection.isSideChain(atom2, bondRep.selection.bonds)) {
                if (show && !areHydrogensOn && (atom1.type == "H" || atom2.type == "H")) {
                }
                else {
                    if (show) {
                        bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.one);

                    }
                    else {
                        bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.zero);
                    }

                    if (bondRep.multiBondCoordStickTexture.ContainsKey(duo)) {
                        int3 infoTex2 = bondRep.multiBondCoordStickTexture[duo];
                        if (show)
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.one);
                        else
                            bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.zero);
                        multiBondTexturesToUpdate[infoTex2.x] = true;
                    }

                    texturesToUpdate[infoTex.x] = true;
                }
            }
        }
        areSideChainsOn = show;
        ApplyTextures();
    }

    public override void ShowBackbone(bool show) {
        HashSet<UnityMolAtom> toHide = new HashSet<UnityMolAtom>();

        foreach (AtomDuo duo in bondRep.coordStickTexture.Keys) {
            UnityMolAtom atom1 = duo.a1;
            UnityMolAtom atom2 = duo.a2;

            if (MDAnalysisSelection.isBackBone(atom1, bondRep.selection.bonds)) {
                toHide.Add(atom1);
            }
            if (MDAnalysisSelection.isBackBone(atom2, bondRep.selection.bonds)) {
                toHide.Add(atom2);
            }
        }
        ShowAtoms(toHide, show);
        isBackboneOn = show;

    }

    public override void SetColor(Color32 col, UnityMolSelection sele) {

        foreach (UnityMolAtom a in sele.atoms) {
            SetColor(col, a, false);
        }
        ApplyTextures();
    }

    public override void SetColor(Color32 col, UnityMolAtom a) {
        SetColor(col, a, true);
    }

    public void SetColor(Color32 col, UnityMolAtom a, bool now = true) {
        if (bondRep.meshesGO.Count == 0 || !bondRep.atomToDuo.ContainsKey(a)) {
            return;
        }

        // For all atoms linked
        foreach (AtomDuo ad in bondRep.atomToDuo[a]) {
            if (ad.a1 == a) { //Use a2
                SetColorForAtom(col, a, ad.a2);
            }
            else { //use a1
                SetColorForAtom(col, a, ad.a1);
            }
        }
        if (now) {
            ApplyTextures();
        }
    }
    public override void SetColors(Color32 col, List<UnityMolAtom> atoms) {
        foreach (UnityMolAtom a in atoms) {
            SetColor(col, a, false);
        }
        ApplyTextures();
    }

    public override void SetColors(List<Color32> cols, List<UnityMolAtom> atoms) {
        if (atoms.Count != cols.Count) {
            Debug.LogError("Lengths of color list and atom list are different");
            return;
        }
        for (int i = 0; i < atoms.Count; i++) {
            UnityMolAtom a = atoms[i];
            Color32 col = cols[i];
            SetColor(col, a, false);
        }
        ApplyTextures();
    }

    public void SetColorForAtom(Color32 col, UnityMolAtom a1, UnityMolAtom a2) {
        AtomDuo key = new AtomDuo(a1, a2);
        bondRep.colorPerAtom[a1] = col;
        int3 infoTex;
        if (bondRep.coordStickTexture.TryGetValue(key, out infoTex)) {
            bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 2, col);
            texturesToUpdate[infoTex.x] = true;

            if (bondRep.multiBondCoordStickTexture != null && bondRep.multiBondCoordStickTexture.ContainsKey(key)) {
                int3 infoTex2 = bondRep.multiBondCoordStickTexture[key];
                bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 2, col);
                bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y + 1, 2, col);
                multiBondTexturesToUpdate[infoTex2.x] = true;
            }
        }
        else {
            key = new AtomDuo(a2, a1);
            if (bondRep.coordStickTexture.TryGetValue(key, out infoTex) ) {
                bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 3, col);
                texturesToUpdate[infoTex.x] = true;
            }
            if (bondRep.multiBondCoordStickTexture != null && bondRep.multiBondCoordStickTexture.ContainsKey(key)) {
                int3 infoTex2 = bondRep.multiBondCoordStickTexture[key];
                bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 3, col);
                bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y + 1, 3, col);
                multiBondTexturesToUpdate[infoTex2.x] = true;
            }
        }

    }

    public override void SetDepthCueingStart(float v) {
        if (bondRep.meshesGO == null)
            return;

        foreach (GameObject meshGO in bondRep.meshesGO) {
            Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
            mats[0].SetFloat("_FogStart", v);
        }

        if (bondRep.multiBondGO == null)
            return;

        foreach (GameObject meshGO in bondRep.multiBondGO) {
            Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
            mats[0].SetFloat("_FogStart", v);
        }

    }

    public override void SetDepthCueingDensity(float v) {
        if (bondRep.meshesGO == null)
            return;
        foreach (GameObject meshGO in bondRep.meshesGO) {

            Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
            mats[0].SetFloat("_FogDensity", v);
        }

        if (bondRep.multiBondGO == null)
            return;

        foreach (GameObject meshGO in bondRep.multiBondGO) {
            Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
            mats[0].SetFloat("_FogDensity", v);
        }

    }

    public override void EnableDepthCueing() {
        if (bondRep.meshesGO == null)
            return;
        foreach (GameObject meshGO in bondRep.meshesGO) {

            Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
            mats[0].SetFloat("_UseFog", 1.0f);
        }

        if (bondRep.multiBondGO == null)
            return;

        foreach (GameObject meshGO in bondRep.multiBondGO) {
            Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
            mats[0].SetFloat("_UseFog", 1.0f);
        }

    }

    public override void DisableDepthCueing() {
        if (bondRep.meshesGO == null)
            return;
        foreach (GameObject meshGO in bondRep.meshesGO) {

            Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
            mats[0].SetFloat("_UseFog", 0.0f);
        }
        if (bondRep.multiBondGO == null)
            return;

        foreach (GameObject meshGO in bondRep.multiBondGO) {
            Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
            mats[0].SetFloat("_UseFog", 0.0f);
        }
    }

    public override void ShowShadows(bool show) {
        for (int i = 0; i < bondRep.meshesGO.Count; i++) {
            if (show) {
                bondRep.meshesGO[i].GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
                bondRep.meshesGO[i].GetComponent<Renderer>().receiveShadows = true;
            }
            else {
                bondRep.meshesGO[i].GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                bondRep.meshesGO[i].GetComponent<Renderer>().receiveShadows = false;
            }
        }
        for (int i = 0; i < bondRep.multiBondGO.Count; i++) {
            if (show) {
                bondRep.multiBondGO[i].GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
                bondRep.multiBondGO[i].GetComponent<Renderer>().receiveShadows = true;
            }
            else {
                bondRep.multiBondGO[i].GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                bondRep.multiBondGO[i].GetComponent<Renderer>().receiveShadows = false;
            }
        }
    }

    public void SetShininess(float val) {
        //Clamp and invert shininess
        shininess = val;
        float valShine = (shininess < 0.0001f ? 0.0f : 1.0f / shininess);

        for (int i = 0; i < bondRep.meshesGO.Count; i++) {
            bondRep.meshesGO[i].GetComponent<Renderer>().sharedMaterial.SetFloat("_Shininess", valShine);
        }
        for (int i = 0; i < bondRep.multiBondGO.Count; i++) {
            bondRep.multiBondGO[i].GetComponent<Renderer>().sharedMaterial.SetFloat("_Shininess", valShine);
        }
    }

    public void ResetShininess() {
        SetShininess(0.0f);
    }

    public void SetShrink(float newShrink) {
        if (newShrink > 0.0f && newShrink <= 1.0f) {
            for (int i = 0; i < bondRep.meshesGO.Count; i++) {
                bondRep.meshesGO[i].GetComponent<Renderer>().sharedMaterial.SetFloat("_Shrink", newShrink);
            }
            // for (int i = 0; i < bondRep.multiBondGO.Count; i++) {
            //  bondRep.multiBondGO[i].GetComponent<Renderer>().sharedMaterial.SetFloat("_Shrink", newShrink);
            // }

            shrink = newShrink;
        }
    }

    public void ShowBondForAtom(bool show, UnityMolAtom atom) {

        if (bondRep.meshesGO.Count == 0  || !bondRep.atomToDuo.ContainsKey(atom)) {
            return;
        }

        // For all atoms linked
        foreach (AtomDuo ad in bondRep.atomToDuo[atom]) {
            if (ad.a1 == atom) { //Use a2
                showBond(atom, ad.a2, show);
            }
            else { //use a1
                showBond(atom, ad.a1, show);
            }
        }
    }

    public void SetScaleForAtom(float size, UnityMolAtom atom, bool now = true) {
        if (bondRep.meshesGO.Count == 0 || !bondRep.atomToDuo.ContainsKey(atom)) {
            return;
        }

        // For all atoms linked
        foreach (AtomDuo ad in bondRep.atomToDuo[atom]) {
            if (ad.a1 == atom) { //Use a2
                setScaleBond(atom, ad.a2, size);
            }
            else { //use a1
                setScaleBond(atom, ad.a1, size);
            }
        }

        if (now) {
            ApplyTextures();
        }
    }

    public void showBond(UnityMolAtom atom1, UnityMolAtom atom2, bool show) {

        AtomDuo key = new AtomDuo(atom1, atom2);
        int3 infoTex;
        if (bondRep.coordStickTexture.TryGetValue(key, out infoTex)) {
            if (show) {
                bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.one);
            }
            else {
                bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.zero);
            }
            texturesToUpdate[infoTex.x] = true;

            if (bondRep.multiBondCoordStickTexture != null && bondRep.multiBondCoordStickTexture.ContainsKey(key)) {
                int3 infoTex2 = bondRep.multiBondCoordStickTexture[key];
                if (show)
                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.one);
                else
                    bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.zero);
                multiBondTexturesToUpdate[infoTex2.x] = true;
            }
        }
    }

    public void setScaleBond(UnityMolAtom a1, UnityMolAtom a2, float size) {

        AtomDuo key = new AtomDuo(a1, a2);
        int3 infoTex;
        if (bondRep.coordStickTexture.TryGetValue(key, out infoTex)) {
            Vector4 newSize = bondRep.paramTextures[infoTex.x].GetPixel(infoTex.y, 11);
            newSize.x = size;
            bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 11, newSize);
            texturesToUpdate[infoTex.x] = true;
        }
        else {
            key = new AtomDuo(a2, a1);
            if (bondRep.coordStickTexture.TryGetValue(key, out infoTex) ) {
                Vector4 newSize = bondRep.paramTextures[infoTex.x].GetPixel(infoTex.y, 11);
                newSize.y = size;
                bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 11, newSize);
                texturesToUpdate[infoTex.x] = true;
            }
        }
    }

    public void ResetVisibility() {
        foreach (AtomDuo duo in bondRep.coordStickTexture.Keys) {
            UnityMolAtom atom1 = duo.a1;
            UnityMolAtom atom2 = duo.a2;
            int3 infoTex = bondRep.coordStickTexture[duo];
            bondRep.paramTextures[infoTex.x].SetPixel(infoTex.y, 10, Vector4.one);
            texturesToUpdate[infoTex.x] = true;

            if (bondRep.multiBondCoordStickTexture != null && bondRep.multiBondCoordStickTexture.ContainsKey(duo)) {
                int3 infoTex2 = bondRep.multiBondCoordStickTexture[duo];
                bondRep.multiBondParamTextures[infoTex2.x].SetPixel(infoTex2.y, 10, Vector4.one);
                multiBondTexturesToUpdate[infoTex2.x] = true;
            }

        }
        ApplyTextures();
    }

    public override void updateWithTrajectory() {
        ResetPositions();
        SetLargeBoundingVolume();
    }

    public override void updateWithModel() {
        ResetPositions();
    }

    public override void ShowAtom(UnityMolAtom atom, bool show) {
        ShowBondForAtom(show, atom);
    }

    public override void SetSize(UnityMolAtom atom, float size) {
        SetScaleForAtom(size, atom);
    }

    public override void SetSizes(List<UnityMolAtom> atoms, List<float> sizes) {
        int i = 0;
        foreach (UnityMolAtom a in atoms) {
            SetScaleForAtom(sizes[i], a, false);
            i++;
        }
        ApplyTextures();
    }

    public override void SetSizes(List<UnityMolAtom> atoms, float size) {
        scaleBond = size;

        foreach (UnityMolAtom a in atoms) {
            SetScaleForAtom(size, a, false);
        }

        ApplyTextures();
    }

    public override void ResetSize(UnityMolAtom atom) {
        SetSize(atom, 1.0f);
    }

    public override void ResetSizes() {
        UnityMolModel m = bondRep.selection.atoms[0].residue.chain.model;

        foreach (int ida in bondRep.selection.bonds.bonds.Keys) {
            UnityMolAtom a = m.allAtoms[ida];
            SetScaleForAtom(1.0f, a, false);
        }
        ApplyTextures();
        scaleBond = 1.0f;
    }

    public override void ResetColor(UnityMolAtom atom) {
        SetColor(atom.color32, atom);
    }

    public override void ResetColors() {
        UnityMolModel m = bondRep.selection.atoms[0].residue.chain.model;

        foreach (int ida in bondRep.selection.bonds.bonds.Keys) {
            UnityMolAtom a = m.allAtoms[ida];
            SetColor(a.color32, a, false);
        }
        ApplyTextures();
        bondRep.colorationType = colorType.atom;
    }

    public override void HighlightRepresentation() {
    }


    public override void DeHighlightRepresentation() {
    }

    public override void SetSmoothness(float val) {
        Debug.LogWarning("Cannot change this value for the hyperstick representation");
    }
    public override void SetMetal(float val) {
        Debug.LogWarning("Cannot change this value for the hyperstick representation");
    }
    public override void UpdateLike() {
    }

    public override UnityMolRepresentationParameters Save() {

        UnityMolRepresentationParameters res = new UnityMolRepresentationParameters();

        res.repT.bondType = BondType.bondorder;
        res.colorationType = bondRep.colorationType;

        if (bondRep.meshesGO == null || bondRep.meshesGO.Count == 0)
            return res;

        if (res.colorationType == colorType.custom) {
            res.colorPerAtom = new Dictionary<UnityMolAtom, Color32>(bondRep.selection.atoms.Count);
            foreach (UnityMolAtom a in bondRep.selection.atoms) {
                res.colorPerAtom[a] = bondRep.colorPerAtom[a];
            }

        }
        else if (res.colorationType == colorType.full) { //Get color of first atom/residue
            foreach (UnityMolAtom a in bondRep.colorPerAtom.Keys) {
                res.fullColor = bondRep.colorPerAtom[a];
                break;
            }
        }
        else if (res.colorationType == colorType.bfactor) {
            res.bfactorStartColor = bondRep.bfactorStartCol;
            res.bfactorMidColor = bondRep.bfactorMidColor;
            res.bfactorEndColor = bondRep.bfactorEndCol;
        }
        res.smoothness = shininess;
        // res.HSShrink = shrink;

        res.shadow = (bondRep.meshesGO[0].GetComponent<Renderer>().shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On);
        // res.HSScale = scaleBond;


        return res;
    }

    public override void Restore(UnityMolRepresentationParameters savedParams) {
        if (savedParams.repT.bondType == BondType.bondorder) {
            if (savedParams.colorationType == colorType.full) {
                SetColor(savedParams.fullColor, bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.custom) {
                foreach (UnityMolAtom a in bondRep.selection.atoms) {
                    if (savedParams.colorPerAtom.ContainsKey(a)) {
                        SetColor(savedParams.colorPerAtom[a], a);
                    }
                }
                ApplyTextures();
            }
            else if (savedParams.colorationType == colorType.res) {
                colorByRes(bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.chain) {
                colorByChain(bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.hydro) {
                colorByHydro(bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.seq) {
                colorBySequence(bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.charge) {
                colorByCharge(bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.restype) {
                colorByResType(bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.rescharge) {
                colorByResCharge(bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.resnum) {
                colorByResnum(bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.resid) {
                colorByResid(bondRep.selection);
            }
            else if (savedParams.colorationType == colorType.bfactor) {
                colorByBfactor(bondRep.selection, savedParams.bfactorStartColor, savedParams.bfactorMidColor, savedParams.bfactorEndColor);
            }

            // SetSizes(bondRep.selection.atoms, savedParams.HSScale);
            // SetShrink(savedParams.HSShrink);
            SetShininess(savedParams.smoothness);
            ShowShadows(savedParams.shadow);
            bondRep.colorationType = savedParams.colorationType;
        }
        else {
            Debug.LogError("Could not restore representation parameters");
        }
    }

}
}
