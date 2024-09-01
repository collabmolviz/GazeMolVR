using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimateTorsionAngle : MonoBehaviour {


    public Transform t1;
    public Transform t2;
    public float speed = 25.0f;

    void Update(){
        if(t1 != null && t2 != null){
            Vector3 pos = transform.TransformPoint(Vector3.zero);
            transform.RotateAround(pos, (t1.position - t2.position) , speed * Time.deltaTime);
        }
    }
}