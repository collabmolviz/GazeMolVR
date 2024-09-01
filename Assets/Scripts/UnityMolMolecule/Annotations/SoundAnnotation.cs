using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class SoundAnnotation : UnityMolAnnotation {


    public override void Create() {

    }
    public override void Update() {

    }
    public override void Delete() {
        if (go != null) {
            GameObject.Destroy(go);
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
        fillSerializedAtoms(san);
        return san;
    }
    public override int toAnnoType(){
        return 10;
    }
}
}