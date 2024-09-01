using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class CustomLineAnnotation : UnityMolAnnotation {

    public float sizeLine = 0.005f;
    public Color colorLine = new Color(0.0f, 0.0f, 0.5f, 1.0f);
    public Vector3 start;
    public Vector3 end;

    public override void Create() {

        GameObject lineObject = new GameObject("worldLine");
        lineObject.transform.parent = annoParent;
        lineObject.transform.localPosition = Vector3.zero;
        lineObject.transform.localScale = Vector3.one;
        lineObject.transform.localRotation = Quaternion.identity;

        LineRenderer curLine = lineObject.AddComponent<LineRenderer>();
        curLine.useWorldSpace = false;
        curLine.positionCount = 2;          // initialize to one line segment

        Shader lineShader = Shader.Find("Particles/Alpha Blended");
        if (lineShader == null)
            lineShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        curLine.material = new Material (lineShader);
        curLine.startColor = curLine.endColor = colorLine;
        curLine.startWidth = curLine.endWidth = sizeLine;
        curLine.alignment = LineAlignment.View;             // have line always face viewer

        curLine.SetPosition(0, start);
        curLine.SetPosition(1, end);

        go = lineObject;
    }
    public override void Update() {

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
        san.positions = new List<Vector3>(2);
        san.positions.Add(start);
        san.positions.Add(end);
        fillSerializedAtoms(san);
        return san;
    }
    public override int toAnnoType(){
        return 4;
    }
}
}
