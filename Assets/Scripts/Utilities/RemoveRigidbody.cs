using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveRigidbody : MonoBehaviour {
    public int checkFrame = 100;

    int f = 0;
    void Update () {
        if (f == checkFrame) {
            if (GetComponent<Rigidbody>() != null) {
                Destroy(GetComponent<Rigidbody>());
            }
            f = 0;
        }
        f++;
    }
}
