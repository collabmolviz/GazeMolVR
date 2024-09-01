using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UMol.API;

namespace UMol {
public class CutplaneHB : MonoBehaviour
{

	private Plane cutPlane;
	private Camera mainCam;

	public bool doCutPlane = false;
	public float distplane = 1f;
	public float cutPlaneZ = 10.0f;


	void Start(){
		activateCutPlane();
	}

	// Update is called once per frame
	void Update()
	{
		if (mainCam == null) {
			mainCam = Camera.main;
		}

		if(doCutPlane){
			hideAtomsCutplane();
			cutPlane.distance = cutPlaneZ;
		}


	}

	void activateCutPlane() {
		if (mainCam == null) {
			mainCam = Camera.main;
		}
		distplane = 10.0f;
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCam);
		cutPlane = planes[4];
		cutPlane.distance /= distplane;
		cutPlane.normal *= -1;
	}

	void hideAtomsCutplane() {
		UnityMolStructure s = APIPython.last();
		HashSet<UnityMolAtom> toHide = new HashSet<UnityMolAtom>();

		RepType rt = APIPython.getRepType("hb");

		for (int i = 0; i < s.representations.Count; i++) {
			UnityMolRepresentation rep = s.representations[i];
			if (rep.repType == rt) {
				foreach (SubRepresentation sr in rep.subReps) {
					((UnityMolHBallMeshManager)sr.atomRepManager).ResetVisibility();
					((UnityMolHStickMeshManager)sr.bondRepManager).ResetVisibility();
				}
			}
		}

		foreach (UnityMolAtom a in s.currentModel.allAtoms) {
			Vector3 p = a.curWorldPosition;
			if (cutPlane.GetSide(p)) {
				foreach(UnityMolAtom ar in a.residue.atoms.Values){
					toHide.Add(ar);
				}
			}
		}
		for (int i = 0; i < s.representations.Count; i++) {
			UnityMolRepresentation rep = s.representations[i];
			if (rep.repType == rt) {
				foreach (SubRepresentation sr in rep.subReps) {
					// ((UnityMolHBallMeshManager)sr.atomRepManager).ShowAtoms(toShow, true);
					((UnityMolHBallMeshManager)sr.atomRepManager).ShowAtoms(toHide, false);

					// ((UnityMolHStickMeshManager)sr.bondRepManager).ShowAtoms(toShow, true);
					((UnityMolHStickMeshManager)sr.bondRepManager).ShowAtoms(toHide, false);
				}
			}
		}
	}

}
}