using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleTextCameraDistance : MonoBehaviour {
    public float distanceFactor = 100.0f;
    public float minSize = 0.1f;
    public float maxSize = 2.0f;

    Camera mainCam;
    Transform savedParent;

    TextMesh tm;
    Renderer rd;
    Transform bg;

    void Awake () {
        savedParent = transform.parent;
        transform.parent = null;
        mainCam = Camera.main;
        transform.parent = savedParent;

        tm = GetComponent<TextMesh>();
        if (tm != null) {
            rd = GetComponent<Renderer>();
            bg = transform.GetChild(0);
        }
    }

    void Start() {
        if (tm == null) {
            tm = GetComponent<TextMesh>();
        }
        if (mainCam == null) {
            mainCam = Camera.main;
        }

        scaleCamDist();
        scaleBackground();
    }

    void Update () {
        if (mainCam == null) {
            mainCam = Camera.main;
        }

        scaleCamDist();

        // scaleBackground();


    }

    void scaleCamDist()
    {
        if (mainCam == null) {
            return;
        }
        float dist = Vector3.Distance(mainCam.transform.position, transform.position);
        float scaling = dist / distanceFactor;
        Vector3 newScale = Vector3.one * scaling;

        transform.localScale = Vector3.one;

        float clampedScale = Mathf.Clamp(scaling, minSize * transform.lossyScale.x, maxSize * transform.lossyScale.x);

        transform.localScale = Vector3.one * clampedScale / transform.lossyScale.x;

        // savedParent = transform.parent;
        // transform.parent = null;
        // transform.localScale = newScale;
        // transform.parent = savedParent;
    }

    void scaleBackground() {
        if (rd == null) {
            if (tm != null) {
                rd = GetComponent<Renderer>();
                bg = transform.GetChild(0);
            }
        }

        if (rd != null) {

            Quaternion tmpRot = rd.gameObject.transform.rotation;
            rd.gameObject.transform.rotation = Quaternion.identity;

            float sx = 0.0f;
            float sy = 0.0f;

            sy = rd.bounds.size.y;
            sx = Mathf.Max(sy * 3.33f, rd.bounds.size.x);
            sy = Mathf.Clamp(sy, sx / 3.33f, rd.bounds.size.y);

            Vector3 nsc = new Vector3(sx, sy, 0.0f) * 1.5f;

            float zscale = bg.localScale.z;
            bg.localScale = new Vector3(nsc.x / transform.lossyScale.x, nsc.y / transform.lossyScale.x, zscale);

            rd.gameObject.transform.rotation = tmpRot;

        }
    }

}
