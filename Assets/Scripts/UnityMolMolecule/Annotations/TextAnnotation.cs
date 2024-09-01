using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class TextAnnotation : UnityMolAnnotation {

    public bool showLine = true;
    public string content;
    public Color colorText = new Color(0.0f, 0.0f, 0.5f, 1.0f);

    public float lineWidth = 0.1f;
    public float textDistToAtom = 2.0f;

    GameObject linkedLine;
    Transform annoBG;

    public override void Create() {

        if (atoms == null || atoms.Count != 1) {
            Debug.LogError("Could not create TextAnnotation, 'atoms' list is not correctly set");
            return;
        }

        GameObject textObj = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/SphereOverAtom"));
        textObj.name = "TextAnnotation";
        textObj.GetComponent<MeshRenderer>().enabled = false;
        TextMesh textm = textObj.GetComponentsInChildren<TextMesh>()[0];
        textm.gameObject.GetComponent<MeshRenderer>().sortingOrder = 50;

        textm.text = content;

        textObj.transform.parent = annoParent;
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localScale = new Vector3(1.5f, 1.5f, 0.01f);

        textm.color = colorText;
        go = textObj;

        if (showLine) {
            annoBG = go.transform.Find("Text/BG");
            annoBG.parent.localPosition = -annoBG.parent.up * textDistToAtom;

            Renderer rd = annoBG.GetComponent<Renderer>();
            Vector3 topPos = rd.bounds.center + textObj.transform.up * rd.bounds.extents.y;

            linkedLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            linkedLine.name = "textToLabelLine";
            linkedLine.layer = annoBG.gameObject.layer;
            GameObject.Destroy(linkedLine.GetComponent<BoxCollider>());
            linkedLine.transform.parent = go.transform;

            Vector3 atomPos = atoms[0].curWorldPosition;
            Vector3 vec = atomPos - topPos;
            float dist = Vector3.Distance(atomPos, topPos);
            linkedLine.transform.localScale = new Vector3(lineWidth, lineWidth, dist / go.transform.lossyScale.x);
            linkedLine.transform.position = topPos + (vec * 0.5f);
            linkedLine.transform.LookAt(topPos);

            //Creates a new material instance
            linkedLine.GetComponent<MeshRenderer>().material.color = colorText;

        }

    }
    public override void Update() {
    }
    public override void UnityUpdate() {
        if (showLine) {

            Renderer rd = annoBG.GetComponent<Renderer>();
            Vector3 topPos = rd.bounds.center + go.transform.up * rd.bounds.extents.y;

            Vector3 atomPos = atoms[0].curWorldPosition;
            Vector3 vec = atomPos - topPos;
            float dist = Vector3.Distance(atomPos, topPos);
            linkedLine.transform.localScale = new Vector3(lineWidth, lineWidth, dist / go.transform.lossyScale.x);
            linkedLine.transform.position = topPos + (vec * 0.5f);
            linkedLine.transform.LookAt(topPos);

        }

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
        san.showLine = showLine;
        san.content = content;
        san.color = colorText;
        san.size = lineWidth;
        san.size2 = textDistToAtom;
        fillSerializedAtoms(san);
        return san;
    }
    public override int toAnnoType(){
        return 12;
    }
}
}