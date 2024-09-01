using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
/// <summary>
/// Part of the SMCRA data structure, UnityMolChain stores the residues of the structure as a list of UnityMolResidue
/// A reference to the model it belongs is provided
/// </summary>
public class UnityMolChain {

    /// <summary>
    /// Store all the residues of the chain based on their ids
    /// </summary>
    public List<UnityMolResidue> residues;

    /// <summary>
    /// Reference to the model the chain belongs to
    /// </summary>
    public UnityMolModel model;

    /// <summary>
    /// Name of the chain
    /// </summary>
    public string name;

    private int _count = -1;

    /// <summary>
    /// UnityMolChain constructor taking a list of residues as arg
    /// </summary>
    public UnityMolChain(List<UnityMolResidue> _residues, string _name) {
        residues = new List<UnityMolResidue>();
        AddResidues(_residues);
        name = _name;
    }

    /// <summary>
    /// UnityMolChain constructor taking a residue as arg
    /// </summary>
    public UnityMolChain(UnityMolResidue _residue, string _name) {
        residues = new List<UnityMolResidue>();
        residues.Add(_residue);
        name = _name;
    }

    public int Length {
        get {return Count;}
    }
    public int Count {
        get {
            if (_count < 0)
                GetCount();
            return _count;
        }
    }

    /// <summary>
    /// Add a list of residues to the stored residues
    /// </summary>
    public void AddResidues(List<UnityMolResidue> newResidues) {
        foreach (UnityMolResidue r in newResidues) {
            residues.Add(r);
        }
        _count = -1;
    }

    public void GetCount() {
        _count = 0;
        foreach (UnityMolResidue r in residues)
            _count += r.Count;
    }

    public List<UnityMolAtom> allAtoms {
        get { return ToAtomList(); }
    }

    public List<UnityMolAtom> ToAtomList() {
        List<UnityMolAtom> res = new List<UnityMolAtom>();

        foreach (UnityMolResidue r in residues) {
            // res.AddRange(r.allAtoms);
            foreach (UnityMolAtom a in r.atoms.Values) {
                res.Add(a);
            }
        }
        return res;
    }

    public UnityMolSelection ToSelection(bool doBonds = true) {
        List<UnityMolAtom> selectedAtoms = ToAtomList();
        string selectionMDA = ToSelectionMDA();

        if (doBonds) {
            return new UnityMolSelection(selectedAtoms, ToSelectionName(), selectionMDA);
        }
        return new UnityMolSelection(selectedAtoms, newBonds: null, ToSelectionName(), selectionMDA);
    }

    public string ToSelectionMDA() {
        return model.structure.name + " and chain " + name;
    }

    public string ToSelectionName() {
        return model.structure.name + "_" + model.name + "_" + name;
    }

    //TODO: make this faster
    public UnityMolResidue getResidueWithId(int id) {
        for (int i = 0; i < residues.Count; i++) {
            if (residues[i].id == id)
                return residues[i];
        }
        return null;
    }

    /// <summary>
    /// Clone a UnityMolChain by cloning residues and atoms
    /// </summary>
    public UnityMolChain Clone() {
        List<UnityMolResidue> clonedRes = new List<UnityMolResidue>(residues.Count);

        foreach (UnityMolResidue r in residues) {
            var newR = r.Clone();
            clonedRes.Add(newR);
        }

        UnityMolChain cloned = new UnityMolChain(clonedRes, name);

        foreach (UnityMolResidue r in cloned.residues) {
            r.chain = cloned;
        }

        return cloned;
    }

}
}