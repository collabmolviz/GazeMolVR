using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.IO;
using System.Linq;

namespace UMol {

public class testMarchingCubes : MonoBehaviour{

	public string dxFilePath;
	public float isoValue = 0.0f;

	void Start(){

		if(dxFilePath != null){
			UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
			
			DXReader dxR = new DXReader(dxFilePath);
			UnityMolStructure s = UnityMolMain.getStructureManager().GetCurrentStructure();

			dxR.readDxFile(s);
			repManager.AddRepresentation(s.ToSelection(), AtomType.DXSurface, BondType.nobond, dxR, isoValue);
		}

	}
}
}