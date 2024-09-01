using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class ArcLineAnnotation : UnityMolAnnotation {

    public Color colorLine = new Color(0.0f, 0.0f, 0.5f, 1.0f);
    public float sizeLine = 0.0025f;

    Vector3 p1;
    Vector3 p2;
    Vector3 p3;

    public override void Create() {

        if (atoms == null || atoms.Count != 3) {
            Debug.LogError("Could not create ArcLineAnnotation, 'atoms' list is not correctly set");
            return;
        }

        p1 = atoms[0].position;
        p2 = atoms[1].position;
        p3 = atoms[2].position;

        GameObject lineObject = new GameObject("angleLine");
        lineObject.transform.parent = annoParent;
        lineObject.transform.localRotation = Quaternion.identity;
        lineObject.transform.localPosition = Vector3.zero;
        lineObject.transform.localScale = Vector3.one;


        LineRenderer curLine = lineObject.AddComponent<LineRenderer>();
        ArcLine arc = lineObject.AddComponent<ArcLine>();
        curLine.useWorldSpace = false;

        Shader lineShader = Shader.Find("Particles/Alpha Blended");
        if (lineShader == null)
            lineShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        curLine.material = new Material (lineShader);
        curLine.startColor = curLine.endColor = colorLine;
        curLine.alignment = LineAlignment.View;             // have line always face viewer
        curLine.startWidth = curLine.endWidth = sizeLine;

        Vector3 posA1 = annoParent.parent.TransformPoint(p1);
        Vector3 posA2 = annoParent.parent.TransformPoint(p2);
        Vector3 posA3 = annoParent.parent.TransformPoint(p3);

        arc.A = posA1;
        arc.B = posA2;
        arc.C = posA3;

        arc.UpdatePointLine();

        go = lineObject;

    }
    public override void Update() {
        if (p1 != atoms[0].position || p2 != atoms[1].position || p3 != atoms[2].position) {

            p1 = atoms[0].position;
            p2 = atoms[1].position;
            p3 = atoms[2].position;

            LineRenderer curLine = go.GetComponent<LineRenderer>();
            ArcLine arc = go.GetComponent<ArcLine>();

            curLine.startColor = curLine.endColor = colorLine;
            curLine.startWidth = curLine.endWidth = sizeLine;

            Vector3 posA1 = annoParent.parent.TransformPoint(p1);
            Vector3 posA2 = annoParent.parent.TransformPoint(p2);
            Vector3 posA3 = annoParent.parent.TransformPoint(p3);

            arc.A = posA1;
            arc.B = posA2;
            arc.C = posA3;

            arc.UpdatePointLine();

        }
    }
    public override void UnityUpdate() {
    }
    public override void Delete() {
        if (go != null) {
            GameObject.Destroy(go);
        }
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
        san.size = sizeLine;
        fillSerializedAtoms(san);
        return san;
    }

    public override int toAnnoType(){
        return 2;
    }
}
}
