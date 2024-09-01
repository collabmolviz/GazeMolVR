using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class CustomSphereAnnotation : UnityMolAnnotation {

    public float scale = 1.1f;

    public override void Create() {

        GameObject haloGo = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/SphereOverAtom"));
        haloGo.transform.GetChild(0).gameObject.SetActive(false);//Disable text

        haloGo.layer = LayerMask.NameToLayer("Ignore Raycast");
        haloGo.SetActive(true);

        haloGo.transform.parent = annoParent;
        haloGo.transform.localPosition = Vector3.zero;
        haloGo.transform.rotation = Quaternion.identity;//Keep global rotation
        haloGo.transform.localScale =  Vector3.one * scale;

        go = haloGo;

    }
    public override void Update() {
    }
    public override void UnityUpdate() {
    }
    public override void Delete() {
        if (go != null && go.transform.parent != null) {
            GameObject.Destroy(go.transform.parent.gameObject);
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
        san.positions = new List<Vector3>(1);
        san.positions.Add(annoParent.position);
        san.size = scale;
        fillSerializedAtoms(san);
        return san;
    }
    public override int toAnnoType(){
        return 5;
    }
}
}