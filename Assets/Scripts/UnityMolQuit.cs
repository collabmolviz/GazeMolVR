using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UMol {
public class UnityMolQuit : MonoBehaviour
{

	void OnApplicationQuit() {
		//Delete all loaded molecules
		UnityMolStructureManager sm = UnityMolMain.getStructureManager();
		List<string> sNames = sm.structureToGameObject.Keys.ToList();
		foreach (string s in sNames) {
			sm.Delete(sm.GetStructure(s));
		}
		UnityMolMain.getCustomRaycast().Clean();
		//Record command history to player prefs to restore them later
		PythonConsole2.addCommandsToUserPref(PythonConsole2.m_previousCommands);


		CenterOfGravBurst.curRes.Dispose();
		CenterOfGravBurst.curMin.Dispose();
		CenterOfGravBurst.curMax.Dispose();

	}
}
}