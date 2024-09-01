using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using UMol.API;

namespace UMol {
public class WrapperCudaQuickSurf  {
    [DllImport("QuickSurf")]
    public static extern void API_computeQS(Vector3 [] atomPos, float [] atomRadii, int N,
                                            out int NVert, out int NTri, float rscale, float gspace, float glim, float iso);

    [DllImport("QuickSurf")]
    public static extern IntPtr API_getVertices();

    [DllImport("QuickSurf")]
    public static extern IntPtr API_getNormals();

    [DllImport("QuickSurf")]
    public static extern IntPtr API_getTriangles(bool invertTri);

    [DllImport("QuickSurf")]
    public static extern IntPtr API_getAtomIdPerVert();

    [DllImport("QuickSurf")]
    public static extern void API_freeMesh();


    [ThreadStatic] //Ensure thread safe
    public static Vector3[] posBuffer;
    [ThreadStatic] //Ensure thread safe
    public static float[] radBuffer;

    public static void createSurface(int idF, Transform meshPar, string name,
                                     UnityMolSelection select, ref MeshData mData,
                                     float rscale = 1.0f, float gspace = 1.0f, float glim = 1.5f, float iso = 0.5f) {

        int N = select.atoms.Count;

        if (posBuffer == null || N > posBuffer.Length) {
            posBuffer = new Vector3[N];
            radBuffer = new float[N];
        }


        int id = 0;
        foreach (UnityMolAtom a in select.atoms) {
            posBuffer[id] = a.position;
            if (idF != -1) {
                posBuffer[id] = select.extractTrajFramePositions[idF][id];
            }
            radBuffer[id] = a.radius;
            id++;
        }

        IntPtr outVertices = IntPtr.Zero;
        IntPtr outTriangles = IntPtr.Zero;
        IntPtr outNormals = IntPtr.Zero;
        IntPtr outAtomIdPerVert = IntPtr.Zero;
        float[] resultVerticesf;
        Vector3[] resultVertices;
        int[] resultTriangles;
        int[] resultAtomIdPerVert;
        int NVert = 0;
        int NTri = 0;

        float timerQS = Time.realtimeSinceStartup;

        API_computeQS(posBuffer, radBuffer, N, out NVert, out NTri, rscale, gspace, glim, iso);

        if (NVert != 0 && NTri != 0) {
            outTriangles = API_getTriangles(false);
            outVertices = API_getVertices();
            outNormals = API_getNormals();
            outAtomIdPerVert = API_getAtomIdPerVert();

            if (mData == null) {
                mData = new MeshData();
                mData.vertices = new Vector3[NVert];
                mData.triangles = new int[NTri];
                mData.colors = new Color32[NVert];
                mData.normals = new Vector3[NVert];
                mData.atomByVert = new int[NVert];
                mData.vertBuffer = new float[NVert * 3];
                mData.nVert = NVert;
                mData.nTri = NTri / 3;
            }
            else {
                if (NVert > mData.vertices.Length) {//We need more space
                    mData.vertices = new Vector3[NVert];
                    mData.colors = new Color32[NVert];
                    mData.normals = new Vector3[NVert];
                    mData.atomByVert = new int[NVert];
                    mData.vertBuffer = new float[NVert * 3];
                    mData.nVert = NVert;
                }
                else {//Do not reallocate, just use a part of the buffer
                }

                if (NTri > mData.triangles.Length) {//We need more space
                    mData.triangles = new int[NTri];
                }

                mData.nTri = NTri / 3;
                mData.nVert = NVert;
            }

            Marshal.Copy(outVertices, mData.vertBuffer, 0, NVert * 3);
            Marshal.Copy(outTriangles, mData.triangles, 0, NTri);
            Marshal.Copy(outAtomIdPerVert, mData.atomByVert, 0, NVert);


            mData.FillWhite();
            mData.CopyVertBufferToVert();

            Marshal.Copy(outNormals, mData.vertBuffer, 0, NVert * 3);
            
            for (int i = 0; i < NVert; i++) {
                mData.normals[i] = new Vector3(mData.vertBuffer[i * 3 + 0],
                                               mData.vertBuffer[i * 3 + 1],
                                               mData.vertBuffer[i * 3 + 2]);
            }

        }
        else {
            Debug.LogError("Failed to compute QuickSurf");
            return;
        }

        float timerQS2 = Time.realtimeSinceStartup;

        // Debug.Log("Time for QS: " + (1000.0f * (timerQS2 - timerQS)).ToString("f3") + " ms");

        //Free mem
        API_freeMesh();

    }
}
}
