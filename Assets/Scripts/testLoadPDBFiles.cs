using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UMol.API;

namespace UMol {
public class testLoadPDBFiles : MonoBehaviour {

	public List<string> pdbIds = new List<string>();
	public bool usemmCIF = true;

	// Use this for initialization
	IEnumerator Start() {
		float start = Time.realtimeSinceStartup;
		UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();


		for (int i = 0; i < pdbIds.Count; i++) {
			UnityMolStructure newStruct = null;
			// try {
			// 	// string filePath = Application.dataPath + "/../samples/" + pdbIds[i].ToUpper();
			// 	// newStruct = UnityMolPDBParser.LoadPDB(filePath);
			// 	// newStruct = PDBHandler.ReadLocalFile(filePath);
			// } catch {
			// 	// newStruct = UnityMolPDBParser.FetchPDB(pdbIds[i].ToUpper());*
			// 	// StartCoroutine(PDBHandler.ReadRemoteFile(pdbIds[i].ToUpper(), value => newStruct = value));

			// }


			if (newStruct == null) {
				newStruct = APIPython.fetch(pdbIds[i].ToUpper(), usemmCIF);
				// if(usemmCIF) {
				// 	PDBxReader r = new PDBxReader();
				// 	yield return StartCoroutine(r.Fetch(pdbIds[i].ToUpper(), value => newStruct = value));
				// }
				// else {
				// 	PDBReader r = new PDBReader();
				// 	yield return StartCoroutine(r.Fetch(pdbIds[i].ToUpper(), value => newStruct = value));
				// }
			}
			if (newStruct != null) {

				// repManager.AddRepresentation(newStruct, AtomType.optihb, BondType.optihs);

				// // repManager.AddRepresentation(newStruct, AtomType.EDTSurface, BondType.nobond);

				// StrideWrapper.callStride(newStruct.currentModel);
				// repManager.AddRepresentation(newStruct, AtomType.cartoon, BondType.nobond);

				// Debug.Log("Number of models : "+newStruct.models.Count);
			}
			else {
				Debug.Log("Could not load pdb id " + pdbIds[i]);
			}
		}
		float stop = Time.realtimeSinceStartup;

		Debug.Log("Needed " + (stop - start) + " to parse and show pdbs");
		yield return null;

	}
}
}
