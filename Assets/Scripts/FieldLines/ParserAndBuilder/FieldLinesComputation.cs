using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Runtime.InteropServices;


namespace UMol {
public static class FieldLinesComputation {

    // #space to grid
    static int3 s2g(Vector3 pos3d, Vector3 dx, Vector3 origin, int3 dim) {
        int i = Mathf.Max(0, Mathf.FloorToInt((origin.x - pos3d.x) / dx.x));
        int j = Mathf.Max(0, Mathf.FloorToInt((pos3d.y - origin.y) / dx.y));
        int k = Mathf.Max(0, Mathf.FloorToInt((pos3d.z - origin.z) / dx.z));
        i = Mathf.Min(i, dim.x - 1);
        j = Mathf.Min(j, dim.y - 1);
        k = Mathf.Min(k, dim.z - 1);
        int3 res;
        res.x = i;
        res.y = j;
        res.z = k;
        return res;
    }

    // #grid to space
    static Vector3 g2s(int3 ijk, Vector3 dx, Vector3 origin, int3 dim) {
        float x = origin.x - ijk.x * dx.x;
        float y = origin.y + ijk.y * dx.y;
        float z = origin.z + ijk.z * dx.z;
        Vector3 res = new Vector3(x, y, z);
        return res;
    }
    // #to know if the trajectory go out of the grid3D
    static bool isInBox(Vector3 pos, Vector3 dx, Vector3 origin, int3 dim) {
        if (pos.x < origin.x - dim.x * dx.x)
            return false;
        if (pos.x > origin.x)
            return false;
        if (pos.y > origin.y + dim.y * dx.y)
            return false;
        if (pos.y < origin.y)
            return false;
        if (pos.z > origin.z + dim.z * dx.z)
            return false;
        if (pos.z < origin.z)
            return false;
        return true;
    }

    static List<int3> getSeeds(Vector3[] grad, int3 gridSize, float gradThreshold) {

        float minGrad = (gradThreshold * 0.5f);
        float maxGrad = (gradThreshold * 1.5f);
        float minGrad2 = minGrad * minGrad;
        float maxGrad2 = maxGrad * maxGrad;

        List<int3> ids = new List<int3>();
        for (int i = 0; i < gridSize.x; i++) {
            for (int j = 0; j < gridSize.y; j++) {
                for (int k = 0; k < gridSize.z; k++) {
                    int idGrad = ((gridSize.z * gridSize.y * i) + (gridSize.z * j) + k);

                    if (grad[idGrad].sqrMagnitude >= minGrad2 && grad[idGrad].sqrMagnitude <= maxGrad2) {
                        int3 id;
                        id.x = i; id.y =  j; id.z = k;
                        ids.Add(id);
                    }
                }
            }
        }
        return ids;
    }

    static bool IsNaN(Vector3 p) {
        return float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z);
    }

    public static List<Vector3>[] computeFL(Vector3[] grad, Vector3[] dx, Vector3 origin,
                                            int3 gridSize, int nbIter, float gradThreshold,
                                            float xl, float yl, float zl,
                                            float minLength = 10.0f, float maxLength = 50.0f) {

        return FieldLinesBurst.computeFL(grad, dx, origin, gridSize, nbIter, gradThreshold, xl, yl, zl, minLength, maxLength);

    }

    public static FieldLinesReader computeFieldlinesToFLReader(DXReader r, int nbIter, float gradThreshold) {

        Vector3[] grad = r.gradient;

        List<Vector3>[] fl = computeFL(grad, r.deltaS, r.origin, r.gridSize, nbIter, gradThreshold, r.xl, r.yl, r.zl);

        if (fl == null) {
            return null;
        }

        FieldLinesReader fakeFLR = new FieldLinesReader();
        Dictionary<string, List<Vector3>> linesPos = new Dictionary<string, List<Vector3>>();
        int id = 0;
        for (int i = 0; i < fl.Length; i++) {
            if (fl[i].Count != 0) {
                linesPos[id.ToString()] = fl[i];
                id++;
            }
        }
        fakeFLR.linesPositions = linesPos;
        return fakeFLR;
    }

}
}