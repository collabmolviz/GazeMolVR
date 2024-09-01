using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class CustomTextAnnotation : UnityMolAnnotation {

    public string content;
    public float scale;
    public Color colorText = new Color(0.0f, 0.0f, 0.5f, 1.0f);

    public override void Create() {

        GameObject textObj = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/SphereOverAtom"));
        textObj.name = "TextAnnotation";
        textObj.GetComponent<MeshRenderer>().enabled = false;
        TextMesh textm = textObj.GetComponentsInChildren<TextMesh>()[0];
        textm.gameObject.GetComponent<MeshRenderer>().sortingOrder = 50;

        textm.text = content;

        textObj.transform.parent = annoParent;
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localScale = Vector3.one * scale;
        textObj.transform.localRotation = Quaternion.identity;

        textObj.GetComponent<LookAtCamera>().onlyAlignVectors = true;

        textm.color = colorText;
        go = textObj;

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
        san.size = scale;
        san.color = colorText;
        san.content = content;
        san.positions = new List<Vector3>(1);
        san.positions.Add(annoParent.position);
        fillSerializedAtoms(san);
        return san;
    }
    public override int toAnnoType() {
        return 6;
    }
}
}