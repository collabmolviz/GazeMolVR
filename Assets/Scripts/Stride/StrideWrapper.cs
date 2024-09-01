using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UMol {

public static class StrideWrapper {

	[DllImport ("StrideLib")]
	public static extern IntPtr  runStride(string pdbContent);

	[DllImport ("StrideLib")]
	public static extern IntPtr  runStridePath(string pdbPath);

	public static void callStride(UnityMolModel model){
		List<UnityMolAtom> atoms = model.allAtoms;
		Debug.Log("Calling stride with "+atoms.Count+" atoms");
		string filecontent = PDBReader.Write(model.ToSelection(), writeHET: false);
		string ssString = null;
		// if(false){
		// 	string path = Application.temporaryCachePath+"/tmpstride.pdb";
		// 	Debug.Log("Writting to = '"+path+"'");
		// 	StreamWriter writer = new StreamWriter(path, false);
	 //        writer.WriteLine(filecontent);
	 //        writer.Close();


		// 	ssString = Marshal.PtrToStringAnsi(runStridePath(path));
		// }
		// else{
			ssString = Marshal.PtrToStringAnsi(runStride(filecontent));
		// }

		// Debug.Log(filecontent);
		// Debug.Log("Result = "+ssString);
		if(ssString == null || ssString.Length == 0 || ssString.StartsWith("Error")){
			Debug.LogWarning("Stride failed");
			#if UNITY_EDITOR
			Debug.Log(ssString);
			if(ssString.StartsWith("Error reading PDB file")){
				Debug.Log(filecontent);
			}
			#endif
			return;
		}

		Dictionary<UnityMolResidue,bool> doneResi = new Dictionary<UnityMolResidue,bool>();
		foreach(UnityMolAtom a in atoms){
			doneResi[a.residue] = false;
		}

		var allResi = doneResi.Keys.ToList();

		string[] lines = ssString.Split(new [] {"\r", "\n", System.Environment.NewLine}, System.StringSplitOptions.RemoveEmptyEntries);
		for(int i=0;i<lines.Length;i++){
			string s = lines[i];

			bool isHelixLine = s.StartsWith ("HELIX");
			bool isSheetLine = s.StartsWith("SHEET");
			bool isAccLine = s.StartsWith("ACC");
			bool isDonLine = s.StartsWith("DNR");


			if(isHelixLine){
				string chainh = s.Substring (19,2).Trim ();
				string initr = s.Substring(22,4);
				string termr = s.Substring (34,4);
				string classH = s.Substring(39,2);

				int initres = int.Parse (initr);
				int termres = int.Parse (termr);

				int classhelix = 1;
				try{
					classhelix = int.Parse (classH);
				}catch{
					classhelix = int.Parse (s.Substring(38,2));
				}

				foreach(UnityMolResidue r in allResi){
					bool done = doneResi[r];

					if(!done){//Residue not already set
						if(r.chain.name == chainh){
							if(r.id >= initres && r.id <= termres){
								r.secondaryStructure = (UnityMolResidue.secondaryStructureType)classhelix;
								doneResi[r] = true;
							}
						}
					}
				}
			}

			if(isSheetLine){
				string chainS = s.Substring (21, 2).Trim ();
				string initr = s.Substring (23, 4);
				string termr = s.Substring (34, 4);
				int initres = int.Parse (initr);
				int termres = int.Parse (termr);

				foreach(UnityMolResidue r in allResi){
					bool done = doneResi[r];
					if(!done){//Residue not already set
						if(r.chain.name == chainS){
							if(r.id >= initres && r.id <= termres){
								r.secondaryStructure = UnityMolResidue.secondaryStructureType.Strand;
								doneResi[r] = true;
							}
						}
					}
				}
			}

			if(isAccLine){
				string[] tokens = s.Split(new [] { ' '}, System.StringSplitOptions.RemoveEmptyEntries);
				string chain1 = tokens[2];
				int idResAcc = int.Parse(tokens[3]);
				string chain2 = tokens[7];
				int idResDon = int.Parse(tokens[8]);
				// Debug.Log(model.chains[chain1].residues[idResAcc]+"    ---    "+model.chains[chain2].residues[idResDon]);
			}

		}
	}
}
}
