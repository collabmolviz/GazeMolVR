using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class ArrowAnnotation : UnityMolAnnotation {

    public Color colorLine = new Color(0.0f, 0.0f, 0.5f, 1.0f);

    Vector3 p1;
    Vector3 p2;

    public override void Create() {

        if (atoms == null || atoms.Count != 2) {
            Debug.LogError("Could not create ArrowAnnotation, 'atoms' list is not correctly set");
            return;
        }

        p1 = atoms[0].position;
        p2 = atoms[1].position;

        GameObject arrowObject = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/ArrowCircleSimple"));
        arrowObject.transform.parent = annoParent;

        Vector3 posA2 = annoParent.parent.TransformPoint(p1);
        Vector3 posA3 = annoParent.parent.TransformPoint(p2);

        Vector3 mid = (posA2 + posA3) * 0.5f;

        arrowObject.transform.position = mid;
        arrowObject.transform.rotation = Quaternion.FromToRotation(arrowObject.transform.up, (posA3 - posA2).normalized);

        arrowObject.transform.localScale = Vector3.one * 0.01f;

        AnimateTorsionAngle anim = arrowObject.AddComponent<AnimateTorsionAngle>();
        anim.t1 = UnityMolMain.getAnnotationManager().getGO(atoms[0]).transform;
        anim.t2 = UnityMolMain.getAnnotationManager().getGO(atoms[1]).transform;

        arrowObject.GetComponentsInChildren<MeshRenderer>()[0].material.color = colorLine;

        // addToDic(a4, arrowObject);
        go = arrowObject;
    }
    public override void Update() {
        if (p1 != atoms[0].position || p2 != atoms[1].position) {

            p1 = atoms[0].position;
            p2 = atoms[1].position;

            Vector3 posA2 = annoParent.parent.TransformPoint(p1);
            Vector3 posA3 = annoParent.parent.TransformPoint(p2);

            Vector3 mid = (posA2 + posA3) * 0.5f;

            go.transform.position = mid;
            go.transform.rotation = Quaternion.FromToRotation(go.transform.up, (posA3 - posA2).normalized);

            go.GetComponentsInChildren<MeshRenderer>()[0].material.color = colorLine;

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
        fillSerializedAtoms(san);
        return san;
    }
    public override int toAnnoType(){
        return 3;
    }
}
}