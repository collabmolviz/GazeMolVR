using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using UMol.API;

namespace UMol {
public class WrapperCudaSES  {

    [DllImport("SESCuda")]
    public static extern void API_computeSES(float resoSES, Vector3 [] atomPos, float [] atomRadii, int N,
            out IntPtr out_vertices, out int NVert, out IntPtr out_triangles , out int NTri, int smooth);

    [DllImport("SESCuda")]
    public static extern IntPtr API_getVertices();

    [DllImport("SESCuda")]
    public static extern IntPtr API_getTriangles(bool invertTri);

    [DllImport("SESCuda")]
    public static extern IntPtr API_getAtomIdPerVert();

    [DllImport("SESCuda")]
    public static extern void API_freeMesh();

    public static void createCUDASESSurface(int idF, Transform meshPar, string name, UnityMolSelection select, ref MeshData mData,
                                            float resolutionSES = 0.3f, int smoothingSteps = 1) {

        int N = select.atoms.Count;
        Vector3[] pos = new Vector3[N];
        float[] radii = new float[N];
        int id = 0;
        foreach (UnityMolAtom a in select.atoms) {
            pos[id] = a.position;
            if (idF != -1) {
                pos[id] = select.extractTrajFramePositions[idF][id];
            }
            radii[id] = a.radius;
            id++;
        }


        IntPtr outVertices = IntPtr.Zero;
        IntPtr outTriangles = IntPtr.Zero;
        IntPtr outAtomIdPerVert = IntPtr.Zero;

        int NVert = 0;
        int NTri = 0;

        float timerSES = Time.realtimeSinceStartup;

        API_computeSES(resolutionSES, pos, radii, N, out outVertices, out NVert, out outTriangles, out NTri, smoothingSteps);

        if (NVert != 0 && NTri != 0) {
            outTriangles = API_getTriangles(true);
            outVertices = API_getVertices();
            outAtomIdPerVert = API_getAtomIdPerVert();

            if (mData == null) {
                mData = new MeshData();
                mData.vertBuffer = new float[NVert * 3];
                mData.vertices = new Vector3[NVert];
                mData.triangles = new int[NTri];
                mData.colors = new Color32[NVert];
                mData.normals = new Vector3[NVert];
                mData.atomByVert = new int[NVert];
            }
            else {
                if (NVert > mData.vertices.Length) {//We need more space
                    mData.vertBuffer = new float[NVert * 3];
                    mData.vertices = new Vector3[NVert];
                    mData.colors = new Color32[NVert];
                    mData.normals = new Vector3[NVert];
                    mData.atomByVert = new int[NVert];
                    mData.triangles = new int[NTri];
                }
            }
            mData.nVert = NVert;
            mData.nTri = NTri / 3;


            // float timerMarshal = Time.realtimeSinceStartup;

            Marshal.Copy(outVertices, mData.vertBuffer, 0, NVert * 3);
            Marshal.Copy(outTriangles, mData.triangles, 0, NTri);
            Marshal.Copy(outAtomIdPerVert, mData.atomByVert, 0, NVert);

            // Debug.Log("Time for Marhal: " + (1000.0f * (Time.realtimeSinceStartup - timerMarshal)).ToString("f3") + " ms");

            // float timerPostpro = Time.realtimeSinceStartup;


            mData.FillWhite();
            mData.CopyVertBufferToVert();
            
            // Debug.Log("Time for ppost pro: " + (1000.0f * (Time.realtimeSinceStartup - timerPostpro)).ToString("f3") + " ms");

        }
        else {
            Debug.LogError("Failed to compute SES");
            return;
        }

        float timerSES2 = Time.realtimeSinceStartup;


        // Debug.Log("Time for SES: " + (1000.0f * (timerSES2 - timerSES)).ToString("f3") + " ms");

        //Free mem
        API_freeMesh();

        // Marshal.FreeCoTaskMem(outVertices);
        // Marshal.FreeCoTaskMem(outTriangles);
    }
}
}