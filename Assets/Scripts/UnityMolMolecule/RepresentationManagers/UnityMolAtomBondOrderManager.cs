using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;

namespace UMol {

public class UnityMolAtomBondOrderManager : UnityMolGenericRepresentationManager {

	public AtomRepresentationBondOrder atomRep;
	private int nbAtoms;
	private bool[] texturesToUpdate;
	private List<Color32> colors;

	public GameObject AtomMeshParent;
	public float shininess = 0.0f;
	public float lastScale = 1.0f;
	private bool largeBB = false;

	private KeyValuePair<int, int> keyValP = new KeyValuePair<int, int>();

	/// <summary>
	/// Initializes this instance of the manager.
	/// </summary>
	public override void Init(SubRepresentation umolRep) {

		if (isInit) {
			return;
		}

		atomRep = (AtomRepresentationBondOrder) umolRep.atomRep;
		nbAtoms = atomRep.selection.atoms.Count;

		texturesToUpdate = new bool[atomRep.paramTextures.Length];
		for (int i = 0; i < atomRep.paramTextures.Length; i++)
			texturesToUpdate[i] = false;

		colors = atomRep.atomColors;

		isInit = true;
		isEnabled = true;
		areSideChainsOn = true;
		areHydrogensOn = true;
		isBackboneOn = true;
	}

	public override void InitRT() {
	}

	public override void Clean() {

		if (atomRep.meshesGO != null) {
			for (int i = 0; i < atomRep.meshesGO.Count; i++) {
				GameObject.Destroy(atomRep.meshesGO[i].GetComponent<MeshFilter>().sharedMesh);
				GameObject.Destroy(atomRep.meshesGO[i]);
			}
		}

		if (atomRep.representationTransform != null) {
			GameObject.DestroyImmediate(atomRep.representationTransform.gameObject);
		}

		for (int i = 0; i < atomRep.meshesGO.Count; i++) {
			GameObject.DestroyImmediate(atomRep.meshesGO[i]);
		}

		colors.Clear();
		atomRep.meshesGO.Clear();
		atomRep.coordAtomTexture.Clear();
		atomRep.atomToId.Clear();
		texturesToUpdate = null;
		for (int i = 0; i < atomRep.paramTextures.Length; i++) {
			GameObject.Destroy(atomRep.paramTextures[i]);
		}
		GameObject.Destroy(AtomMeshParent);

		nbAtoms = 0;
		atomRep.atomToId = null;
		atomRep.paramTextures = null;
		atomRep.meshesGO = null;
		atomRep.coordAtomTexture = null;
		atomRep = null;
		isInit = false;
		isEnabled = false;

		// UnityMolMain.getRepresentationManager().UpdateActiveColliders();

	}

	public void ApplyTextures() {
		if (atomRep.meshesGO == null)
			return;
		for (int i = 0; i < atomRep.paramTextures.Length; i++) {
			if (texturesToUpdate[i]) {
				atomRep.paramTextures[i].Apply(false, false);
			}
			texturesToUpdate[i] = false;
		}
	}

	/// <summary>
	/// Disables the renderers for all objects managed by the instance of the manager.
	/// </summary>
	public override void DisableRenderers() {
		if (atomRep.meshesGO == null)
			return;
		for (int i = 0; i < atomRep.meshesGO.Count; i++) {
			atomRep.meshesGO[i].GetComponent<Renderer>().enabled = false;
		}
		isEnabled = false;
		// UnityMolMain.getRepresentationManager().UpdateActiveColliders();
	}

	/// <summary>
	/// Enables the renderers for all objects managed by the instance of the manager.
	/// </summary>
	public override void EnableRenderers() {
		if (atomRep.meshesGO == null)
			return;
		for (int i = 0; i < atomRep.meshesGO.Count; i++) {
			atomRep.meshesGO[i].GetComponent<Renderer>().enabled = true;
		}
		isEnabled = true;
		// UnityMolMain.getRepresentationManager().UpdateActiveColliders();
	}

	public override void ShowShadows(bool show) {
		if (atomRep.meshesGO == null)
			return;
		for (int i = 0; i < atomRep.meshesGO.Count; i++) {
			if (show) {
				atomRep.meshesGO[i].GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
				atomRep.meshesGO[i].GetComponent<Renderer>().receiveShadows = true;
			}
			else {
				atomRep.meshesGO[i].GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
				atomRep.meshesGO[i].GetComponent<Renderer>().receiveShadows = false;
			}
		}
	}

	/// <summary>
	/// Resets the positions of all atoms. Used when trajectory reading
	/// </summary>
	public void ResetPositions() {
		Vector4 offset = new Vector4(atomRep.offsetPos.x, atomRep.offsetPos.y, atomRep.offsetPos.z, 0.0f);
		for (int i = 0; i < nbAtoms; i++) {
			if (atomRep.coordAtomTexture.TryGetValue(i, out keyValP)) {
				Vector4 atomPos = atomRep.selection.atoms[i].PositionVec4 + offset;
				atomRep.paramTextures[keyValP.Key].SetPixel(keyValP.Value, 0, atomPos);

				texturesToUpdate[keyValP.Key] = true;
			}
		}
		ApplyTextures();
	}

	/// Set a large bounding box to avoid culling
	public void SetLargeBoundingVolume() {
		if (!largeBB) {
			if (atomRep.meshesGO != null && atomRep.meshesGO.Count != 0) {
				for (int i = 0; i < atomRep.meshesGO.Count; i++) {
					Bounds b = atomRep.meshesGO[i].GetComponent<MeshFilter>().sharedMesh.bounds;
					b.size = Vector3.one * 5000.0f;
					atomRep.meshesGO[i].GetComponent<MeshFilter>().sharedMesh.bounds = b;
				}
			}
		}
		largeBB = true;
	}

	public void SetTexture(Texture tex) {
		for (int i = 0; i < atomRep.meshesGO.Count; i++) {
			atomRep.meshesGO[i].GetComponent<Renderer>().sharedMaterial.SetTexture("_MatCap", tex);
		}
	}

	public override void SetColor(Color32 col, UnityMolSelection sele) {
		foreach (UnityMolAtom a in sele.atoms) {
			SetColor(col, a);
		}
		ApplyTextures();
	}

	public override void SetColor(Color32 col, UnityMolAtom a) {
		int idAtom = -1;
		if (atomRep.atomToId.TryGetValue(a, out idAtom)) {
			if (idAtom != -1) {
				SetColor(col, idAtom);
			}
		}
		ApplyTextures();
	}

	public void SetColor(Color32 col, int atomNum) {
		if (atomRep.coordAtomTexture.TryGetValue(atomNum, out keyValP)) {

			atomRep.paramTextures[keyValP.Key].SetPixel(keyValP.Value, 2, col);

			texturesToUpdate[keyValP.Key] = true;
			colors[atomNum] = col;
		}
		//Call ApplyTextures to apply the changes !
	}


	public override void SetColors(Color32 col, List<UnityMolAtom> atoms) {
		foreach (UnityMolAtom a in atoms) {
			SetColor(col, a);
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
			SetColor(col, a);
		}
		ApplyTextures();
	}
	public void SetScale(float newScale, UnityMolAtom a, bool now = true) {
		int idAtom = -1;
		if (atomRep.atomToId.TryGetValue(a, out idAtom)) {
			if (idAtom != -1) {
				SetScale(newScale, idAtom);
			}
		}
		else {
			// Debug.LogWarning("Atom " + a + " not found in the representation");
		}
		if (now) {
			ApplyTextures();
		}
	}

	public void SetScale(float newScale, int idAtom) {
		if (atomRep.coordAtomTexture.TryGetValue(idAtom, out keyValP)) {
			atomRep.paramTextures[keyValP.Key].SetPixel(keyValP.Value, 8, Vector4.one * newScale);
			texturesToUpdate[keyValP.Key] = true;
			lastScale = newScale;
		}
		//Call ApplyTextures to apply the changes !
	}
	public void ResetScale() {
		for (int i = 0; i < nbAtoms; i++) {
			ResetScale(i);
		}
		ApplyTextures();
	}
	public void ResetScale(int idAtom) {
		SetScale(1.0f, idAtom);
		//Call ApplyTextures to apply the changes !
	}

	public override void SetDepthCueingStart(float v) {
		if (atomRep.meshesGO == null)
			return;
		foreach (GameObject meshGO in atomRep.meshesGO) {

			Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
			mats[0].SetFloat("_FogStart", v);
		}
	}

	public override void SetDepthCueingDensity(float v) {
		if (atomRep.meshesGO == null)
			return;
		foreach (GameObject meshGO in atomRep.meshesGO) {

			Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
			mats[0].SetFloat("_FogDensity", v);
		}
	}

	public override void EnableDepthCueing() {
		if (atomRep.meshesGO == null)
			return;
		foreach (GameObject meshGO in atomRep.meshesGO) {

			Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
			mats[0].SetFloat("_UseFog", 1.0f);
		}
	}

	public override void DisableDepthCueing() {
		if (atomRep.meshesGO == null)
			return;
		foreach (GameObject meshGO in atomRep.meshesGO) {

			Material[] mats = meshGO.GetComponent<Renderer>().sharedMaterials;
			mats[0].SetFloat("_UseFog", 0.0f);
		}
	}


	public override void ShowHydrogens(bool show) {
		for (int i = 0; i < nbAtoms; i++) {
			if (atomRep.selection.atoms[i].type == "H") {
				if (show && !areSideChainsOn && MDAnalysisSelection.isSideChain(atomRep.selection.atoms[i], atomRep.selection.bonds)) {
				}
				else {
					ShowAtom(i, show);
				}
			}
		}
		ApplyTextures();
		areHydrogensOn = show;
	}

	public override void ShowSideChains(bool show) {
		for (int i = 0; i < nbAtoms; i++) {
			if (show && !areHydrogensOn && atomRep.selection.atoms[i].type == "H" ) {
			}
			else {
				if (MDAnalysisSelection.isSideChain(atomRep.selection.atoms[i], atomRep.selection.bonds)) {
					ShowAtom(i, show);
				}
			}
		}
		ApplyTextures();
		areSideChainsOn = show;
	}

	public override void ShowBackbone(bool show) {
		for (int i = 0; i < nbAtoms; i++) {
			if (MDAnalysisSelection.isBackBone(atomRep.selection.atoms[i], atomRep.selection.bonds)) {
				ShowAtom(i, show);
			}
		}
		ApplyTextures();
		isBackboneOn = show;
	}

	public void ShowAtoms(HashSet<UnityMolAtom> atoms, bool show) {
		foreach (UnityMolAtom a in atoms) {
			ShowAtom(a, show);
		}
		ApplyTextures();
	}

	public void ShowAtom(int idAtom, bool show) {
		if (atomRep.coordAtomTexture.TryGetValue(idAtom, out keyValP)) {
			if (show) {
				atomRep.paramTextures[keyValP.Key].SetPixel(keyValP.Value, 7, Vector4.one);
			} else {
				atomRep.paramTextures[keyValP.Key].SetPixel(keyValP.Value, 7, Vector4.zero);
			}
			texturesToUpdate[keyValP.Key] = true;
		}
		//Call ApplyTextures to apply the changes !
	}
	public override void ShowAtom(UnityMolAtom a, bool show) {
		int idInCoord = 0;
		if (atomRep.atomToId.TryGetValue(a, out idInCoord)) {
			ShowAtom(idInCoord, show);
		}
		ApplyTextures();
	}

	public void ResetVisibility() {

		for (int i = 0; i < nbAtoms; i++) {
			if (atomRep.coordAtomTexture.TryGetValue(i, out keyValP)) {
				atomRep.paramTextures[keyValP.Key].SetPixel(keyValP.Value, 7, Vector4.one);
				texturesToUpdate[keyValP.Key] = true;
			}
		}
		ApplyTextures();
	}

	public void SetShininess(float val) {
		//Clamp and invert shininess
		shininess = val;
		float valShine = (shininess < 0.0001f ? 0.0f : 1.0f / shininess);

		for (int i = 0; i < atomRep.meshesGO.Count; i++) {
			atomRep.meshesGO[i].GetComponent<Renderer>().sharedMaterial.SetFloat("_Shininess", valShine);
		}
	}

	public void ResetShininess() {
		SetShininess(0.0f);
	}

	public override void updateWithTrajectory() {
		ResetPositions();
		SetLargeBoundingVolume();
	}

	public override void updateWithModel() {
		ResetPositions();
	}

	public override void SetSize(UnityMolAtom atom, float size) {
		SetScale(size, atom);
	}

	public override void SetSizes(List<UnityMolAtom> atoms, List<float> sizes) {
		int i = 0;
		foreach (UnityMolAtom a in atoms) {
			SetScale(sizes[i], a, false);
			i++;
		}
		ApplyTextures();
	}

	public override void SetSizes(List<UnityMolAtom> atoms, float size) {
		foreach (UnityMolAtom a in atoms) {
			SetScale(size, a, false);
		}
		ApplyTextures();
	}


	public override void ResetSize(UnityMolAtom atom) {
		SetScale(1.0f, atom);
	}

	public override void ResetSizes() {
		foreach (UnityMolAtom a in atomRep.selection.atoms) {
			SetScale(1.0f, a, false);
		}
		ApplyTextures();
	}

	public override void ResetColor(UnityMolAtom atom) {
		SetColor(atom.color32, atom);
	}

	public override void ResetColors() {
		foreach (UnityMolAtom a in atomRep.selection.atoms) {
			SetColor(a.color32, a);
		}
		atomRep.colorationType = colorType.atom;
	}


	public override void HighlightRepresentation() {
	}


	public override void DeHighlightRepresentation() {
	}

	public override void SetSmoothness(float val) {
		Debug.LogWarning("Cannot change this value for the hyperball representation");
	}
	public override void SetMetal(float val) {
		Debug.LogWarning("Cannot change this value for the hyperball representation");
	}

	public override void UpdateLike() {
	}

	public override UnityMolRepresentationParameters Save() {
		UnityMolRepresentationParameters res = new UnityMolRepresentationParameters();

		res.repT.atomType = AtomType.bondorder;
		res.colorationType = atomRep.colorationType;

		if (atomRep.meshesGO == null || atomRep.meshesGO.Count == 0)
			return res;

		if (res.colorationType == colorType.custom) {
			int atomNum = 0;
			res.colorPerAtom = new Dictionary<UnityMolAtom, Color32>(atomRep.selection.atoms.Count);
			foreach (UnityMolAtom a in atomRep.selection.atoms) {
				if (atomRep.coordAtomTexture.TryGetValue(atomNum, out keyValP)) {
					res.colorPerAtom[a] = colors[atomNum];
				}
			}
		}
		else if (res.colorationType == colorType.full) { //Get color of first atom/residue
			int atomNum = 0;
			foreach (UnityMolAtom a in atomRep.selection.atoms) {
				if (atomRep.coordAtomTexture.TryGetValue(atomNum, out keyValP)) {
					res.fullColor = colors[atomNum];
					break;
				}
			}
		}
		else if (res.colorationType == colorType.bfactor) {
			res.bfactorStartColor = atomRep.bfactorStartCol;
			res.bfactorMidColor = atomRep.bfactorMidColor;
			res.bfactorEndColor = atomRep.bfactorEndCol;
		}
		res.smoothness = shininess;
		res.shadow = (atomRep.meshesGO[0].GetComponent<Renderer>().shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On);

		return res;
	}
	public override void Restore(UnityMolRepresentationParameters savedParams) {

		if (savedParams.repT.atomType == AtomType.bondorder) {
			if (savedParams.colorationType == colorType.full) {
				SetColor(savedParams.fullColor, atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.custom) {
				List<Color32> colors = new List<Color32>(atomRep.selection.atoms.Count);
				List<UnityMolAtom> restoredAtoms = new List<UnityMolAtom>(atomRep.selection.atoms.Count);
				foreach (UnityMolAtom a in atomRep.selection.atoms) {
					if (savedParams.colorPerAtom.ContainsKey(a)) {
						colors.Add(savedParams.colorPerAtom[a]);
						restoredAtoms.Add(a);
					}
				}
				SetColors(colors, restoredAtoms);
			}
			else if (savedParams.colorationType == colorType.defaultCartoon) {
				//Do nothing !
			}
			else if (savedParams.colorationType == colorType.res) {
				colorByRes(atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.chain) {
				colorByChain(atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.hydro) {
				colorByHydro(atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.seq) {
				colorBySequence(atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.charge) {
				colorByCharge(atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.restype) {
				colorByResType(atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.rescharge) {
				colorByResCharge(atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.resnum) {
				colorByResnum(atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.resid) {
				colorByResid(atomRep.selection);
			}
			else if (savedParams.colorationType == colorType.bfactor) {
				colorByBfactor(atomRep.selection, savedParams.bfactorStartColor, savedParams.bfactorMidColor, savedParams.bfactorEndColor);
			}

			SetShininess(savedParams.smoothness);
			ShowShadows(savedParams.shadow);
			atomRep.colorationType = savedParams.colorationType;
		}
		else {
			Debug.LogError("Could not restore representation parameters");
		}

	}

}
}