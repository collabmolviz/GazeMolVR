using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class SphereAnnotation : UnityMolAnnotation {


    public override void Create() {

        if (atoms == null || atoms.Count != 1) {
            Debug.LogError("Could not create SphereAnnotation, 'atoms' list is not correctly set");
            return;
        }


        GameObject haloGo = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/SphereOverAtom"));
        haloGo.transform.GetChild(0).gameObject.SetActive(false);//Disable text


        haloGo.layer = LayerMask.NameToLayer("Ignore Raycast");
        haloGo.SetActive(true);

        haloGo.transform.parent = annoParent;
        haloGo.transform.localPosition = Vector3.zero;
        haloGo.transform.rotation = Quaternion.identity;//Keep global rotation
        haloGo.transform.localScale =  Vector3.one * atoms[0].radius * 1.1f;

        go = haloGo;

    }
    public override void Update(){
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
        fillSerializedAtoms(san);
        return san;
    }
    public override int toAnnoType(){
        return 11;
    }
}
}