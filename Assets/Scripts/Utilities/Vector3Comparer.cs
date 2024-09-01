using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 
class Vector3Comparer : IComparer<Vector3>
{
    public static float epsilon = 0.000000001f;
    public int Compare(Vector3 a, Vector3 b) {
        if(Mathf.Abs(a.x - b.x) < epsilon){
            if(Mathf.Abs(a.y - b.y) < epsilon){
                if(Mathf.Abs(a.z - b.z) < epsilon) return 0;
                else if(a.z < b.z) return -1;
            }
            else if(a.y < b.y) return -1;
        }
        else if(a.x < b.x) return -1;
        return 1;
        // if      (a.x <= b.x && a.y <= b.y && a.z < b.z) return -1;
        // else if (a.x <= b.x && a.y < b.y) return -1;
        // else if (a.x < b.x) return -1;
        // else return 1;
    }
}