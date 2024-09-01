
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UMol {
public abstract class BondRepresentation {
	public UnityMolSelection selection;

    public colorType colorationType = colorType.atom;

    public Color bfactorStartCol;
    public Color bfactorEndCol;
    public Color bfactorMidColor;
    
    public Transform representationParent;
    public Transform representationTransform;
    public int nbBonds;
    public int idFrame;

    public BondRepresentation(UnityMolSelection sel){
        selection = sel;
    }
    public BondRepresentation(){}

    public abstract void Clean();
}
}