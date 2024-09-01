using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

class Int3Comparer : IComparer<int3>
{
    public int Compare(int3 a, int3 b) {
        if(a.x == b.x){
            if(a.y == b.y){
                if(a.z == b.z) return 0;
                else if(a.z < b.z) return -1;
            }
            else if(a.y < b.y) return -1;
        }
        else if(a.x < b.x) return -1;
        return 1;
    }
}