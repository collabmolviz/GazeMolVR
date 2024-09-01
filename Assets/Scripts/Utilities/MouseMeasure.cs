
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


using UMol.API;

namespace UMol {
[RequireComponent(typeof(ManipulationManager))]
public class MouseMeasure : MonoBehaviour {

    public KeyCode triggeringKey = KeyCode.M;
    private MeasureMode prevMeasureMode = MeasureMode.distance;
    int touchedAtoms = 0;

    UnityMolSelectionManager selM;
    UnityMolAnnotationManager annoM;
    CustomRaycastBurst raycaster;
    ManipulationManager mm;
    MouseOverSelection mos;

    Camera mainCam;

    UnityMolAtom[] atomsArray = new UnityMolAtom[4];
    Vector3[] posExtrArray = new Vector3[4];

    void Start() {

        touchedAtoms = 0;

        selM = UnityMolMain.getSelectionManager();
        annoM = UnityMolMain.getAnnotationManager();
        raycaster = UnityMolMain.getCustomRaycast();
        mm = APIPython.getManipulationManager();
        mainCam = Camera.main;
        mos = GetComponent<MouseOverSelection>();

    }

    void Update() {
        if (UnityMolMain.inVR())
            return;

        if (mainCam == null)
            mainCam = Camera.main;

        bool MPressed = Input.GetKey(triggeringKey);

        if (MPressed) {
            mos.tempDisable = true;
        }
        else if (!UnityMolMain.isDOFOn) {
            mos.tempDisable = false;
        }

        if (MPressed && Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) {
            doButtonPressed();
        }
    }



    void doButtonPressed() {
        if (mainCam == null)
            return;

        if (prevMeasureMode != UnityMolMain.measureMode) {
            prevMeasureMode = UnityMolMain.measureMode;
            resetTouchedAtoms();
        }

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        Vector3 p = Vector3.zero;
        bool isExtrAtom = false;
        UnityMolAtom a = raycaster.customRaycastAtomBurst(
                             ray.origin,
                             ray.direction,
                             ref p, ref isExtrAtom, false);

        if (a != null) {

            if (touchedAtoms == 4) {
                touchedAtoms = 0;
            }

            switch (UnityMolMain.measureMode) {
            case MeasureMode.distance:
                if (touchedAtoms >= 2)
                    touchedAtoms = 0;
                break;
            case MeasureMode.angle:
                if (touchedAtoms >= 3)
                    touchedAtoms = 0;
                break;
            case MeasureMode.torsAngle:
                if (touchedAtoms >= 4)
                    touchedAtoms = 0;
                break;
            }

            atomsArray[touchedAtoms] = a;
            posExtrArray[touchedAtoms] = p;


            if (atomsArray[touchedAtoms] == null) {
                Debug.LogError("Problem measuring atoms");
                resetTouchedAtoms();
                return;
            }

            //Touched the same atom = Stop measurements
            if (touchedAtoms > 0 && atomsArray[touchedAtoms - 1] == atomsArray[touchedAtoms]) {
                resetTouchedAtoms();
                return;
            }

            //Touched an atom from another molecule
            if (touchedAtoms >= 1) {
                bool sameStruc = true;
                string sName = atomsArray[0].residue.chain.model.structure.name;
                for (int i = 1; i <= touchedAtoms; i++) {
                    if (sName != atomsArray[i].residue.chain.model.structure.name) {
                        sameStruc = false;
                        break;
                    }
                }

                if (!sameStruc) {
                    Debug.LogWarning("No inter-molecule measurements allowed");
                    resetTouchedAtoms();
                    return;
                }
            }

            string s1Name = null;
            string s2Name = null;
            string s3Name = null;
            string s4Name = null;

            if (touchedAtoms == 0) {
                s1Name = atomsArray[0].residue.chain.model.structure.name;

                if (!isExtrAtom) {
                    APIPython.annotateAtom(s1Name, (int)atomsArray[0].number);
                }
                else {
                    Vector3 losS = atomsArray[0].residue.chain.model.structure.annotationParent.transform.lossyScale;
                    APIPython.annotateSphere(posExtrArray[0], losS.x);
                }
                touchedAtoms++;
                return;
            }

            switch (UnityMolMain.measureMode) {
            case MeasureMode.distance:
                if (touchedAtoms == 1) {
                    s1Name = atomsArray[0].residue.chain.model.structure.name;
                    s2Name = atomsArray[1].residue.chain.model.structure.name;

                    if (!isExtrAtom) {

                        APIPython.annotateLine(s1Name, (int)atomsArray[0].number,
                                               s2Name, (int)atomsArray[1].number);

                        APIPython.annotateDistance(s1Name, (int)atomsArray[0].number,
                                                   s2Name, (int)atomsArray[1].number);

                        APIPython.annotateAtom(s2Name, (int)atomsArray[1].number);
                    }
                    else {
                        Transform sPar = atomsArray[0].residue.chain.model.structure.annotationParent.transform.parent;
                        Vector3 a1pos = sPar.InverseTransformPoint(posExtrArray[0]);
                        Vector3 a2pos = sPar.InverseTransformPoint(posExtrArray[1]);

                        float dist = Vector3.Distance(a1pos, a2pos);

                        string distText = dist.ToString("F1") + "\u212B";
                        float sizeLine = 0.005f;//TODO: probably not the correct value => compute that
                        APIPython.annotateWorldLine(posExtrArray[0], posExtrArray[1],
                                                    sizeLine, new Color(0.0f, 0.0f, 0.5f, 1.0f));

                        float scaleText = 1.0f;//TODO: probably not the correct value => compute that
                        APIPython.annotateWorldText((posExtrArray[0] + posExtrArray[1]) * 0.5f,
                                                    scaleText, distText, new Color(0.0f, 0.0f, 0.5f, 1.0f));


                        Vector3 losS = atomsArray[1].residue.chain.model.structure.annotationParent.transform.lossyScale;

                        APIPython.annotateSphere(posExtrArray[1], losS.x);
                    }
                    resetTouchedAtoms();
                }
                break;
            case MeasureMode.angle:
                if (touchedAtoms == 1) {
                    s1Name = atomsArray[0].residue.chain.model.structure.name;
                    s2Name = atomsArray[1].residue.chain.model.structure.name;

                    APIPython.annotateLine(s1Name, (int)atomsArray[0].number,
                                           s2Name, (int)atomsArray[1].number);

                    APIPython.annotateAtom(s2Name, (int)atomsArray[1].number);
                    touchedAtoms++;
                    break;
                }
                if (touchedAtoms == 2) {
                    s1Name = atomsArray[0].residue.chain.model.structure.name;
                    s2Name = atomsArray[1].residue.chain.model.structure.name;
                    s3Name = atomsArray[2].residue.chain.model.structure.name;

                    APIPython.annotateAtom(s3Name, (int)atomsArray[2].number);

                    APIPython.annotateLine(s2Name, (int)atomsArray[1].number,
                                           s3Name, (int)atomsArray[2].number);

                    APIPython.annotateAngle(s1Name, (int)atomsArray[0].number,
                                            s2Name, (int)atomsArray[1].number,
                                            s3Name, (int)atomsArray[2].number);

                    APIPython.annotateArcLine(s1Name, (int)atomsArray[0].number,
                                              s2Name, (int)atomsArray[1].number,
                                              s3Name, (int)atomsArray[2].number);

                    resetTouchedAtoms();
                }
                break;
            case MeasureMode.torsAngle:

                if (touchedAtoms == 1) {
                    s1Name = atomsArray[0].residue.chain.model.structure.name;
                    s2Name = atomsArray[1].residue.chain.model.structure.name;

                    APIPython.annotateLine(s1Name, (int)atomsArray[0].number,
                                           s2Name, (int)atomsArray[1].number);

                    APIPython.annotateAtom(s2Name, (int)atomsArray[1].number);
                    touchedAtoms++;
                    break;
                }
                if (touchedAtoms == 2) {
                    s1Name = atomsArray[0].residue.chain.model.structure.name;
                    s2Name = atomsArray[1].residue.chain.model.structure.name;
                    s3Name = atomsArray[2].residue.chain.model.structure.name;

                    APIPython.annotateLine(s2Name, (int)atomsArray[1].number,
                                           s3Name, (int)atomsArray[2].number);

                    APIPython.annotateAtom(s3Name, (int)atomsArray[2].number);
                    touchedAtoms++;
                    break;
                }
                if (touchedAtoms == 3) {
                    s1Name = atomsArray[0].residue.chain.model.structure.name;
                    s2Name = atomsArray[1].residue.chain.model.structure.name;
                    s3Name = atomsArray[2].residue.chain.model.structure.name;
                    s4Name = atomsArray[3].residue.chain.model.structure.name;

                    APIPython.annotateAtom(s4Name, (int)atomsArray[3].number);

                    APIPython.annotateLine(s3Name, (int)atomsArray[2].number,
                                           s4Name, (int)atomsArray[3].number);
                    APIPython.annotateDihedralAngle(s1Name, (int)atomsArray[0].number,
                                                    s2Name, (int)atomsArray[1].number,
                                                    s3Name, (int)atomsArray[2].number,
                                                    s4Name, (int)atomsArray[3].number);

                    APIPython.annotateRotatingArrow(s2Name, (int)atomsArray[1].number,
                                                    s3Name, (int)atomsArray[2].number);
                    resetTouchedAtoms();
                }
                break;
            }
        }

    }

    public void resetTouchedAtoms() {
        touchedAtoms = 0;
    }
}
}