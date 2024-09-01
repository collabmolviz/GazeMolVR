using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using Unity.Mathematics;

namespace UMol {
public class MarchingCubesWrapper {

	public enum MCType
	{
		CS = 0,
		CPP = 1,
		CUDA = 2,
		CJOB = 3
	};

	IntPtr cppMCInstance;
	IntPtr cudaMCInstance;
	MarchingCubesBurst mcb;

	[DllImport("CUDAMarchingCubes")]
	public static extern IntPtr cuda_getMCObj(float[] gridVal, int sizeX, int sizeY, int sizeZ);
	[DllImport("CUDAMarchingCubes")]
	public static extern void cuda_ComputeMesh(IntPtr instance, float isoValue, out int vertnumber, out int facenumber);
	[DllImport("CUDAMarchingCubes")]
	public static extern IntPtr cuda_getVertices(IntPtr instance);
	[DllImport("CUDAMarchingCubes")]
	public static extern IntPtr cuda_getTriangles(IntPtr instance);
	[DllImport("CUDAMarchingCubes")]
	public static extern void cuda_Destroy(IntPtr instance);
	[DllImport("CUDAMarchingCubes")]
	public static extern void cuda_freeMeshData(IntPtr instance);

	[DllImport("CPPMarchingCubes")]
	public static extern IntPtr getMCObj(float[] gridVal, int sizeX, int sizeY, int sizeZ);
	[DllImport("CPPMarchingCubes")]
	public static extern void ComputeMesh(IntPtr instance, float isoValue, out int vertnumber, out int facenumber);
	[DllImport("CPPMarchingCubes")]
	public static extern IntPtr getVertices(IntPtr instance);
	[DllImport("CPPMarchingCubes")]
	public static extern IntPtr getTriangles(IntPtr instance);
	[DllImport("CPPMarchingCubes")]
	public static extern void Destroy(IntPtr instance);
	[DllImport("CPPMarchingCubes")]
	public static extern void freeMeshData(IntPtr instance);

	float[] densVal;
	int3 gridSize;
	Dictionary<int, int> cellIdToAtomId;
	UnityMolStructure curStructure;

	//Default is C# job implementation
	public MCType mcMode = MCType.CJOB;

	public void Init(float[] densityValues, Vector3[] gradient,
	                 int3 sizeGrid, Vector3 ori, Vector3[] deltas,
	                 Vector3[] cellDir, Dictionary<int, int> cellToAtom = null,
	                 UnityMolStructure s = null) {
		densVal = densityValues;
		gridSize = sizeGrid;
		cellIdToAtomId = cellToAtom;
		curStructure = s;

		mcb = new MarchingCubesBurst(densVal, gradient, gridSize, ori, deltas, cellDir);

// 		if (mcMode == MCType.CUDA) {
// 			if (CudaAvailable.canRunCuda()) {
// 				try {
// 					cudaMCInstance = cuda_getMCObj(densVal, gridSize.x, gridSize.y, gridSize.z);
// 					Debug.Log("Using CUDA Marching Cubes implementation");
// 				}
// 				catch (System.Exception e) {
// 					// CPP version is somehow slower (memory copy probably slow) => use the CJOB version
// 					mcMode = MCType.CJOB;
// #if UNITY_EDITOR
// 					Debug.LogError(e);
// #endif
// 				}
// 			}
// 			else {
// 				mcMode = MCType.CJOB;
// 				Debug.Log("Using C# Marching Cubes implementation");
// 			}
// 		}
// 		if (mcMode == MCType.CJOB) {
// 			Vector3 oriXInv = ori;
// 			oriXInv.x *= -1;
// 			mcb = new MarchingCubesBurst(densVal, gridSize, oriXInv, dx);
// 		}
// 		else{
// 			mcMode = MCType.CS;
// 		}
	}

	public MeshData computeMC(float isoValue) {

		MeshData mData = new MeshData();

		IntPtr IntArrayPtrVertices = IntPtr.Zero;
		IntPtr IntArrayPtrTriangles = IntPtr.Zero;

		// if (mcMode == MCType.CUDA) {
		// 	cuda_ComputeMesh(cudaMCInstance, isoValue, out vertNumber, out faceNumber);

		// 	IntArrayPtrVertices = cuda_getVertices(cudaMCInstance);
		// 	IntArrayPtrTriangles = cuda_getTriangles(cudaMCInstance);
		// }
		// else if (mcMode == MCType.CPP) {
		// 	ComputeMesh(cppMCInstance, isoValue, out vertNumber, out faceNumber);

		// 	IntArrayPtrVertices = getVertices(cppMCInstance);
		// 	IntArrayPtrTriangles = getTriangles(cppMCInstance);

		// }
		// if (mcMode == MCType.CPP || mcMode == MCType.CUDA) {
		// 	float[] vertices = new float[vertNumber * 3];
		// 	int[] triangles = new int[faceNumber * 3];

		// 	Marshal.Copy(IntArrayPtrVertices, vertices, 0, 3 * vertNumber);
		// 	Marshal.Copy(IntArrayPtrTriangles, triangles, 0, 3 * faceNumber);

		// 	// Marshal.FreeCoTaskMem(IntArrayPtrVertices);
		// 	// Marshal.FreeCoTaskMem(IntArrayPtrTriangles);

		// 	// freeMeshData();

		// 	Vector3[] allVertices = new Vector3[vertNumber];
		// 	Vector3[] normals = new Vector3[vertNumber];
		// 	for (long i = 0; i < vertNumber; i++) {
		// 		Vector3 v = new Vector3(-vertices[i * 3], vertices[i * 3 + 1], vertices[i * 3 + 2]);
		// 		allVertices[i] = v;
		// 		normals[i] = Vector3.zero;
		// 	}

		// 	mData.triangles = triangles;
		// 	mData.vertices = allVertices;
		// 	mData.colors = new Color32[vertNumber];
		// 	mData.normals = normals;
		// }
		// if (mcMode == MCType.CS) {

		// 	// //CS version
		// 	// Marching marching = new MarchingCubes();
		// 	// marching.Surface = isoValue;

		// 	//       List<Vector3> verts = new List<Vector3>();
		// 	//       List<int> indices = new List<int>();

		// 	// marching.Generate(densVal, gridSize.x, gridSize.y, gridSize.z, verts, indices);

		// 	// MeshData mData = new MeshData();
		// 	// mData.triangles = indices.ToArray();
		// 	// mData.vertices = verts.ToArray();
		// 	// mData.colors = new Color32[verts.Count];
		// 	// mData.normals = new Vector3[indices.Count];

		// 	// //CS version 2
		// 	MarchingCubesSimple marching = new MarchingCubesSimple();

		// 	List<Vector3> verts = new List<Vector3>();
		// 	List<int> indices = new List<int>();

		// 	// marching.Generate(densVal, gridSize.x, gridSize.y, gridSize.z, verts, indices);
		// 	marching.marchingCubes(densVal, gridSize, ref indices, ref verts, isoValue);

		// 	mData.triangles = indices.ToArray();
		// 	mData.vertices = verts.ToArray();
		// 	mData.colors = new Color32[verts.Count];
		// 	mData.normals = new Vector3[verts.Count];
		// }
		// else if (mcMode == MCType.CJOB) {
		mcb.computeIsoSurface(isoValue);

		Vector3[] newVerts = mcb.getVertices();
		Vector3[] newNorms = mcb.getNormals();
		//Invert x for vertices and normals
		if (isoValue > 0.0f) {
			for (int i = 0; i < newVerts.Length; i++) {
				// newVerts[i].x *= -1;
				newNorms[i] *= -1;
			}
		}
		int[] newTri = mcb.getTriangles();
		Color32[] newCols = mcb.getColors();

		mData.triangles = newTri;
		mData.vertices = newVerts;
		mData.colors = newCols;
		mData.normals = newNorms;
		mData.nVert = newVerts.Length;
		mData.nTri = newTri.Length;

		if (curStructure != null && cellIdToAtomId != null) {
			int[] vertexToCellId = mcb.getVertexToCellId();
			mData.atomByVert = new int[newVerts.Length];

			for (int i = 0; i < newVerts.Length; i ++) {
				int idVoxel = vertexToCellId[i];
				if (cellIdToAtomId.ContainsKey(idVoxel)) {
					int idAtom = cellIdToAtomId[idVoxel];
					mData.atomByVert[i] = idAtom;
				}
			}
		}

		return mData;
	}

	public void FreeMC() {
		if (cppMCInstance != IntPtr.Zero) {
			Destroy(cppMCInstance);
		}
		if (cudaMCInstance != IntPtr.Zero) {
			Destroy(cudaMCInstance);
		}
		if (mcb != null) {
			mcb.Clean();
			mcb = null;
		}
	}
}
}
