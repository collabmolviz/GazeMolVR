using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UMol {

public abstract class AtomRepresentation {

    public colorType colorationType = colorType.atom;
    public UnityMolSelection selection;
    public int nbAtoms = 0;
    public Transform representationParent;
    public Transform representationTransform;
    public Color bfactorStartCol;
    public Color bfactorEndCol;
    public Color bfactorMidColor;

    public int idFrame;

    public AtomRepresentation(UnityMolSelection sel){
        selection = sel;
    }
    public AtomRepresentation(){}

    public abstract void Clean();
}
}