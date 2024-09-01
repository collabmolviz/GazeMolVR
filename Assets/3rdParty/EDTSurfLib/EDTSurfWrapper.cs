using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Threading;

namespace UMol {


public class EDTSurfWrapper {

	[DllImport("EDTSurfLib")]
	public static extern IntPtr getEDTSurfLibrary();

	[DllImport("EDTSurfLib")]
	public static extern void ComputeSurfaceMesh(IntPtr instance, string name, string lines, out int vertnumber, out int facenumber);

	[DllImport("EDTSurfLib")]
	public static extern IntPtr getVertices(IntPtr instance);

	[DllImport("EDTSurfLib")]
	public static extern IntPtr getColors(IntPtr instance);

	[DllImport("EDTSurfLib")]
	public static extern IntPtr getTriangles(IntPtr instance);

	[DllImport("EDTSurfLib")]
	public static extern IntPtr getAtomVert(IntPtr instance);

	[DllImport("EDTSurfLib")]
	public static extern void freeMeshData(IntPtr instance);

	[DllImport("EDTSurfLib")]
	public static extern void Destroy(IntPtr instance);

	[ThreadStatic] //Ensure thread safe
	public static StringBuilder sbEDT;

	public static void createEDTSurface(int idF, Transform meshPar, string name, UnityMolSelection select, ref MeshData mData, Material curMat) {
		if (select.atoms.Count < 10) {
			Debug.LogError("Cannot create an EDTSurf surface for a selection containing less than 10 atoms");
			return;
		}

		Vector3[] atomPos = null;
		if (idF != -1) {
			atomPos = select.extractTrajFramePositions[idF];
		}
		else {
			atomPos = new Vector3[select.atoms.Count];
			int id = 0;
			foreach (UnityMolAtom a in select.atoms) {
				atomPos[id++] = a.position;
			}
		}

		if (sbEDT == null)
			sbEDT = new StringBuilder();

		string pdbLines = PDBReader.Write(select, overridedPos: atomPos, sw: sbEDT);

		if (pdbLines.Length == 0 || emptyAtomLines(pdbLines)) {
			//Try to write HET as Atoms
			pdbLines = PDBReader.Write(select, writeModel: false, writeHET: true, forceHetAsAtom: true, sw: sbEDT);
		}

		callEDTSurf(ref mData, name, pdbLines);

	}



	//Calls native plugin EDTSurf to create meshes and return the number of meshes created
	public static void callEDTSurf(ref MeshData mData, string pdbName, string pdbLines) {

		if (pdbLines.Length == 0 || emptyAtomLines(pdbLines)) {
			Debug.LogWarning("No atoms for surface");
			return;
		}

		int vertNumber;
		int faceNumber;

		IntPtr IntArrayPtrVertices;
		IntPtr IntArrayPtrColors;
		IntPtr IntArrayPtrTriangles;
		IntPtr IntArrayPtrAtomVert;

		IntPtr EDTSurfObj = getEDTSurfLibrary();

		if (EDTSurfObj == IntPtr.Zero) {
			Debug.LogError("Something went wrong when initializing EDTSurf library");
			return;
		}


		ComputeSurfaceMesh(EDTSurfObj, pdbName, pdbLines, out vertNumber, out faceNumber);

		IntArrayPtrVertices = getVertices(EDTSurfObj);
		IntArrayPtrColors = getColors(EDTSurfObj);
		IntArrayPtrTriangles = getTriangles(EDTSurfObj);
		IntArrayPtrAtomVert = getAtomVert(EDTSurfObj);

		if (mData == null) {
			mData = new MeshData();

			mData.triangles = new int[faceNumber * 3];
			mData.vertices =  new Vector3[vertNumber];
			mData.vertBuffer = new float[vertNumber * 3];
			mData.colBuffer = new int[vertNumber * 3];
			mData.colors = new Color32[vertNumber];
			mData.normals = new Vector3[vertNumber];
			mData.atomByVert = new int[vertNumber];
			mData.nVert = vertNumber;
			mData.nTri = faceNumber;
		}
		else {

			if (vertNumber > mData.nVert) { //New mesh is bigger than previous one => allocate more
				mData.triangles = new int[faceNumber * 3];
				mData.vertices =  new Vector3[vertNumber];
				mData.vertBuffer = new float[vertNumber * 3];
				mData.colBuffer = new int[vertNumber * 3];
				mData.colors = new Color32[vertNumber];
				mData.normals = new Vector3[vertNumber];
				mData.atomByVert = new int[vertNumber];
			}

			mData.nVert = vertNumber;
			mData.nTri = faceNumber;
		}


		Marshal.Copy(IntArrayPtrVertices, mData.vertBuffer, 0, 3 * vertNumber);
		Marshal.Copy(IntArrayPtrColors, mData.colBuffer, 0, 3 * vertNumber);
		Marshal.Copy(IntArrayPtrTriangles, mData.triangles, 0, 3 * faceNumber);
		Marshal.Copy(IntArrayPtrAtomVert, mData.atomByVert, 0, vertNumber);


		Marshal.FreeCoTaskMem(IntArrayPtrVertices);
		Marshal.FreeCoTaskMem(IntArrayPtrColors);
		Marshal.FreeCoTaskMem(IntArrayPtrTriangles);
		Marshal.FreeCoTaskMem(IntArrayPtrAtomVert);

		// freeMeshData(EDTSurfObj);
		// Destroy(EDTSurfObj);

		mData.FillWhite();
		mData.CopyVertBufferToVert();

		mData.InvertX();
		mData.InvertTri();
	}

	/// <summary>
	/// Test if there is ATOM in lines
	/// </summary>
	public static bool emptyAtomLines(string lines) {
		return lines.IndexOf("ATOM") == -1;
	}
}
}

