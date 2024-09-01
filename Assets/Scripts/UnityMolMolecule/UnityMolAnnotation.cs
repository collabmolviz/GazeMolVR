using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public abstract class UnityMolAnnotation {

    public GameObject go;
    public bool isShown = true;
    public Transform annoParent;
    public List<UnityMolAtom> atoms = new List<UnityMolAtom>();

    public UnityMolAnnotation() {}

    public abstract void Create();
    public abstract void Update();
    public abstract void UnityUpdate();
    public abstract void Delete();
    public abstract void Show(bool show);
    public abstract SerializedAnnotation Serialize();
    public abstract int toAnnoType();

    public void fillSerializedAtoms(SerializedAnnotation san) {
        san.annoType = toAnnoType();
        if (atoms == null || atoms.Count == 0)
            return;
        san.structureIds = new List<int>(atoms.Count);
        san.atomIds = new List<int>(atoms.Count);
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        for (int i = 0; i < atoms.Count; i++) {
            san.structureIds.Add(-1);
            san.atomIds.Add(-1);
        }

        for (int i = 0; i < sm.loadedStructures.Count; i++) {
            for (int a = 0; a < atoms.Count; a++) {
                if (sm.loadedStructures[i] == atoms[a].residue.chain.model.structure) {
                    san.structureIds[a] = i;
                    san.atomIds[a] = atoms[a].idInAllAtoms;
                }
            }
        }
    }
}
}