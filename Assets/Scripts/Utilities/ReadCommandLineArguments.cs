using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UMol.API;

namespace UMol {
public class ReadCommandLineArguments : MonoBehaviour {


	private static List<string> GetArg(string name)
	{
		var args = System.Environment.GetCommandLineArgs();

		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] == name && args.Length > i + 1)
			{
				List<string> res = new List<string>();
				for (int j = i + 1; j < args.Length; j++) {
					if (args[j].StartsWith("-")) {
						break;
					}
					res.Add(args[j]);
				}
				return res;
			}
		}
		return new List<string>();
	}


	void Start() {
		List<string> filePaths = GetArg("-i");
		List<string> directory = GetArg("-d");
		if (directory.Count > 1) {
			Debug.LogError("Only using the first directory specified");
		}
		if (directory.Count > 0) {
			Debug.Log("Setting directory to '" + directory[0] + "'");
			APIPython.cd(Path.GetFullPath(directory[0]));
		}

		foreach (string p in filePaths) {
			if (PythonUtils.IsPythonFile(p)) {
				APIPython.loadHistoryScript(p);
			}
			else if (p.Length == 4 && !p.Contains(".")) {
				APIPython.fetch(p);
			}
			else {
				if (p.EndsWith(".xtc") && APIPython.last() != null) {
					string lastStructureName = APIPython.last().name;
					if (lastStructureName != null) {
						APIPython.loadTraj(lastStructureName, p);
					}
				}
				else if (p.EndsWith(".dx") && APIPython.last() != null) {
					string lastStructureName = APIPython.last().name;
					if (lastStructureName != null) {
						APIPython.loadDXmap(lastStructureName, p);
					}
				}
				else if (p.EndsWith(".itp") && APIPython.last() != null) {
					string lastStructureName = APIPython.last().name;
					if (lastStructureName != null)
						APIPython.loadMartiniITP(lastStructureName, p);
				}
				else if (p.EndsWith(".psf") && APIPython.last() != null) {
					string lastStructureName = APIPython.last().name;
					if (lastStructureName != null)
						APIPython.loadPSFTopology(lastStructureName, p);
				}
				else if (p.EndsWith(".top") && APIPython.last() != null) {
					string lastStructureName = APIPython.last().name;
					if (lastStructureName != null)
						APIPython.loadTOPTopology(lastStructureName, p);
				}
				else {
					APIPython.load(p);
				}

			}
		}


	}
}
}