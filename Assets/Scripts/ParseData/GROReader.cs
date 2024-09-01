//
// New classes to handle GRO format
// Xavier Martinez
//
// Classes:
//   ReadData: parses a GRO string
//   TODO:
//   WriteGROToString: writes a structure in GRO format to a string

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
using System.Globalization;

namespace UMol {

/// <summary>
/// Creates a UnityMolStructure object from a local GRO file
/// </summary>
public class GROReader: Reader {

    static readonly string[] chainNames = new[] {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N",
            "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI",
            "AJ", "AK", "AL", "AM", "AN", "AO", "AP", "AQ", "AR", "AS", "AT", "AU", "AV", "AW", "AX", "AY", "AZ", "BA", "BB",
            "BC", "BD", "BE", "BF", "BG", "BH"
                                                };

    public static string[] GROextensions = {"gro"};

    public GROReader(string fileName = ""): base(fileName) {}

    /// <summary>
    /// Parses a GRO file file to a Molecule object
    /// </summary>
    protected override UnityMolStructure ReadData(StreamReader sr, bool readHET, bool readWater,
            bool simplyParse = false, UnityMolStructure.MolecularType? forceStructureType = null) {

        float start = Time.realtimeSinceStartup;

        List<UnityMolModel> models = new List<UnityMolModel>();
        List<UnityMolChain> chains = new List<UnityMolChain>();
        List<UnityMolResidue> residues = new List<UnityMolResidue>();
        List<UnityMolAtom> atomsList = new List<UnityMolAtom>();
        List<UnityMolAtom> allAtoms = new List<UnityMolAtom>();
        List<Vector3[]> frames = new List<Vector3[]>();

        bool normalPosParse = true;
        int resNum = 0;

        int lineNumber = 0;
        StringBuilder debug = new StringBuilder();
        StringBuilder sbtrim = new StringBuilder(10);
        float[] floatBuf = new float[3];


        using (sr) { // Don't use garbage collection but free temp memory after reading the pdb file

            // Read frames as trajectory frames
            if (modelsAsTraj) {
                Vector3[] curFrame = null;

                HashSet<int> doneResidues = new HashSet<int>();

                bool onlyReadPos = false;
                int countAtomsInModels = 0;
                int idA = 0;
                string line;
                int curModel = 0;
                int curChain = 0;
                int lastResidueid = -1;
                int prevAtomSerial = -1;
                int atomSerialAdd = 0;
                string lastResidueName = null;

                while ((line = sr.ReadLine()) != null) {

                    if (line.Contains("t=")) {
                        if (onlyReadPos) {
                            if (countAtomsInModels != 0 && idA == countAtomsInModels) {
                                frames.Add(curFrame);
                            }
                            else {
                                debug.Append("Something went wrong when reading frame before line " + lineNumber);
                            }
                            curFrame = new Vector3[countAtomsInModels];
                            idA = 0;
                        }
                        if (atomsList.Count > 0 ) {
                            if (!onlyReadPos) { //First frame end
                                onlyReadPos = true;
                                idA = 0;
                                countAtomsInModels = allAtoms.Count;
                                curFrame = new Vector3[countAtomsInModels];
                                for (int i = 0; i < countAtomsInModels; i++) {
                                    curFrame[i] = allAtoms[i].position;
                                }
                                frames.Add(curFrame);
                                curFrame = new Vector3[countAtomsInModels];

                                residues.Add(new UnityMolResidue(resNum, lastResidueid, atomsList, lastResidueName));
                                resNum++;
                                for (int a = 0; a < atomsList.Count; a++) {
                                    atomsList[a].SetResidue(residues.Last());
                                }
                                atomsList.Clear();


                                if (residues.Count > 0) {
                                    string nameChain = "_";
                                    if (curChain < chainNames.Length)
                                        nameChain = chainNames[curChain];

                                    UnityMolChain c = new UnityMolChain(residues, nameChain);
                                    chains.Add(c);
                                    for (int r = 0; r < residues.Count; r++) {
                                        residues[r].chain = c;
                                    }
                                    curChain++;
                                    residues.Clear();
                                }
                                if (chains.Count > 0) {
                                    //Record the model
                                    UnityMolModel model = new UnityMolModel(chains, curModel.ToString());
                                    model.allAtoms.AddRange(allAtoms);
                                    allAtoms.Clear();
                                    chains.Clear();

                                    models.Add(model);
                                    chains.Clear();
                                    curModel++;
                                    prevAtomSerial = -1;
                                }
                            }
                        }
                    }

                    else if (line.Length >= 39) { //Atom line

                        if (onlyReadPos) {
                            float posx, posy, posz;

                            if (normalPosParse) {
                                //Position in Angstrom + Unity has x inverted
                                // posx = -float.Parse(line.Substring(20, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                // posy = float.Parse(line.Substring(28, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                // posz = float.Parse(line.Substring(36, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);

                                TryParseFloatFast(line, 20, 20 + 8, out posx);
                                TryParseFloatFast(line, 28, 28 + 8, out posy);
                                TryParseFloatFast(line, 36, 36 + 8, out posz);
                                posx = -posx;

                            }
                            else {
                                // string[] fields = line.Substring(20).Split(new [] { ' '}, System.StringSplitOptions.RemoveEmptyEntries);
                                // posx = -float.Parse(fields[0], System.Globalization.CultureInfo.InvariantCulture);
                                // posy = float.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture);
                                // posz = float.Parse(fields[2], System.Globalization.CultureInfo.InvariantCulture);
                                ParseFloats(3, line, ref floatBuf, 20);
                                posx = floatBuf[0];
                                posy = floatBuf[1];
                                posz = floatBuf[2];
                                // TryParseFloatFast(fields[0], 0, fields[0].Length, out posx);
                                // TryParseFloatFast(fields[1], 0, fields[1].Length, out posy);
                                // TryParseFloatFast(fields[2], 0, fields[2].Length, out posz);
                                posx = -posx;
                            }

                            Vector3 coord = new Vector3(posx, posy, posz) * 10.0f;//gro file format unit is nm not Angstrom
                            if (idA >= 0 && idA > countAtomsInModels) {
                                curFrame[idA] = coord;
                            }
                            idA++;

                        }
                        else {
                            try {
                                int resnb = ParseInt(line, 0, 5);//int.Parse(line.Substring(0, 5).Trim());
                                string resName = SubstringWithTrim(sbtrim, line, 5, 5);//line.Substring(5, 5).Trim();
                                string atomName = SubstringWithTrim(sbtrim, line, 10, 5);//line.Substring(10, 5).Trim();
                                int atomSerial = ParseInt(line, 15, 15 + 5); //int.Parse(line.Substring(15, 5).Trim());


                                float posx, posy, posz;

                                if (!readWater && WaterSelection.waterResidues.Contains(resName, StringComparer.OrdinalIgnoreCase)) {
                                    continue;
                                }
                                bool isHet = (resName == "HEC");

                                if (!readHET && isHet) {
                                    continue;
                                }

                                string type = PDBReader.GuessElementFromAtomName(atomName, resName, isHet);

                                if (allAtoms.Count == 0) {//First atom line read => try to parse using columns and if it fails try to split the line
                                    try {
                                        //Position in angstrom + Unity has x inverted
                                        TryParseFloatFast(line, 20, 20 + 8, out posx);
                                        TryParseFloatFast(line, 28, 28 + 8, out posy);
                                        TryParseFloatFast(line, 36, 36 + 8, out posz);
                                        posx = -posx;
                                        normalPosParse = true;
                                    }
                                    catch { //Test to parse with a split approach

                                        ParseFloats(3, line, ref floatBuf, 20);
                                        posx = floatBuf[0];
                                        posy = floatBuf[1];
                                        posz = floatBuf[2];
                                        posx = -posx;
                                        normalPosParse = false;
                                    }
                                }
                                else {
                                    if (normalPosParse) {
                                        //Position in Angstrom + Unity has x inverted
                                        // posx = -float.Parse(line.Substring(20, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                        // posy = float.Parse(line.Substring(28, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                        // posz = float.Parse(line.Substring(36, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                        TryParseFloatFast(line, 20, 20 + 8, out posx);
                                        TryParseFloatFast(line, 28, 28 + 8, out posy);
                                        TryParseFloatFast(line, 36, 36 + 8, out posz);
                                        posx = -posx;
                                    }
                                    else {
                                        // string[] fields = line.Substring(20).Split(new [] { ' '}, System.StringSplitOptions.RemoveEmptyEntries);
                                        // posx = -float.Parse(fields[0], System.Globalization.CultureInfo.InvariantCulture);
                                        // posy = float.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture);
                                        // posz = float.Parse(fields[2], System.Globalization.CultureInfo.InvariantCulture);

                                        // TryParseFloatFast(fields[0], 0, fields[0].Length, out posx);
                                        // TryParseFloatFast(fields[1], 0, fields[1].Length, out posy);
                                        // TryParseFloatFast(fields[2], 0, fields[2].Length, out posz);

                                        ParseFloats(3, line, ref floatBuf, 20);
                                        posx = floatBuf[0];
                                        posy = floatBuf[1];
                                        posz = floatBuf[2];

                                        posx = -posx;
                                    }
                                }

                                Vector3 coord = new Vector3(posx, posy, posz) * 10.0f;//gro file format unit is nm not Angstrom


                                if (atomsList.Count != 0 && lastResidueid != resnb) { // New residue
                                    if (doneResidues.Contains(lastResidueid)) { //Residue number already done = new Chain
                                        if (residues.Count > 0) {
                                            string nameChain = "_";
                                            if (curChain < chainNames.Length)
                                                nameChain = chainNames[curChain];
                                            chains.Add(new UnityMolChain(residues, nameChain));
                                            for (int r = 0; r < residues.Count; r++) {
                                                residues[r].chain = chains.Last();
                                            }
                                            residues.Clear();
                                            curChain++;
                                        }
                                        doneResidues.Clear();
                                    }


                                    residues.Add(new UnityMolResidue(resNum, lastResidueid, atomsList, lastResidueName));
                                    resNum++;
                                    for (int a = 0; a < atomsList.Count; a++) {
                                        atomsList[a].SetResidue(residues.Last());
                                    }
                                    atomsList.Clear();

                                    doneResidues.Add(lastResidueid);
                                }

                                int modifAtomSerial = atomSerial;
                                if (prevAtomSerial == 99999 && atomSerial < 10) {
                                    atomSerialAdd += 100000;
                                }
                                modifAtomSerial = atomSerialAdd + atomSerial;

                                float bfactor = 0.0f;
                                UnityMolAtom newAtom = new UnityMolAtom(atomName, type, coord, bfactor, modifAtomSerial, isHet);
                                atomsList.Add(newAtom);
                                allAtoms.Add(newAtom);

                                lastResidueid = resnb;
                                lastResidueName = resName;
                                prevAtomSerial = atomSerial;
                            }
                            catch {
                                debug.AppendFormat("Ignoring line {0} : {1}\n", (lineNumber + 1), line);
                                // Debug.LogWarning("Ignoring line " + (lineNumber + 1) + " : " + line);
                            }
                        }
                    }
                    lineNumber++;
                }

                if (debug.Length != 0) {
                    Debug.LogWarning(debug.ToString());
                }


                if (!onlyReadPos) {//No frames to record
                    // Record last residue and last chain
                    if (atomsList.Count > 0) {

                        residues.Add(new UnityMolResidue(resNum, lastResidueid, atomsList, lastResidueName));
                        resNum++;
                        for (int a = 0; a < atomsList.Count; a++) {
                            atomsList[a].SetResidue(residues.Last());
                        }
                        atomsList.Clear();


                    }

                    if (residues.Count > 0) {
                        string nameChain = "_";
                        if (curChain < chainNames.Length)
                            nameChain = chainNames[curChain];

                        UnityMolChain c = new UnityMolChain(residues, nameChain);
                        chains.Add(c);
                        for (int r = 0; r < residues.Count; r++) {
                            residues[r].chain = c;
                        }
                        curChain++;
                        residues.Clear();
                    }
                    if (chains.Count > 0) {
                        //Record the model
                        UnityMolModel model = new UnityMolModel(chains, curModel.ToString());
                        model.allAtoms.AddRange(allAtoms);
                        allAtoms.Clear();
                        chains.Clear();

                        models.Add(model);
                        chains.Clear();
                        curModel++;
                        prevAtomSerial = -1;
                    }
                }
                else if (idA == countAtomsInModels) { //Record as new frame
                    frames.Add(curFrame);
                    curFrame = new Vector3[countAtomsInModels];
                }

            }
            // Read GRO frames as new models
            else {
                HashSet<int> doneResidues = new HashSet<int>();

                string line;
                int curModel = 0;
                int curChain = 0;
                int lastResidue = -1;
                int prevAtomSerial = -1;
                int atomSerialAdd = 0;
                string lastResidueName = null;

                while ((line = sr.ReadLine()) != null) {

                    if (line.Contains("t=")) {
                        if (atomsList.Count > 0 ) { //New model
                            //Save previously recorded atoms to residue
                            residues.Add(new UnityMolResidue(resNum, lastResidue, atomsList, lastResidueName));
                            resNum++;
                            for (int a = 0; a < atomsList.Count; a++) {
                                atomsList[a].SetResidue(residues.Last());
                            }
                            atomsList.Clear();
                        }


                        if (residues.Count > 0) {
                            string nameChain = "_";
                            if (curChain < chainNames.Length)
                                nameChain = chainNames[curChain];

                            UnityMolChain c = new UnityMolChain(residues, nameChain);
                            chains.Add(c);
                            for (int r = 0; r < residues.Count; r++) {
                                residues[r].chain = c;
                            }
                            residues.Clear();
                            doneResidues.Clear();
                            curChain++;
                        }
                        if (chains.Count > 0) {
                            //Record the model
                            UnityMolModel model = new UnityMolModel(chains, curModel.ToString());
                            model.allAtoms.AddRange(allAtoms);
                            allAtoms.Clear();
                            chains.Clear();
                            models.Add(model);
                            curModel++;
                            curChain = 0;
                        }

                    }

                    else if (line.Length >= 39) { //Atom line
                        try {
                            int resid = ParseInt(line, 0, 5);//int.Parse(line.Substring(0, 5).Trim());
                            string resName = SubstringWithTrim(sbtrim, line, 5, 5);//line.Substring(5, 5).Trim();
                            string atomName = SubstringWithTrim(sbtrim, line, 10, 5);//line.Substring(10, 5).Trim();
                            int atomSerial = ParseInt(line, 15, 15 + 5); //int.Parse(line.Substring(15, 5).Trim());


                            float posx, posy, posz;

                            if (!readWater && WaterSelection.waterResidues.Contains(resName, StringComparer.OrdinalIgnoreCase)) {
                                continue;
                            }
                            bool isHet = (resName == "HEC");

                            if (!readHET && isHet) {
                                continue;
                            }

                            string type = PDBReader.GuessElementFromAtomName(atomName, resName, isHet);

                            if (allAtoms.Count == 0) {
                                try {
                                    //Position in angstrom + Unity has x inverted
                                    // posx = -float.Parse(line.Substring(20, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                    // posy = float.Parse(line.Substring(28, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                    // posz = float.Parse(line.Substring(36, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                    TryParseFloatFast(line, 20, 20 + 8, out posx);
                                    TryParseFloatFast(line, 28, 28 + 8, out posy);
                                    TryParseFloatFast(line, 36, 36 + 8, out posz);
                                    posx = -posx;
                                    normalPosParse = true;
                                }
                                catch { //Test to parse with a split approach
                                    // string[] fields = line.Substring(20).Split(new [] { ' '}, System.StringSplitOptions.RemoveEmptyEntries);
                                    // posx = -float.Parse(fields[0], System.Globalization.CultureInfo.InvariantCulture);
                                    // posy = float.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture);
                                    // posz = float.Parse(fields[2], System.Globalization.CultureInfo.InvariantCulture);

                                    // TryParseFloatFast(fields[0], 0, fields[0].Length, out posx);
                                    // TryParseFloatFast(fields[1], 0, fields[1].Length, out posy);
                                    // TryParseFloatFast(fields[2], 0, fields[2].Length, out posz)
                                    ;
                                    ParseFloats(3, line, ref floatBuf, 20);
                                    posx = floatBuf[0];
                                    posy = floatBuf[1];
                                    posz = floatBuf[2];

                                    posx = -posx;
                                    normalPosParse = false;
                                }
                            }
                            else {
                                if (normalPosParse) {
                                    //Position in Angstrom + Unity has x inverted
                                    // posx = -float.Parse(line.Substring(20, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                    // posy = float.Parse(line.Substring(28, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                    // posz = float.Parse(line.Substring(36, 8).Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                    TryParseFloatFast(line, 20, 20 + 8, out posx);
                                    TryParseFloatFast(line, 28, 28 + 8, out posy);
                                    TryParseFloatFast(line, 36, 36 + 8, out posz);
                                    posx = -posx;
                                }
                                else {
                                    // string[] fields = line.Substring(20).Split(new [] { ' '}, System.StringSplitOptions.RemoveEmptyEntries);
                                    // posx = -float.Parse(fields[0], System.Globalization.CultureInfo.InvariantCulture);
                                    // posy = float.Parse(fields[1], System.Globalization.CultureInfo.InvariantCulture);
                                    // posz = float.Parse(fields[2], System.Globalization.CultureInfo.InvariantCulture);

                                    // TryParseFloatFast(fields[0], 0, fields[0].Length, out posx);
                                    // TryParseFloatFast(fields[1], 0, fields[1].Length, out posy);
                                    // TryParseFloatFast(fields[2], 0, fields[2].Length, out posz);

                                    ParseFloats(3, line, ref floatBuf, 20);
                                    posx = floatBuf[0];
                                    posy = floatBuf[1];
                                    posz = floatBuf[2];

                                    posx = -posx;
                                }
                            }

                            Vector3 coord = new Vector3(posx, posy, posz) * 10.0f;//gro file format unit is nm not Angstrom


                            if (atomsList.Count != 0 && lastResidue != resid) { // New residue
                                if (doneResidues.Contains(lastResidue)) { //Residue number already done = new Chain
                                    if (residues.Count > 0) {
                                        string nameChain = "_";
                                        if (curChain < chainNames.Length)
                                            nameChain = chainNames[curChain];
                                        chains.Add(new UnityMolChain(residues, nameChain));
                                        for (int r = 0; r < residues.Count; r++) {
                                            residues[r].chain = chains.Last();
                                        }
                                        residues.Clear();
                                        curChain++;
                                    }
                                    doneResidues.Clear();
                                }


                                residues.Add(new UnityMolResidue(resNum, lastResidue, atomsList, lastResidueName));
                                resNum++;
                                for (int a = 0; a < atomsList.Count; a++) {
                                    atomsList[a].SetResidue(residues.Last());
                                }
                                atomsList.Clear();

                                doneResidues.Add(lastResidue);
                            }

                            int modifAtomSerial = atomSerial;
                            if (prevAtomSerial == 99999 && atomSerial < 10) {
                                atomSerialAdd += 100000;
                            }
                            modifAtomSerial = atomSerialAdd + atomSerial;

                            float bfactor = 0.0f;

                            UnityMolAtom newAtom = new UnityMolAtom(atomName, type, coord, bfactor, modifAtomSerial, isHet);
                            atomsList.Add(newAtom);
                            allAtoms.Add(newAtom);

                            lastResidue = resid;
                            lastResidueName = resName;
                            prevAtomSerial = atomSerial;
                        }
                        catch {
                            debug.AppendFormat("Ignoring line {0} : {1}\n", (lineNumber + 1), line);
                            // Debug.LogWarning("Ignoring line " + (lineNumber + 1) + " : " + line);
                        }
                    }
                    lineNumber++;
                }

                if (debug.Length != 0) {
                    Debug.LogWarning(debug.ToString());
                }


                // Record last residue and last chain
                if (atomsList.Count > 0) {
                    residues.Add(new UnityMolResidue(resNum, lastResidue, atomsList, lastResidueName));
                    resNum++;
                    for (int a = 0; a < atomsList.Count; a++) {
                        atomsList[a].SetResidue(residues.Last());
                    }
                    atomsList.Clear();
                }
                if (residues.Count > 0) {
                    string nameChain = "_";
                    if (curChain < chainNames.Length)
                        nameChain = chainNames[curChain];

                    UnityMolChain c = new UnityMolChain(residues, nameChain);
                    chains.Add(c);
                    for (int r = 0; r < residues.Count; r++) {
                        residues[r].chain = c;
                    }
                    curChain++;
                    residues.Clear();
                }
                if (chains.Count > 0) {
                    //Record the model
                    UnityMolModel model = new UnityMolModel(chains, curModel.ToString());
                    model.allAtoms.AddRange(allAtoms);
                    allAtoms.Clear();
                    chains.Clear();

                    models.Add(model);
                    chains.Clear();
                    curModel++;
                    prevAtomSerial = -1;
                }
            }
        }

        UnityMolStructure newStruct = null;
        if (frames.Count != 0) {
            newStruct = new UnityMolStructure(models, this.fileNameWithoutExtension, frames);
        }
        else {
            newStruct = new UnityMolStructure(models, this.fileNameWithoutExtension);
        }

        if (!forceStructureType.HasValue) {
            identifyStructureMolecularType(newStruct);
        }
        else {
            newStruct.structureType = forceStructureType.Value;
        }

        if (newStruct.structureType != UnityMolStructure.MolecularType.standard) {
            newStruct.updateAtomRepValues();
        }

        for (int i = 0; i < models.Count; i++) {
            newStruct.models[i].structure = newStruct;
        }


        if (!simplyParse) {
            for (int i = 0; i < models.Count; i++) {
                newStruct.models[i].fillIdAtoms();
                // newStruct.models[i].bonds = ComputeUnityMolBonds.ComputeBondsSlidingWindow(models[i].allAtoms);
                newStruct.models[i].bonds = ComputeUnityMolBonds.ComputeBondsByResidue(models[i].allAtoms);
                newStruct.models[i].ComputeCentroid();
                // newStruct.models[i].CenterAtoms();
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
        }

#if UNITY_EDITOR
        Debug.Log("Time for parsing: " + (1000.0f * (Time.realtimeSinceStartup - start)).ToString("f3") + " ms");
#endif
        return newStruct;
    }

    /// <summary>
    /// GRO writer
    /// Uses a structure and outputs a string containing the first model
    /// </summary>
    public static string Write(UnityMolStructure structure) {
        UnityMolModel m = structure.models[0];
        return Write(m.ToSelection());
    }

    /// <summary>
    /// GRO writer
    /// Uses a selection to output a string containing the first model
    /// </summary>
    public static string Write(UnityMolSelection select, bool writeHET = true, Vector3[] overridedPos = null) {
        if (overridedPos != null && select.atoms.Count != overridedPos.Length) {
            Debug.LogError("Size of the overridedPos list does not match the number of atoms in the selections");
            return "";
        }

        if (select.structures.Count > 1) {
            Debug.LogError("Only supports selections with one structure");
            return "";
        }

        int count = select.atoms.Count;

        if (!writeHET) {
            for (int i = 0; i < select.atoms.Count; i++) {
                if (!select.atoms[i].isHET)
                    count++;
            }
        }

        StringBuilder sw = new StringBuilder();
        sw.Append(select.name.Replace("t=", "").ToUpper());
        sw.Append("\n");
        sw.AppendFormat("{0,5}", count);
        sw.Append("\n");

        int atomSerial = 0;
        int serial = 0;

        for (int i = 0; i < select.atoms.Count; i++) {
            UnityMolAtom a = select.atoms[i];
            if (a.isHET && !writeHET)
                continue;
            serial = atomSerial;
            if (atomSerial > 99999) {
                serial = atomSerial % 99999;
            }
            int resid = a.residue.id;

            if (resid > 9999) {
                resid = resid % 9999;
            }

            float x = -1 * a.oriPosition.x * 0.1f; // Revert to right-handed
            float y = a.oriPosition.y * 0.1f;
            float z = a.oriPosition.z * 0.1f;

            if (overridedPos != null) {
                x = -1 * overridedPos[i].x * 0.1f;
                y = overridedPos[i].y * 0.1f;
                z = overridedPos[i].z * 0.1f;
            }

            sw.AppendFormat(CultureInfo.InvariantCulture, "{0,5}", resid);
            sw.AppendFormat(CultureInfo.InvariantCulture, "{0,-5}", a.residue.name);
            sw.AppendFormat(CultureInfo.InvariantCulture, "{0,5}", a.name);
            sw.AppendFormat(CultureInfo.InvariantCulture, "{0,5}", serial + 1);

            int decX = (int)(1000 * (x - (int)(x)));
            int decY = (int)(1000 * (y - (int)(y)));
            int decZ = (int)(1000 * (z - (int)(z)));

            sw.AppendFormat(CultureInfo.InvariantCulture, "{0,4}.{1,-3}", (int)x , decX);
            sw.AppendFormat(CultureInfo.InvariantCulture, "{0,4}.{1,-3}", (int)y , decY);
            sw.AppendFormat(CultureInfo.InvariantCulture, "{0,4}.{1,-3}", (int)z , decZ);


            sw.Append("\n");

            atomSerial++;
        }
        sw.Append("   1.00000   1.00000   1.00000\n\n");
        return sw.ToString();
    }

}
}
