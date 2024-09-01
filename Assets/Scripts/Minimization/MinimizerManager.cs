using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMol {

public class MinimizerManager {

	public void runMinimizer(List<UnityMolStructure> structures, int steps) {

		int totalAtoms = 0;
		List<string> types = new List<string>();
		List<float> pos = new List<float>();

		foreach (UnityMolStructure s in structures) {
			totalAtoms += s.Count;
			Vector3[] sPos = s.getWorldPositions();
			for (int i = 0; i < s.Count; i++) {
				types.Add(s.currentModel.allAtoms[i].type);
				pos.Add(-sPos[i].x);
				pos.Add(sPos[i].y);
				pos.Add(sPos[i].z);
			}
		}

		float[] result = new float[totalAtoms * 3];

		bool success = false;
		//Call external Minimizer
		///


		if (success) {
			int cpt = 0;
			foreach (UnityMolStructure s in structures) {
				//If list in not initialized
				if (s.trajAtomPositions == null ||
				        s.trajAtomPositions.Length != s.Count) {
					s.trajAtomPositions = new Vector3[s.Count];
				}
				//Copy the result positions (X inverted)
				for (int i = 0; i < s.Count; i++) {
					s.trajAtomPositions[i] = new Vector3(
					    -result[cpt],
					    result[cpt + 1],
					    result[cpt + 2]);
					
					cpt += 3;
				}
				//Update real atom positions and representations
				s.trajUpdateAtomPositions();
				s.updateRepresentations(trajectory: true);
			}
		}

	}
}
}