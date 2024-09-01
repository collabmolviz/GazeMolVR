using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class AngleAnnotation : UnityMolAnnotation {

    public Color colorText = new Color(0.0f, 0.0f, 0.5f, 1.0f);
    Vector3 p1;
    Vector3 p2;
    Vector3 p3;


    public override void Create() {

        if (atoms == null || atoms.Count != 3) {
            Debug.LogError("Could not create AngleAnnotation, 'atoms' list is not correctly set");
            return;
        }

        p1 = atoms[0].position;
        p2 = atoms[1].position;
        p3 = atoms[2].position;

        float angle = Vector3.Angle(atoms[0].position - atoms[1].position, atoms[2].position - atoms[1].position);

        Vector3 posA1 = annoParent.parent.TransformPoint(atoms[0].position);
        Vector3 posA2 = annoParent.parent.TransformPoint(atoms[1].position);
        Vector3 posA3 = annoParent.parent.TransformPoint(atoms[2].position);
        Vector3 mid = (posA1 + posA3) * 0.5f;

        Vector3 pos = posA2 + (mid - posA2) * 0.35f;

        string text = angle.ToString("F1") + "°";


        GameObject textObj = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/SphereOverAtom"));
        textObj.name = "Angle";
        textObj.GetComponent<MeshRenderer>().enabled = false;
        TextMesh textm = textObj.GetComponentsInChildren<TextMesh>()[0];
        textm.gameObject.GetComponent<MeshRenderer>().sortingOrder = 50;

        textm.text = text;

        textObj.transform.parent = annoParent;
        textObj.transform.position = pos;
        textObj.transform.localScale = Vector3.one * 1.5f;

        textm.color = colorText;


        // AnimateAnnotation anim = textObj.AddComponent<AnimateAnnotation>();
        // anim.a1 = a1;
        // anim.a2 = a2;
        // anim.a3 = a3;

        // anim.atomT = par;
        // anim.angleLine = line;
        // anim.mode = AnimateAnnotation.AnimationMode.angle;
        // anim.updateRotation = true;

        // addToDic(a3, textObj);
        go = textObj;
        Debug.Log("Angle between " + atoms[0] + " & " + atoms[1] + " & " + atoms[2] + " : " + text);
    }
    public override void Update() {
        if (p1 != atoms[0].position || p2 != atoms[1].position || p3 != atoms[2].position) {

            p1 = atoms[0].position;
            p2 = atoms[1].position;
            p3 = atoms[2].position;

            float angle = Vector3.Angle(atoms[0].position - atoms[1].position, atoms[2].position - atoms[1].position);

            Vector3 posA1 = annoParent.parent.TransformPoint(atoms[0].position);
            Vector3 posA2 = annoParent.parent.TransformPoint(atoms[1].position);
            Vector3 posA3 = annoParent.parent.TransformPoint(atoms[2].position);
            Vector3 mid = (posA1 + posA3) * 0.5f;

            Vector3 pos = posA2 + (mid - posA2) * 0.35f;

            string text = angle.ToString("F1") + "°";


            TextMesh textm = go.GetComponentsInChildren<TextMesh>()[0];
            textm.gameObject.GetComponent<MeshRenderer>().sortingOrder = 50;

            textm.text = text;

            go.transform.position = pos;

            textm.color = colorText;
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
        san.color = colorText;
        fillSerializedAtoms(san);
        return san;
    }

    public override int toAnnoType(){
        return 0;
    }
}
}