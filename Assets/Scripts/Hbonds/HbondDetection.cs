using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Unity.Collections;
using Unity.Mathematics;
using KNN;
using KNN.Jobs;
using Unity.Jobs;

namespace UMol {
public class HbondDetection {

    public static float neighboorDistance = 4.0f;//4 Angstrom
    public static float limitDistanceHA = 2.8f;
    public static float limitAngleDHA = 120.0f;
    public static float limitAngleHAB = 90.0f;

    public static List<string> listAcceptors = new List<string> {"O", "N"};
    public static Dictionary<string, int> maxBonded = new Dictionary<string, int> { {"O", 2}, {"N", 3}};

    public static UnityMolBonds DetectHydrogenBonds(UnityMolSelection selec, int idF, Dictionary<UnityMolAtom, int> atomToIdInSel) {

        UnityMolBonds customChemBonds = getCustomChemBonds(selec);
        if (customChemBonds.Count != 0) {
            return customChemBonds;
        }
        // return DetectHydrogenBonds_Avogadro(selec, idF, atomToIdInSel);
        return DetectHydrogenBonds_Shrodinger(selec, idF, atomToIdInSel);
    }

    public static UnityMolBonds getCustomChemBonds(UnityMolSelection selec) {
        UnityMolBonds bonds = new UnityMolBonds();

        HashSet<UnityMolModel> doneModel = new HashSet<UnityMolModel>();

        foreach (UnityMolAtom a in selec.atoms) {
            UnityMolModel curModel = a.residue.chain.model;
            if (curModel.customChemBonds.Count != 0) {
                foreach (int2 pair in curModel.customChemBonds) {
                    UnityMolAtom a1 = curModel.getAtomWithID(pair.x);
                    UnityMolAtom a2 = curModel.getAtomWithID(pair.y);
                    if (a1 != null && a2 != null) {
                        bonds.Add(a1, a2);
                    }
                    else if (a1 == null) {
                        Debug.LogWarning("Could not find atom with id " + pair.x + " in " + curModel.structure);
                    }
                    else if (a2 == null) {
                        Debug.LogWarning("Could not find atom with id " + pair.y + " in " + curModel.structure);
                    }
                }
                doneModel.Add(curModel);
            }
        }
        return bonds;
    }

    public static UnityMolBonds DetectHydrogenBonds_Avogadro(UnityMolSelection selec, int idFrame, Dictionary<UnityMolAtom, int> atomToIdInSel) {
        //From https://github.com/cryos/avogadro/blob/2a98712f24506d023aa4b84c984136a913017e81/libavogadro/src/protein.cpp
        UnityMolBonds bonds = new UnityMolBonds();

        if (selec.atoms.Count == 0) {
            return bonds;
        }

        Transform loadedMol = UnityMolMain.getRepresentationParent().transform;
        Transform sParent = UnityMolMain.getStructureManager().GetStructureGameObject(
                                selec.structures[0].name).transform;


        var allAtoms = new NativeArray<float3>(selec.atoms.Count, Allocator.Persistent);

        int id = 0;
        //Transform points in the loadedMolecules transform space
        foreach (UnityMolAtom a in selec.atoms) {
            allAtoms[id] = loadedMol.InverseTransformPoint(a.curWorldPosition);
            if (selec.extractTrajFrame) {
                Vector3 wPos = sParent.TransformPoint(selec.extractTrajFramePositions[idFrame][id]);
                allAtoms[id] = loadedMol.InverseTransformPoint(wPos);
            }
            id++;
        }

        //Arbitrary number that should be enough to find all atoms in the neighboorDistance
        int kNeighbours = 15;

        //Create KDTree
        KnnContainer knnContainer = new KnnContainer(allAtoms, false, Allocator.TempJob);
        NativeArray<float3> queryPositions = new NativeArray<float3>(allAtoms.Length, Allocator.TempJob);
        NativeArray<int> results = new NativeArray<int>(selec.atoms.Count * kNeighbours, Allocator.TempJob);

        var rebuildJob = new KnnRebuildJob(knnContainer);
        rebuildJob.Schedule().Complete();

        var batchQueryJob = new QueryKNearestBatchJob(knnContainer, queryPositions, results);
        batchQueryJob.ScheduleBatch(queryPositions.Length, queryPositions.Length / 32).Complete();

        int idA = 0;
        foreach (UnityMolAtom a1 in selec.atoms) {

            for (int i = 0; i < kNeighbours; i++) { //For all neighboors
                int idAtom = results[idA * kNeighbours + i];

                if (idAtom < 0 || idAtom >= selec.atoms.Count) {
                    continue;
                }

                UnityMolAtom a2 = selec.atoms[idAtom];

                UnityMolResidue res1 = a1.residue;
                UnityMolResidue res2 = a2.residue;

                if (res1 == res2) {
                    continue;
                }
                if (Mathf.Abs(res2.id - res1.id) <= 2) {
                    continue;
                }

                // residue 1 has the N-H
                // residue 2 has the C=O
                if (a1.type != "O") {
                    if (a2.type != "O") {
                        continue;
                    }
                }
                else {
                    res1 = a2.residue;
                    res2 = a1.residue;
                }

                if (!res1.atoms.ContainsKey("N")) {
                    continue;
                }
                if (!res2.atoms.ContainsKey("C") || !res2.atoms.ContainsKey("O")) {
                    continue;
                }

                UnityMolModel curM = res1.chain.model;

                UnityMolAtom N = res1.atoms["N"];
                UnityMolAtom H = null;
                UnityMolAtom C = res2.atoms["C"];
                UnityMolAtom O = res2.atoms["O"];

                if (!selec.bonds.bonds.ContainsKey(N.idInAllAtoms)) {
                    Debug.Log("Nothing bonded to " + N);
                    continue;
                }
                int[] bondedN = selec.bonds.bonds[N.idInAllAtoms];

                Vector3 Npos = N.position;
                Vector3 Opos = O.position;
                Vector3 Cpos = C.position;
                if (idFrame != -1) {
                    Npos = selec.extractTrajFramePositions[idFrame][atomToIdInSel[N]];
                    Opos = selec.extractTrajFramePositions[idFrame][atomToIdInSel[O]];
                    Cpos = selec.extractTrajFramePositions[idFrame][atomToIdInSel[C]];
                }


                Vector3 posH = Vector3.zero;
                foreach (int ibN in bondedN) { //For all neighboors of N
                    if (ibN != -1) {
                        UnityMolAtom bN = curM.allAtoms[ibN];
                        Vector3 bNpos = bN.position;
                        if (idFrame != -1) {
                            bNpos = selec.extractTrajFramePositions[idFrame][atomToIdInSel[bN]];
                        }

                        if (bN.type == "H") {
                            posH = bNpos;
                            H = bN;
                            break;
                        }
                        else { //Average of bonded atoms to compute H position
                            posH += Npos - bNpos;
                        }
                    }
                }
                if (H == null) { //H was not find => compute position
                    posH = Npos + (1.1f * posH);
                }


                //  C=O ~ H-N
                //
                //  C +0.42e   O -0.42e
                //  H +0.20e   N -0.20e
                float rON = (Opos - Npos).magnitude;
                float rCH = (Cpos - posH).magnitude;
                float rOH = (Opos - posH).magnitude;
                float rCN = (C.position - Npos).magnitude;

                float eON = 332 * (-0.42f * -0.20f) / rON;
                float eCH = 332 * ( 0.42f *  0.20f) / rCH;
                float eOH = 332 * (-0.42f *  0.20f) / rOH;
                float eCN = 332 * ( 0.42f * -0.20f) / rCN;
                float E = eON + eCH + eOH + eCN;

                if (E >= -0.5f)
                    continue;

                if (H == null) {
                    bonds.Add(O, N);
                }
                else {
                    bonds.Add(O, H);
                }
            }
            idA++;
        }

        knnContainer.Dispose();
        queryPositions.Dispose();
        results.Dispose();
        allAtoms.Dispose();

        // Vector3[] points = new Vector3[selec.atoms.Count];
        // int id = 0;
        // //Transform points in the framework of the loadedMolecules transform
        // foreach (UnityMolAtom a in selec.atoms) {
        //     points[id++] = loadedMol.InverseTransformPoint(a.curWorldPosition);
        // }

        // KDTree tmpKDTree = KDTree.MakeFromPoints(points);


        // foreach (UnityMolAtom a1 in selec.atoms) {

        //     int[] ids = tmpKDTree.FindNearestsRadius(loadedMol.InverseTransformPoint(a1.curWorldPosition) , neighboorDistance);

        //     for (int i = 0; i < ids.Length; i++) { //For all neighboors
        //         UnityMolAtom a2 = selec.atoms[ids[i]];

        //         UnityMolResidue res1 = a1.residue;
        //         UnityMolResidue res2 = a2.residue;

        //         if (res1 == res2) {
        //             continue;
        //         }
        //         if (Mathf.Abs(res2.id - res1.id) <= 2) {
        //             continue;
        //         }

        //         // residue 1 has the N-H
        //         // residue 2 has the C=O
        //         if (a1.type != "O") {
        //             if (a2.type != "O") {
        //                 continue;
        //             }
        //         }
        //         else {
        //             res1 = a2.residue;
        //             res2 = a1.residue;
        //         }

        //         if (!res1.atoms.ContainsKey("N")) {
        //             continue;
        //         }
        //         if (!res2.atoms.ContainsKey("C") || !res2.atoms.ContainsKey("O")) {
        //             continue;
        //         }

        //         UnityMolAtom N = res1.atoms["N"];
        //         UnityMolAtom H = null;
        //         UnityMolAtom C = res2.atoms["C"];
        //         UnityMolAtom O = res2.atoms["O"];

        //         if (!selec.bonds.bondsDual.ContainsKey(N)) {
        //             Debug.Log("Nothing bonded to " + N);
        //             if (selec.bonds.bonds.ContainsKey(N)) {
        //                 for (int d = 0; d < selec.bonds.bonds[N].Length; d++) {
        //                     if (selec.bonds.bonds[N][d] != null) {
        //                         Debug.Log(selec.bonds.bonds[N][d]);
        //                     }
        //                 }
        //             }
        //             continue;
        //         }
        //         UnityMolAtom[] bondedN = selec.bonds.bondsDual[N];

        //         Vector3 posH = Vector3.zero;
        //         foreach (UnityMolAtom bN in bondedN) { //For all neighboors of N
        //             if (bN != null) {
        //                 if (bN.type == "H") {
        //                     posH = bN.position;
        //                     H = bN;
        //                     break;
        //                 }
        //                 else { //Average of bonded atoms to compute H position
        //                     posH += N.position - bN.position;
        //                 }
        //             }
        //         }
        //         if (H == null) { //H was not find => compute position
        //             posH = N.position + (1.1f * posH);
        //         }


        //         //  C=O ~ H-N
        //         //
        //         //  C +0.42e   O -0.42e
        //         //  H +0.20e   N -0.20e
        //         float rON = (O.position - N.position).magnitude;
        //         float rCH = (C.position - posH).magnitude;
        //         float rOH = (O.position - posH).magnitude;
        //         float rCN = (C.position - N.position).magnitude;

        //         float eON = 332 * (-0.42f * -0.20f) / rON;
        //         float eCH = 332 * ( 0.42f *  0.20f) / rCH;
        //         float eOH = 332 * (-0.42f *  0.20f) / rOH;
        //         float eCN = 332 * ( 0.42f * -0.20f) / rCN;
        //         float E = eON + eCH + eOH + eCN;

        //         if (E >= -0.5f)
        //             continue;

        //         if (H == null) {
        //             bonds.Add(O, N);
        //         }
        //         else {
        //             bonds.Add(O, H);
        //         }
        //     }
        // }


        return bonds;
    }

    public static UnityMolBonds DetectHydrogenBonds_Shrodinger(UnityMolSelection selec, int idFrame,
            Dictionary<UnityMolAtom, int> atomToIdInSel) {

        UnityMolBonds bonds = new UnityMolBonds();
        if (selec.atoms.Count == 0) {
            return bonds;
        }
        if (selec.structures.Count != 1) {
            Debug.LogWarning("Cannot compute hbonds for global selections");
            return bonds;
        }

        List<UnityMolAtom> acceptors = getAcceptors(selec);

        if (acceptors.Count == 0) {
            return bonds;
        }

        if(idFrame != -1) {
//Implement this  !!!!!
            return bonds;
        }


        int kNei = (int)((Mathf.PI * 4 * limitDistanceHA * limitDistanceHA) * 0.3f);//Assuming 0.3 atom per Angstrom^3

        UnityMolStructure s = selec.structures[0];

        NativeArray<int> results = s.spatialSearch.SearchWithin(acceptors, limitDistanceHA, kNei);

        int idA = 0;
        foreach (UnityMolAtom acc in acceptors) {
            for (int i = 0; i < kNei; i++) { //For all neighboors
                int id = results[idA * kNei + i];
                if (id >= 0) {
                    UnityMolAtom n = s.currentModel.allAtoms[id];
                    if (n.type == "H" && selec.atoms.Contains(n)) {
                        Vector3 npos = n.position;

                        float distance = Vector3.Distance(acc.position, npos);

                        if (distance < limitDistanceHA) {
                            UnityMolAtom donor = getBonded(n, selec.bonds);

                            if (donor == null) {
                                continue;
                            }

                            Vector3 donorpos = donor.position;

                            float DHA_angle = Vector3.Angle(donorpos - npos, acc.position - npos);

                            if (DHA_angle > limitAngleDHA) {
                                UnityMolAtom bondedToAcceptor = getBonded(acc, selec.bonds);

                                if (bondedToAcceptor == null) {
                                    continue;
                                }
                                Vector3 bondedToAcceptorpos = bondedToAcceptor.position;

                                float HAB_angle = Vector3.Angle(npos - acc.position, bondedToAcceptorpos -  acc.position);
                                if (HAB_angle > limitAngleHAB) {
                                    bonds.Add(n, acc);
                                }
                            }
                        }
                    }
                }
            }
            idA++;
        }

        results.Dispose();

        return bonds;
    }

    static List<UnityMolAtom> getAcceptors(UnityMolSelection selec) {
        List<UnityMolAtom> result = new List<UnityMolAtom>();

        foreach (UnityMolAtom a in selec.atoms) {
            if (isAcceptor(a)) {
                int nbBonded = selec.bonds.countBondedAtoms(a);

                if (nbBonded > 0 && nbBonded < maxBonded[a.type]) {
                    if (a.type == "O" && !linkedToH(a, selec)) {
                        result.Add(a);
                    }
                    else if (a.type == "N") {
                        result.Add(a);
                    }
                }
            }
        }
        return result;
    }
    static bool linkedToH(UnityMolAtom a, UnityMolSelection selec) {
        int[] bonded = null;
        if (selec.bonds.bonds.TryGetValue(a.idInAllAtoms, out bonded)) {
            for (int i = 0; i < bonded.Length; i++) {
                if (bonded[i] != -1 && a.residue.chain.model.allAtoms[bonded[i]].type == "H") {
                    return true;
                }
            }
        }
        return false;
    }

    static bool isAcceptor(UnityMolAtom atom) {
        return listAcceptors.Contains(atom.type);
    }

//Returns first atom bonded to atom
    static UnityMolAtom getBonded(UnityMolAtom a, UnityMolBonds bonds) {
        int[] res;
        if (bonds.bonds.TryGetValue(a.idInAllAtoms, out res)) {
            if (res[0] != -1) {
                return a.residue.chain.model.allAtoms[res[0]];
            }
        }
        // Debug.LogWarning("Did not find atom "+a+" in bonds");
        return null;

    }
}
}