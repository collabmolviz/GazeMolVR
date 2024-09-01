using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UMol {

public class AnimateAnnotation : MonoBehaviour {

    public enum AnimationMode {
        line = 0,
        distance = 1,
        angle = 2,
        torsion = 3,
    }

    public bool updateRotation = false;

    public AnimationMode mode = AnimationMode.line;

    public Transform atomT;
    public LineRenderer distanceLine;
    // public CurvedLine angleLine;
    public ArcLine angleLine;
    public GameObject arrowsTorsion;


    public UnityMolAtom a1;
    public UnityMolAtom a2;
    public UnityMolAtom a3;
    public UnityMolAtom a4;

    private Camera mainCam;

    void Start() {
        mainCam = Camera.main;
    }
    void Update() {
        if (updateRotation) {
            if (mainCam != null) {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
            }
        }

        if (!shouldUpdate()) {
            return;
        }

        if (mode == AnimationMode.line){
            updateDistanceLine();
        }
        else if (mode == AnimationMode.distance) {
            updateDistanceText();
        }
        else if (mode == AnimationMode.angle) {
            updateAngleText();
            updateAngleLine();
        }
        else if (mode == AnimationMode.torsion) {
            updateTorsionText();
            updateTorsionArrows();
        }
    }

    bool shouldUpdate() {
        if (atomT != null && a1 != null && a2 != null) {
            UnityMolStructure s = a1.residue.chain.model.structure;
            return s.trajectoryLoaded;
        }
        return false;
    }

    void updateDistanceText() {
        if(GetComponent<TextMesh>() != null){
            float dist = Vector3.Distance(a1.position, a2.position);

            Vector3 tPos1 = atomT.parent.TransformPoint(a1.position);
            Vector3 tPos2 = atomT.parent.TransformPoint(a2.position);

            string text = dist.ToString("F1") + "\u212B";
            transform.position = (tPos1 + tPos2) * 0.5f;

            TextMesh textm = GetComponent<TextMesh>();
            textm.text = text;
        }
    }

    void updateDistanceLine() {
        if (distanceLine != null) {
            Vector3 transformedPosition1 = atomT.parent.TransformPoint(a1.position);
            Vector3 transformedPosition2 = atomT.parent.TransformPoint(a2.position);

            distanceLine.SetPosition(0, atomT.InverseTransformPoint(transformedPosition1));
            distanceLine.SetPosition(1, atomT.InverseTransformPoint(transformedPosition2));
        }
    }

    void updateAngleText() {
        if (a3 == null) {
            return;
        }
        float angle = Vector3.Angle(a1.position - a2.position, a3.position - a2.position);

        Vector3 posA1 = atomT.parent.TransformPoint(a1.position);
        Vector3 posA2 = atomT.parent.TransformPoint(a2.position);
        Vector3 posA3 = atomT.parent.TransformPoint(a3.position);
        Vector3 mid = (posA1 + posA3) * 0.5f;

        Vector3 pos = posA2 + (mid - posA2) * 0.35f;

        string text = angle.ToString("F1") + "°";

        transform.position = pos;
        TextMesh textm = transform.GetChild(0).gameObject.GetComponent<TextMesh>();
        textm.text = text;

    }
    void updateAngleLine() {
        if (angleLine != null) {

            Vector3 posA1 = atomT.parent.TransformPoint(a1.position);
            Vector3 posA2 = atomT.parent.TransformPoint(a2.position);
            Vector3 posA3 = atomT.parent.TransformPoint(a3.position);

            // Vector3 mid = (posA1 + posA3) * 0.5f;

            // Vector3[] positions = new Vector3[3];
            // positions[0] = atomT.InverseTransformPoint(posA2 + (posA1 - posA2) * 0.25f);
            // positions[1] = atomT.InverseTransformPoint(posA2 + (mid - posA2) * 0.35f);
            // positions[2] = atomT.InverseTransformPoint(posA2 + (posA3 - posA2) * 0.25f);

            // angleLine.linePositions = positions;

            angleLine.A = posA1;
            angleLine.B = posA2;
            angleLine.C = posA3;

            angleLine.UpdatePointLine();
        }

    }

    void updateTorsionText() {
        if (a4 == null) {
            return;
        }

        float dihe = UnityMolAnnotationManager.dihedral(a1.position, a2.position, a3.position, a4.position);

        Vector3 posA2 = atomT.parent.TransformPoint(a2.position);
        Vector3 posA3 = atomT.parent.TransformPoint(a3.position);

        Vector3 mid = (posA2 + posA3) * 0.5f;

        string text = dihe.ToString("F1") + "°";


        transform.position = mid;
        Vector3 tmpPos = transform.localPosition;
        tmpPos.y += 1.0f;
        transform.localPosition =  tmpPos;

        TextMesh textm = transform.GetChild(0).gameObject.GetComponent<TextMesh>();
        textm.text = text;
    }
    void updateTorsionArrows() {
        //Rotation of the arrows is done in AnimateTorsionAngle
        if (arrowsTorsion != null) {
            Vector3 posA2 = atomT.parent.TransformPoint(a2.position);
            Vector3 posA3 = atomT.parent.TransformPoint(a3.position);
            Vector3 mid = (posA2 + posA3) * 0.5f;

            arrowsTorsion.transform.position = mid;

        }
    }
}
}