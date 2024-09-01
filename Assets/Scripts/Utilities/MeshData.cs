using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

public class MeshData {
    public int[] triangles;
    public Vector3[] normals;
    public Vector3[] vertices;
    public Color32[] colors;
    public float[] vertBuffer;
    public int[] colBuffer;
    public int[] atomByVert;

    public int nVert = 0;
    public int nTri = 0;

    public void Scale(Vector3 scale) {
        for (int i = 0; i < nVert; i++) {
            vertices[i] = Vector3.Scale(scale, vertices[i]);
        }
    }
    public void Offset(Vector3 offset) {
        for (int i = 0; i < nVert; i++) {
            vertices[i] += offset;
        }
    }
    public void InvertX() {
        for (int i = 0; i < nVert; i++) {
            vertices[i].x = -vertices[i].x;
        }
    }
    public void InvertTri() {
        for (int i = 0; i < nTri; i++) { //Revert the triangles
            int save = triangles[i * 3];
            triangles[i * 3] = triangles[i * 3 + 1];
            triangles[i * 3 + 1] = save;
        }
    }

    public void CopyVertBufferToVert() {
        GCHandle handleVs = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        IntPtr pV = handleVs.AddrOfPinnedObject();

        Marshal.Copy(vertBuffer, 0, pV, vertBuffer.Length);
    }

    public void FillWhite() {
        Color32 white = Color.white;
        for (int i = 0; i < nVert; i++) {
            colors[i] = white;
        }
    }
}

public struct LInt2 {
    public long x;
    public long y;
}