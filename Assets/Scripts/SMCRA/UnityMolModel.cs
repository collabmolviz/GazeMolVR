using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace UMol {
/// <summary>
/// Part of the SMCRA data structure, UnityMolModel stores the chains of the structure
/// as a dictionary <string,UnityMolChain>.
/// It also stores the bonds as UnityMolBonds
/// A list of UnityMolAtom of the model is provided to loop over all atoms quickly
/// </summary>
public class UnityMolModel {
	/// <summary>
	/// Store all the chains of the model
	/// </summary>
	public UnityMolStructure structure;

	/// <summary>
	/// Store all the chains of the model
	/// </summary>
	public Dictionary<string, UnityMolChain> chains;

	/// <summary>
	/// Name of the model
	/// </summary>
	public string name;

	/// <summary>
	/// Bonds of the model, contains a dictionary of <UnityMolAtom, UnityMolAtom[]>
	/// </summary>
	public UnityMolBonds bonds;

	/// <summary>
	/// Saved bonds of the model
	/// </summary>
	public UnityMolBonds savedBonds;

	/// <summary>
	/// Bonds parsed with BondOrderParser, records only covalent bonds
	/// </summary>
	public Dictionary<AtomDuo, bondOrderType> covBondOrders;

	/// <summary>
	/// Stores a reference to all the atoms of the model
	/// </summary>
	public List<UnityMolAtom> allAtoms;

	private Vector3[] allPositions;

	/// <summary>
	/// Center of gravity of the model
	/// </summary>
	public Vector3 centroid;

	/// <summary>
	/// Maximum position in x, y and z
	/// </summary>
	public Vector3 maximumPositions;

	/// <summary>
	/// Minimum position in x, y and z
	/// </summary>
	public Vector3 minimumPositions;

	/// <summary>
	/// Custom chemical bonds read in a PDB file
	/// </summary>
	public List<int2> customChemBonds = new List<int2>();

	/// <summary>
	/// FieldLines Json file reader to be passed to UnityMolRepresentation
	/// </summary>
	public FieldLinesReader fieldLinesR;

	private Dictionary<long, int> _atomIdToIndex;
	public Dictionary<long, int> atomIdToIndex {
		get {
			if (_atomIdToIndex == null) {
				_atomIdToIndex = new Dictionary<long, int>(allAtoms.Count);
				for (int i = 0; i < allAtoms.Count; i++) {
					_atomIdToIndex[allAtoms[i].number] = i;
				}
			}
			return _atomIdToIndex;
		}
	}

	public int Count {
		get {return allAtoms.Count;}
	}

	/// <summary>
	/// UnityMolModel constructor taking chain dictionary as arg
	/// </summary>
	public UnityMolModel(Dictionary<string, UnityMolChain> dictChains, string nameModel) {
		chains = dictChains;
		name = nameModel;
		allAtoms = new List<UnityMolAtom>();
		foreach (UnityMolChain c in dictChains.Values) {
			c.model = this;
		}
	}

	/// <summary>
	/// UnityMolModel constructor taking chain list as arg,
	/// all the chains are inserted into the _chains dictionary
	/// </summary>
	public UnityMolModel(List<UnityMolChain> listChains, string nameModel) {

		chains = new Dictionary<string, UnityMolChain>();
		UnityMolChain outChain = null;
		for (int c = 0; c < listChains.Count; c++) {
			if (!chains.TryGetValue(listChains[c].name, out outChain)) {
				chains[listChains[c].name] = listChains[c];
			}
			else {
				outChain.AddResidues(listChains[c].residues);
			}
		}
		name = nameModel;
		allAtoms = new List<UnityMolAtom>();
		for (int c = 0; c < listChains.Count; c++) {
			listChains[c].model = this;
		}
		foreach (var c in chains.Values) {
			foreach (UnityMolResidue r in c.residues) {
				r.chain = c;
			}
		}
	}

	/// <summary>
	/// UnityMolModel constructor taking one chain as arg,
	/// the chain is inserted into the chains dictionary
	/// </summary>
	public UnityMolModel(UnityMolChain newChain, string nameModel) {
		chains = new Dictionary<string, UnityMolChain>();
		chains[newChain.name] = newChain;
		name = nameModel;
		allAtoms = new List<UnityMolAtom>();
		newChain.model = this;
	}

	/// <summary>
	/// Allocate the current list of chains
	/// </summary>
	public List<UnityMolChain> GetChains() {
		return chains.Values.ToList();
	}

	public void ComputeCentroid() {
		if (allAtoms.Count == 0) {
			centroid = Vector3.zero;
			return;
		}

		centroid = CenterOfGravBurst.computeCOG(allAtoms, ref minimumPositions, ref maximumPositions);
	}


	public static Vector3 ComputeCentroid(Vector3[] positions) {
		Vector3 dummymin = Vector3.zero;
		Vector3 dummymax = Vector3.zero;
		return CenterOfGravBurst.computeCOG(positions, ref dummymin, ref dummymax);
	}


	/// <summary>
	/// Fills UnityMol.position using UnityMolAtom.oriposition and the centroid computed with ComputeCentroid()
	/// </summary>
	public void CenterAtoms() {
		for (int i = 0; i < allAtoms.Count; i++) {
			allAtoms[i].position = allAtoms[i].oriPosition - centroid;
		}
	}

	/// <summary>
	/// Fills idInAllAtoms field
	/// </summary>
	public void fillIdAtoms() {
		for (int i = 0; i < allAtoms.Count; i++) {
			allAtoms[i].idInAllAtoms = i;
		}
		_atomIdToIndex = null;//Force update of atomId to index
	}

	public UnityMolAtom getAtomWithID(int idAtom) {
		int res = -1;
		if (atomIdToIndex.TryGetValue(idAtom, out res)) {
			return allAtoms[res];
		}
		return null;
	}

	/// Creates a new list of atoms
	public List<UnityMolAtom> ToAtomList() {
		return allAtoms.ToList();//Copy the list
	}


	public UnityMolSelection ToSelection() {
		List<UnityMolAtom> selectedAtoms = allAtoms;
		return new UnityMolSelection(selectedAtoms, bonds, ToSelectionName());
	}

	public string ToSelectionName() {
		return structure.name + "_" + name;
	}

	public bool hasHydrogens() {
		foreach (UnityMolAtom a in allAtoms) {
			if (a.type == "H") {
				return true;
			}
		}
		return false;
	}

	public Vector3[] getAllPositions() {
		if (allPositions == null) {
			allPositions = new Vector3[allAtoms.Count];
			int id = 0;
			foreach (UnityMolAtom a in allAtoms) {
				allPositions[id++] = a.position;
			}
		}
		return allPositions;
	}

	/// <summary>
	/// Clone a UnityMolModel by cloning chains, residues and atoms
	/// </summary>
	public UnityMolModel Clone(string newName = "") {
		List<UnityMolChain> clonedChains = new List<UnityMolChain>(chains.Count);
		List<UnityMolAtom> newAllAtoms = new List<UnityMolAtom>(Count);
		foreach (UnityMolChain c in chains.Values) {
			var newC = c.Clone();
			clonedChains.Add(newC);
			newAllAtoms.AddRange(newC.allAtoms);
		}

		if (string.IsNullOrEmpty(newName)) {
			newName = name + "_cloned";
		}

		UnityMolModel cloned = new UnityMolModel(clonedChains, newName);

		if (bonds != null) {
			cloned.bonds = new UnityMolBonds();
			cloned.bonds.bonds = new Dictionary<int, int[]>(bonds.bonds);
			cloned.bonds.bondsCount = bonds.bondsCount;
		}


		if (savedBonds != null) {
			cloned.savedBonds = new UnityMolBonds();
			cloned.savedBonds.bonds = new Dictionary<int, int[]>(bonds.bonds);
			cloned.savedBonds.bondsCount = bonds.bondsCount;
		}
		if (covBondOrders != null) {
			covBondOrders = new Dictionary<AtomDuo, bondOrderType>(covBondOrders);
		}

		cloned.allAtoms = newAllAtoms;

		foreach (UnityMolChain c in cloned.chains.Values) {
			c.model = cloned;
		}

		return cloned;
	}
}
}