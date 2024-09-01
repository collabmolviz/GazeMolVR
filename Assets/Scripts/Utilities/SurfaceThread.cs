using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace UMol {
public class SurfaceThread {

	public bool _isEDTRunning;
	public bool _isMSMSRunning;
	private Thread _EDTthread;
	private Thread _MSMSthread;
	public UnityMolSelection sel;

	private string tempPath;

	public void StartThread() {
		tempPath = Application.temporaryCachePath;
		_EDTthread = new Thread(ComputeEDTSurface);
		_EDTthread.Start();
		_MSMSthread = new Thread(ComputeMSMSSurface);
		_MSMSthread.Start();
	}
	void ComputeMSMSSurface() {
		_isMSMSRunning = true;

		List<UnityMolSelection> subSels = ISurfaceRepresentation.cutSelection(sel);
		int idSub = 0;
		foreach (UnityMolSelection sub in subSels) {

			//Thread stopping from outside
			if (_isMSMSRunning == false) {
				break;
			}

			//Don't compute the chain if already there
			string keyPrecomputedRep = sub.atoms[0].residue.chain.model.structure.name + "_" + sub.atoms[0].residue.chain.name + "_" + SurfMethod.MSMS.ToString();
			if (UnityMolMain.getPrecompRepManager().precomputedRep.ContainsKey(keyPrecomputedRep)) {
				continue;
			}

			//Default value from the MSMSWrapper
			float density = 15.0f;
			float probeRad = 1.4f;
			MeshData mdata = null;
			MSMSWrapper.callMSMS(ref mdata, -1, sub, density, probeRad, tempPath);
			int[] meshAtomVert = mdata.atomByVert;
			
			if (mdata != null) {
				UnityMolMain.getPrecompRepManager().precomputedRep[keyPrecomputedRep] = mdata;
				UnityMolMain.getPrecompRepManager().precomputedSurfAsso[keyPrecomputedRep] = meshAtomVert;
			}

			idSub++;
		}
		Debug.Log("(Thread) Done computing MSMS surface " + sel.name);
		_isMSMSRunning = false;
	}

	void ComputeEDTSurface() {
		_isEDTRunning = true;

		List<UnityMolSelection> subSels = ISurfaceRepresentation.cutSelection(sel);
		int idSub = 0;
		foreach (UnityMolSelection sub in subSels) {
			//Thread stopping from outside
			if (_isEDTRunning == false) {
				break;
			}

			//Don't compute the chain if already there
			string keyPrecomputedRep = sub.atoms[0].residue.chain.model.structure.name + "_" + sub.atoms[0].residue.chain.name + "_" + SurfMethod.EDTSurf.ToString();
			if (UnityMolMain.getPrecompRepManager().precomputedRep.ContainsKey(keyPrecomputedRep)) {
				continue;
			}

			Vector3[] atomPos = new Vector3[sub.Count];
			int id = 0;
			foreach (UnityMolAtom a in sub.atoms) {
				atomPos[id++] = a.position;
			}

			string pdbLines = PDBReader.Write(sub, overridedPos: atomPos);


			if (pdbLines.Length == 0 || EDTSurfWrapper.emptyAtomLines(pdbLines)) {
				//Try to write HET as Atoms
				pdbLines = PDBReader.Write(sub, writeModel: false, writeHET: true, forceHetAsAtom: true);
			}
			MeshData mdata = null;
			EDTSurfWrapper.callEDTSurf(ref mdata, sel.name + "_" + idSub.ToString(), pdbLines);
			int[] meshAtomVert = mdata.atomByVert;
			if (mdata != null) {
				UnityMolMain.getPrecompRepManager().precomputedRep[keyPrecomputedRep] = mdata;
				UnityMolMain.getPrecompRepManager().precomputedSurfAsso[keyPrecomputedRep] = meshAtomVert;

			}

			idSub++;
		}

		Debug.Log("(Thread) Done computing EDTSurf surface" + sel.name);
		_isEDTRunning = false;
	}
	public void Clear() {
		if (_isEDTRunning) {

			// Force thread to quit
			_isEDTRunning = false;

			// wait for thread to finish and clean up
			_EDTthread.Abort();
		}
		if (_isMSMSRunning) {

			// Force thread to quit
			_isMSMSRunning = false;

			// wait for thread to finish and clean up
			_MSMSthread.Abort();
		}
	}
}
}

