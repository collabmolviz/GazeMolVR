
// Unity Classes
using UnityEngine;

// C# Classes
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Net;


namespace UMol {

/// <summary>
/// Creates a UnityMolStructure object from a local XYZ file
/// Records several molecules in different models
/// </summary>
public class XYZReader: Reader {

    public static string[] XYZextensions = {"xyz"};


    public XYZReader(string fileName = ""): base(fileName) {}

    protected override UnityMolStructure ReadData(StreamReader sr, bool readHET, bool readWater,
            bool simplyParse = false, UnityMolStructure.MolecularType? forceStructureType = null) {
        List<UnityMolModel> models = new List<UnityMolModel>();
        List<UnityMolAtom> allAtoms = new List<UnityMolAtom>();
        List<UnityMolResidue> residues = new List<UnityMolResidue>();
        List<UnityMolChain> chains = new List<UnityMolChain>();


        int nbAtomsToParse = 0;
        int idAtom = 1;
        bool readAtomLine = false;
        bool commentLine = false;
        int curMol = 0;
        using (sr) { // Don't use garbage collection but free temp memory after reading the pdb file
            StringBuilder debug = new StringBuilder();

            string line = "";
            while ((line = sr.ReadLine()) != null) {
                int dummy = 0;
                if (line.Length > 0 && int.TryParse(line, out dummy)) { //New molecule
                    if (allAtoms.Count != 0) { //Record the current Model

                        UnityMolResidue uniqueRes = new UnityMolResidue(0, 0, allAtoms, "XYZ");
                        foreach (UnityMolAtom a in allAtoms) {
                            a.SetResidue(uniqueRes);
                        }

                        residues.Add(uniqueRes);

                        UnityMolChain c = new UnityMolChain(residues, "A");
                        chains.Add(c);
                        uniqueRes.chain = c;

                        UnityMolModel model = new UnityMolModel(chains, curMol.ToString());
                        model.allAtoms.AddRange(allAtoms);

                        models.Add(model);
                        curMol++;
                        idAtom = 1;
                    }
                    nbAtomsToParse = dummy;
                    allAtoms.Clear();
                    residues.Clear();
                    chains.Clear();

                    readAtomLine = false;
                    commentLine = true;
                    continue;
                }
                if (commentLine) {
                    commentLine = false;
                    readAtomLine = true;
                    continue;
                }
                if (readAtomLine) {
                    if (line.Length == 0) {
                        continue;
                    }
                    if (idAtom == nbAtomsToParse + 1) {
                        debug.AppendFormat("More atoms in the files than specified {0} / {1}\n", idAtom, nbAtomsToParse);
                        // Debug.LogWarning("More atoms in the files than specified "+idAtom+" / "+nbAtomsToParse);
                    }
                    allAtoms.Add(parseAtomLine(line, idAtom));
                    idAtom++;
                }
            }
            Debug.LogWarning(debug.ToString());
        }


        UnityMolResidue uRes = new UnityMolResidue(0, 0, allAtoms, "XYZ");
        foreach (UnityMolAtom a in allAtoms) {
            a.SetResidue(uRes);
        }

        residues.Add(uRes);

        UnityMolChain newC = new UnityMolChain(residues, "A");
        chains.Add(newC);
        uRes.chain = newC;

        UnityMolModel lastModel = new UnityMolModel(chains, curMol.ToString());
        newC.model = lastModel;
        lastModel.allAtoms.AddRange(allAtoms);

        models.Add(lastModel);


        UnityMolStructure newStruct = new UnityMolStructure(models, this.fileNameWithoutExtension);
        
        if (!forceStructureType.HasValue) {
            identifyStructureMolecularType(newStruct);
        }
        else {
            newStruct.structureType = forceStructureType.Value;
        }

        foreach (UnityMolModel m in models) {
            m.structure = newStruct;
            if (!simplyParse) {
                m.fillIdAtoms();
                m.bonds = ComputeUnityMolBonds.ComputeBondsByResidue(m.allAtoms);
                m.ComputeCentroid();

            }
        }

        if (simplyParse) {
            return newStruct;
        }

        UnityMolSelection sel = newStruct.ToSelection();

        if (newStruct.models.Count != 1) {
            for (int i = 1; i < newStruct.models.Count; i++) {
                CreateUnityObjects(newStruct.ToSelectionName(), new UnityMolSelection(newStruct.models[i].allAtoms, newBonds: null, sel.name, newStruct.name));
            }
        }
        CreateUnityObjects(newStruct.ToSelectionName(), sel);
        newStruct.surfThread = startSurfaceThread(sel);
        
        UnityMolMain.getStructureManager().AddStructure(newStruct);
        UnityMolMain.getSelectionManager().Add(sel);

        return newStruct;
    }

    private UnityMolAtom parseAtomLine(string line, int idAtom) {
        string[] splits = line.Split(new [] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

        int atomSerial = idAtom;
        string atomElement = splits[0];
        string atomName = atomElement + idAtom.ToString();

        float x = -1 * float.Parse(splits[1], System.Globalization.CultureInfo.InvariantCulture);
        float y = float.Parse(splits[2], System.Globalization.CultureInfo.InvariantCulture);
        float z = float.Parse(splits[3], System.Globalization.CultureInfo.InvariantCulture);
        Vector3 coord = new Vector3(x, y, z);

        float bfactor = 0.0f;

        //Stored as hetatm
        UnityMolAtom newAtom = new UnityMolAtom(atomName, atomElement, coord, bfactor, atomSerial, _isHET: true);
        return newAtom;
    }


    /// <summary>
    /// XYZ writer
    /// Uses a selection
    /// Uses the molecule name of the first atom
    /// </summary>
    public static string Write(UnityMolSelection select, string structName = "") {

        StringBuilder sw = new StringBuilder();

        sw.Append(select.atoms.Count);
        sw.Append("\n");
        sw.Append(select.atoms[0].residue.chain.model.structure.name);
        sw.Append("\n");

        //Atoms
        foreach (UnityMolAtom a in select.atoms) {
            sw.AppendFormat("{0,3}", a.type);
            sw.AppendFormat("{0,15:F5}", (-a.oriPosition.x));
            sw.AppendFormat("{0,15:F5}", (a.oriPosition.y));
            sw.AppendFormat("{0,15:F5}", (a.oriPosition.z));
            sw.Append("\n");
        }

        return sw.ToString();

    }

    public static string Write(UnityMolStructure structure) {
        StringBuilder sw = new StringBuilder();

        foreach (UnityMolModel m in structure.models) {
            sw.Append(Write(m.ToSelection(), (structure.name + "_" + m.name)));
        }

        return sw.ToString();
    }
}
}