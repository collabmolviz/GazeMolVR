using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class DrawAnnotation : UnityMolAnnotation {

    public int id;//Unique id given by AnnotationManager
    public Color colorLine = Color.black;
    public float sizeLine = 0.005f;
    public List<Vector3> positions = new List<Vector3>();

    public override void Create() {
        if (atoms == null || atoms.Count != 1) {
            Debug.LogError("Could not create DrawAnnotation, 'atoms' list is not correctly set");
            return;
        }

        GameObject lineObject = new GameObject("drawLine");
        lineObject.transform.parent = annoParent;
        lineObject.transform.localPosition = Vector3.zero;
        lineObject.transform.localScale = Vector3.one;
        lineObject.transform.localRotation = Quaternion.identity;

        lineObject.AddComponent<MeshFilter>();
        MeshRenderer mr = lineObject.AddComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = colorLine;
        mr.sharedMaterial = mat;

        MeshLineRenderer lr = lineObject.AddComponent<MeshLineRenderer>();
        lr.Init();
        lr.setWidth(sizeLine);

        for (int i = 0; i < positions.Count; i++) {
            lr.AddPoint(positions[i]);
        }

        go = lineObject;
    }
    public override void Update() {

    }
    public override void Delete() {
        if (go != null) {
            GameObject.Destroy(go.GetComponent<MeshRenderer>().sharedMaterial);
            GameObject.Destroy(go);
        }
        if (positions != null) {
            positions.Clear();
        }
    }
    public override void UnityUpdate() {
    }

    public override void Show(bool show = true) {
        if (go != null) {
            isShown = show;
            go.SetActive(show);
        }
    }

    public override SerializedAnnotation Serialize() {
        SerializedAnnotation san = new SerializedAnnotation();
        san.color = colorLine;
        san.id = id;
        san.size = sizeLine;
        san.positions = new List<Vector3>(positions.Count);
        san.positions.AddRange(positions);
        fillSerializedAtoms(san);
        return san;
    }
    public override int toAnnoType() {
        return 8;
    }
}
}