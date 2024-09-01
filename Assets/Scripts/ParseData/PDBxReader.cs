using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Net;
using Unity.Mathematics;
using ICSharpCode.SharpZipLib.GZip;

namespace UMol {

/// <summary>
/// Creates a Molecule object from a local or remote PDBx/mmCIF file file
/// </summary>
public class PDBxReader: Reader {
    public static string[] PDBxextensions = {"cif", "cif.gz", "mmcif", "mmcif.gz"};

    private float timeOut = 10.0f;
    private string PDBxServer = "https://ftp.wwpdb.org/pub/pdb/data/structures/all/mmCIF/";
    private string PDBxServerAssembly = "https://www.ebi.ac.uk/pdbe/static/entry/download/";

    public PDBxReader(string fileName = "", string PDBxServer = ""): base(fileName)
    {
        if (PDBxServer != "") {
            this.PDBxServer = PDBxServer;
        }
    }

    /// <summary>
    /// Fetches compressed PDBx/mmCIF file from RCSB servers and parses the data
    /// </summary>
    public IEnumerator Fetch(string entryCode, System.Action<UnityMolStructure> result,
                             bool readHet = true, bool readWater = true, int forceType = -1,
                             bool bioAssembly = false) {

        const string extension = ".cif.gz";
        string entryCodeLow = entryCode.ToLower();

        this.fileName = entryCode + extension;
        updateFileNames();

        string entryURL = null;
        if (bioAssembly)
            entryURL = PDBxServerAssembly + entryCodeLow + "-assembly-1" + extension;
        else
            entryURL = PDBxServer + entryCodeLow + extension;
        Debug.Log("Attempting to fetch remote file: " + entryURL);

        UnityWebRequest request = UnityWebRequest.Get(entryURL);
        yield return request.SendWebRequest();

        float timer = 0;
        bool failed = false;

        while (!request.isDone)
        {
            if (timer > timeOut) { failed = true; break; }
            timer += Time.deltaTime;
            yield return null;
        }
        if (failed || !string.IsNullOrEmpty(request.error))
        {
            request.Dispose();
            yield break;
        }


        UnityMolStructure structure = null;

        if (request.isNetworkError) {
            Debug.Log("Error reading remote URL: " + request.error);
        }
        else {
            // PDB returns a gzip'ed binary file
            using(MemoryStream byteStream = new MemoryStream(request.downloadHandler.data))
            //There is a bug in the standard GZip lib for some gzip files => use SharpZipLib
            // using(GZipStream flatStream = new GZipStream(byteStream, CompressionMode.Decompress))
            using (GZipInputStream flatStream = new GZipInputStream(byteStream))
            using (StreamReader sr = new StreamReader(flatStream)) {
                try {
                    if (forceType == -1)
                        structure = ReadData(sr, readHet, readWater);
                    else
                        structure = ReadData(sr, readHet, readWater, false, (UnityMolStructure.MolecularType)forceType);
                }
                catch (ParsingException err) {
                    Debug.LogError("Something went wrong when parsing your PDBx/mmCIF file: " + err);
                }

            }
        }
        result(structure);

    }

    public UnityMolStructure Fetch(string entryCode, bool readHet = true, bool readWater = true,
                                   int forceType = -1, bool bioAssembly = false) {

        const string extension = ".cif.gz";
        string entryCodeLow = entryCode.ToLower();

        this.fileName = entryCode + extension;
        updateFileNames();

        string entryURL = null;
        if (bioAssembly)
            entryURL = PDBxServerAssembly + entryCodeLow + "-assembly-1" + extension;
        else
            entryURL = PDBxServer + entryCodeLow + extension;

        Debug.Log("Fetching " + entryCode + " | " + entryURL);

        UnityMolStructure structure = null;


        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(entryURL);
        request.Timeout = 5000;
        using(HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
            using(Stream stream = response.GetResponseStream()) {
                //There is a bug in the standard GZip lib for some gzip files => use SharpZipLib
                // using(GZipStream flatStream = new GZipStream(stream, CompressionMode.Decompress)) {
                using (GZipInputStream flatStream = new GZipInputStream(stream)) {

                    using(StreamReader sr = new StreamReader(flatStream))
                    {

                        try {
                            if (forceType == -1)
                                structure = ReadData(sr, readHet, readWater);
                            else
                                structure = ReadData(sr, readHet, readWater, false, (UnityMolStructure.MolecularType)forceType);
                        }
                        catch (ParsingException err) {
                            Debug.LogError("Something went wrong when parsing your PDBx/mmCIF file: " + err);
                        }
                        return structure;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Parses a PDBx/mmCIF file to a UnityMolStructure object
    /// </summary>
    protected override UnityMolStructure ReadData(StreamReader sr, bool readHET, bool readWater,
            bool simplyParse = false, UnityMolStructure.MolecularType? forceStructureType = null) {
        float start = Time.realtimeSinceStartup;


        List<UnityMolModel> models = new List<UnityMolModel>();
        HashSet<string> residueAtoms = new HashSet<string>(); // to check for double definitions
        List<secStruct> parsedSSList = new List<secStruct>();

        StringBuilder debug = new StringBuilder();

        List<UnityMolAtom> atomsList = new List<UnityMolAtom>();
        List<UnityMolAtom> allAtoms = new List<UnityMolAtom>();

        List<UnityMolChain> chains = new List<UnityMolChain>();
        List<UnityMolResidue> residues = new List<UnityMolResidue>();

        List<ParsedBondCif> parsedBonded = new List<ParsedBondCif>();
        List<Vector3[]> frames = new List<Vector3[]>();
        Vector3[] curFrame = null;

        string lastChain = null;
        int lastResidueId = Int32.MinValue;
        string lastResidue = null;
        int lastModelId = Int32.MinValue;
        int resNum = 0;


        using (sr) { // Don't use garbage collection but free temp memory after reading the pdb file
            string keyField = "_atom_site.";
            string keyFieldHelix = "_struct_conf.";
            string keyFieldSheet = "_struct_sheet_range.";
            string keyFieldConnec = "_struct_conn.";
            string line;
            bool readAtomLine = false;
            bool readHelixLine = false;
            bool readSheetLine = false;
            bool readConnectLine = false;
            bool keyFieldStarted = false;
            bool keyFieldHelixStarted = false;
            bool keyFieldSheetStarted = false;
            bool keyFieldConnecStarted = false;
            string constructuedSSLine = "";
            string constructuedCoLine = "";
            string currentLine = null;
            int lineNumber = 0;
            string alternFirst = null;
            int cptAltern = 0;
            int countAtomsInModels = 0;
            int idA = 0;
            int a = 0;



            Dictionary<string, int> atomKeyId = new Dictionary<string, int>() {
                {"group_PDB", -1},
                {"auth_comp_id", -1},
                {"auth_atom_id", -1},
                {"pdbx_PDB_model_num", -1},
                {"id", -1},
                {"auth_asym_id", -1},
                {"auth_seq_id", -1},
                {"B_iso_or_equiv", -1},
                {"type_symbol", -1},
                {"Cartn_x", -1},
                {"Cartn_y", -1},
                {"Cartn_z", -1}
            };

            Dictionary<string, int> helixKeyId = new Dictionary<string, int>() {
                {"id", -1},
                {"beg_auth_asym_id", -1},
                {"beg_auth_seq_id", -1},
                {"end_auth_seq_id", -1},
                {"pdbx_PDB_helix_class", -1}
            };

            Dictionary<string, int> sheetKeyId = new Dictionary<string, int>() {
                {"id", -1},
                {"beg_auth_asym_id", -1},
                {"beg_auth_seq_id", -1},
                {"end_auth_seq_id", -1}
            };

            Dictionary<string, int> connectKeyId = new Dictionary<string, int>() {
                {"conn_type_id", -1},
                {"ptnr1_auth_asym_id", -1},
                {"ptnr2_auth_asym_id", -1},
                {"ptnr1_label_comp_id", -1},
                {"ptnr1_label_atom_id", -1},
                {"ptnr2_label_comp_id", -1},
                {"ptnr2_label_atom_id", -1},
                {"ptnr1_auth_seq_id", -1},
                {"ptnr2_auth_seq_id", -1}

            };

            int cptAtomKey = 0;
            int cptHelixKey = 0;
            int cptSheetKey = 0;
            int cptConnectKey = 0;
            bool allFields = true;
            bool minimalFields = false;


            while ((line = sr.ReadLine()) != null) {
                lineNumber++;

                string[] splits = line.Split(new [] { "\r", "\n", Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length > 0) {
                    currentLine = splits[0];
                    string trimmedLine = currentLine.TrimEnd();

                    //Get the order of the fields
                    if (QuickStartWith(trimmedLine, keyField)) {
                        string n = trimmedLine.Substring(trimmedLine.IndexOf(keyField) + keyField.Length);
                        atomKeyId[n] = cptAtomKey;
                        cptAtomKey++;
                        keyFieldStarted = true;
                    }
                    else if (keyFieldStarted) {
                        keyFieldStarted = false;
                        readAtomLine = true;
                        foreach (int v in atomKeyId.Values) {
                            if (v == -1) {
                                allFields = false;
                            }
                        }
                        if (!allFields) {
                            minimalFields = true;
                            if (atomKeyId["Cartn_x"] == -1 || atomKeyId["Cartn_y"] == -1 || atomKeyId["Cartn_z"] == -1 ||
                                    atomKeyId["type_symbol"] == -1 || atomKeyId["id"] == -1) {
                                minimalFields = false;
                            }
                        }

                        if (!allFields && !minimalFields) {
                            throw new System.Exception("Not enough mmCIF fields describing atoms");
                        }

                    }
                    if (readAtomLine && trimmedLine.Length >= 1 && trimmedLine[0] == '#') {//Stop
                        readAtomLine = false;
                    }
                    if (readAtomLine) {
                        string[] splitAtomLine = currentLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (splitAtomLine.Length != cptAtomKey) {
                            debug.AppendFormat("Failed to read line {0}\n", lineNumber);
                            continue;
                        }


                        // The element column is always present (according to the dictionary)
                        // It might contain funny characters (like charges +1, +2, etc)
                        // So we trim after the first of these guys
                        string fullAtomElement = splitAtomLine[atomKeyId["type_symbol"]];
                        string atomElement = CleanAtomElement(fullAtomElement);

                        int atomSerial = ParseInt(splitAtomLine[atomKeyId["id"]]);//int.Parse(splitAtomLine[atomKeyId["id"]]);

                        // Flip x coord to account for Unity left-handedness
                        // float xcoord = -1 * float.Parse(splitAtomLine[atomKeyId["Cartn_x"]], System.Globalization.CultureInfo.InvariantCulture);
                        // float ycoord = float.Parse(splitAtomLine[atomKeyId["Cartn_y"]], System.Globalization.CultureInfo.InvariantCulture);
                        // float zcoord = float.Parse(splitAtomLine[atomKeyId["Cartn_z"]], System.Globalization.CultureInfo.InvariantCulture);
                        // Vector3 coord = new Vector3(xcoord, ycoord, zcoord);

                        float px, py, pz;
                        TryParseFloatFast(splitAtomLine[atomKeyId["Cartn_x"]], 0, splitAtomLine[atomKeyId["Cartn_x"]].Length, out px);
                        TryParseFloatFast(splitAtomLine[atomKeyId["Cartn_y"]], 0, splitAtomLine[atomKeyId["Cartn_y"]].Length, out py);
                        TryParseFloatFast(splitAtomLine[atomKeyId["Cartn_z"]], 0, splitAtomLine[atomKeyId["Cartn_z"]].Length, out pz);
                        Vector3 coord = new Vector3(-px, py, pz);

                        if (minimalFields) {
                            UnityMolAtom curAtom = new UnityMolAtom(fullAtomElement + "_" + atomSerial, atomElement, coord, 0.0f, atomSerial, true);
                            atomsList.Add(curAtom);
                            allAtoms.Add(curAtom);
                            lastResidue = "CIF";
                            lastChain = "A";
                            lastResidueId = 0;
                            lastModelId = 0;
                            continue;
                        }

                        int modelId = ParseInt(splitAtomLine[atomKeyId["pdbx_PDB_model_num"]]);//int.Parse(splitAtomLine[atomKeyId["pdbx_PDB_model_num"]]);
                        string resName = splitAtomLine[atomKeyId["auth_comp_id"]];
                        string atomName = splitAtomLine[atomKeyId["auth_atom_id"]].Replace("\"", "");
                        string atomChain = splitAtomLine[atomKeyId["auth_asym_id"]];
                        int resid = ParseInt(splitAtomLine[atomKeyId["auth_seq_id"]]);//int.Parse(splitAtomLine[atomKeyId["auth_seq_id"]]);

                        // This is actually weird: the PDBx/mmCIF->PDB conversion page says B_iso_or_equiv_esd
                        // but the dictionary says this is found in only 0.008% of all entries ...
                        // B_iso_or_equiv on the other hand is found in 100% ..
                        float bfactor = 0.0f;
                        // float.Parse(splitAtomLine[atomKeyId["B_iso_or_equiv"]], System.Globalization.CultureInfo.InvariantCulture);
                        TryParseFloatFast(splitAtomLine[atomKeyId["B_iso_or_equiv"]], 0, splitAtomLine[atomKeyId["B_iso_or_equiv"]].Length, out bfactor);

                        bool isHetAtm = splitAtomLine[atomKeyId["group_PDB"]] == "HETATM";
                        bool isWater = WaterSelection.waterResidues.Contains(resName, StringComparer.OrdinalIgnoreCase);

                        if (isHetAtm && !readHET) {
                            continue;
                        }

                        if (isWater && !readWater) {
                            continue;
                        }

                        string altern = splitAtomLine[atomKeyId["label_alt_id"]].Trim();
                        if (altern != "" && alternFirst == null) {
                            alternFirst = altern;
                        }


                        //Read atoms as a trajectory frame
                        if (modelsAsTraj) {
                            if (frames.Count != 0) {//Not the first model -> only parse positions
                                if (idA == countAtomsInModels) {//End of model
                                    idA = 0;
                                    frames.Add(curFrame);
                                    curFrame = new Vector3[countAtomsInModels];
                                }
                                curFrame[idA] = coord;

                                idA++;
                                a++;
                                continue;
                            }
                        }


                        // Check for continuity of the chain
                        // And for atom/residue heterogeneity (partial occupancies, insertion codes, ...)
                        // If this is the case, ignore the atom and send a warning to the logger.
                        //
                        if (a > 0) { // skip all tests on first atom ...

                            if (atomChain == lastChain) { // same chain
                                if (resid == lastResidueId) { // same residue number
                                    if (resName != lastResidue) { // different residue name => new residue
                                        residueAtoms.Clear();
                                        if (atomsList.Count > 0) {
                                            residues.Add(new UnityMolResidue(resNum, lastResidueId, atomsList, lastResidue));
                                            resNum++;
                                            for (int i = 0; i < atomsList.Count; i++) {
                                                atomsList[i].SetResidue(residues.Last());
                                            }
                                            atomsList.Clear();
                                        }

                                        // Do we have a chain break?
                                        if (resid - lastResidueId > 1) {
                                            debug.AppendFormat("Chain {0} discontinuous at residue {1}\n", atomChain, resid);
                                        }
                                        residueAtoms.Add(atomName);

                                        debug.AppendFormat("Residue number {0} on chain {1} defined multiple times with different names consecutively\n", resid, atomChain);
                                    }
                                    else { // is atom name already registered? (partial occupancy)
                                        if (residueAtoms.Contains(atomName)) {
                                            if (altern != "" && altern != alternFirst) {
                                                if (cptAltern < 20) {
                                                    debug.AppendFormat("Residue {0}{1} already contains atom {2}. Ignoring alternative position\n", resName, resid, atomName);
                                                }

                                                cptAltern++;
                                                continue;
                                            }
                                            else {
                                                string newAtomName = findNewAtomName(residueAtoms, atomName);
                                                debug.AppendFormat("Residue {0}{1} already contains atom {2}. Changing name to {3}\n", resName, resid, atomName, newAtomName);
                                                // debug.AppendFormat("Residue {0}{1} already contains atom {2}\n", resName, resid, atomName);
                                                atomName = newAtomName;
                                                // continue;
                                                residueAtoms.Add(atomName);
                                            }
                                        }
                                        else {
                                            residueAtoms.Add(atomName);
                                        }
                                    }
                                }
                                else { // different residue number (new residue)
                                    residueAtoms.Clear();
                                    if (atomsList.Count > 0) {
                                        residues.Add(new UnityMolResidue(resNum, lastResidueId, atomsList, lastResidue));
                                        resNum++;
                                        for (int i = 0; i < atomsList.Count; i++) {
                                            atomsList[i].SetResidue(residues.Last());
                                        }
                                        atomsList.Clear();
                                    }

                                    // Do we have a chain break?
                                    if (resid - lastResidueId > 1) {
                                        debug.AppendFormat("Chain {0} discontinuous at residue {1}\n", atomChain, resid);
                                    }
                                    residueAtoms.Add(atomName);
                                }
                            }
                            else { // different chain identifier (new chain)
                                residueAtoms.Clear();
                                if (atomsList.Count > 0) { // New Residue = record the previous one
                                    residues.Add(new UnityMolResidue(resNum, lastResidueId, atomsList, lastResidue));
                                    resNum++;
                                    for (int i = 0; i < atomsList.Count; i++) {
                                        atomsList[i].SetResidue(residues.Last());
                                    }
                                    atomsList.Clear();
                                }
                                if (residues.Count > 0) { //New Chain = record the previous one
                                    chains.Add(new UnityMolChain(residues, lastChain));
                                    for (int r = 0; r < residues.Count; r++) {
                                        residues[r].chain = chains.Last();
                                    }
                                    residues.Clear();
                                }
                                residueAtoms.Add(atomName);
                            }

                            if (modelId != lastModelId) { // Another model

                                // Record last residue and last chain
                                if (atomsList.Count > 0) {
                                    residues.Add(new UnityMolResidue(resNum, lastResidueId, atomsList, lastResidue));
                                    resNum++;
                                    for (int i = 0; i < atomsList.Count; i++) {
                                        atomsList[i].SetResidue(residues.Last());
                                    }
                                    atomsList.Clear();
                                }
                                if (residues.Count > 0) {
                                    chains.Add(new UnityMolChain(residues, lastChain));
                                    for (int r = 0; r < residues.Count; r++) {
                                        residues[r].chain = chains.Last();
                                    }
                                    residues.Clear();
                                }
                                if (chains.Count > 0) {
                                    // Record the model
                                    UnityMolModel model = new UnityMolModel(chains, modelId.ToString());
                                    model.allAtoms.AddRange(allAtoms);
                                    countAtomsInModels = allAtoms.Count;
                                    allAtoms.Clear();
                                    models.Add(model);
                                    chains.Clear();
                                }
                                if (modelsAsTraj) {
                                    curFrame = new Vector3[countAtomsInModels];

                                    for (int i = 0; i < countAtomsInModels; i++) {
                                        curFrame[i] = models[0].allAtoms[i].position;
                                    }
                                    frames.Add(curFrame);
                                    curFrame = new Vector3[countAtomsInModels];
                                    idA = 0;
                                    curFrame[idA] = coord;
                                    idA++;
                                    a++;
                                    continue;
                                }
                            }
                        }

                        else { // ... but still catch first atom name
                            residueAtoms.Add(atomName);
                        }

                        UnityMolAtom newAtom = new UnityMolAtom(atomName, atomElement, coord, bfactor, atomSerial, isHetAtm);
                        atomsList.Add(newAtom);
                        allAtoms.Add(newAtom);

                        lastChain = atomChain;
                        lastResidue = resName;
                        lastResidueId = resid;
                        lastModelId = modelId;

                        a++;

                    }

                    //Helix -------------------------------------------------------
                    if (QuickStartWith(trimmedLine, keyFieldHelix)) {

                        keyFieldHelixStarted = true;

                        string[] sl = trimmedLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        //Not loop version
                        if (sl.Length == 2) {

                            constructuedSSLine += sl[1] + " ";

                            string n = sl[0].Split(new [] { '.' }, System.StringSplitOptions.RemoveEmptyEntries)[1];
                            helixKeyId[n] = cptHelixKey;
                        }
                        //Loop version
                        else {
                            string n = trimmedLine.Substring(trimmedLine.IndexOf(keyFieldHelix) + keyFieldHelix.Length);
                            helixKeyId[n] = cptHelixKey;
                        }

                        cptHelixKey++;

                    }
                    else if (keyFieldHelixStarted) {
                        keyFieldHelixStarted = false;
                        readHelixLine = true;
                        bool allHelixFields = true;

                        foreach (int v in helixKeyId.Values) {
                            if (v == -1) {
                                allHelixFields = false;
                                break;
                            }
                        }
                        if (!allHelixFields) {
                            Debug.LogError("Not enough mmCIF fields describing helices => Ignoring helices");
                            readHelixLine = false;
                        }
                    }

                    if (readHelixLine && trimmedLine.Length >= 1 &&  trimmedLine[0] == '#') {//Stop

                        if (constructuedSSLine != "") {
                            string[] splitHelixLine = constructuedSSLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);


                            if (QuickStartWith(splitHelixLine[helixKeyId["id"]], "HELX")) {

                                string chainH = splitHelixLine[helixKeyId["beg_auth_asym_id"]];
                                int startRes = ParseInt(splitHelixLine[helixKeyId["beg_auth_seq_id"]]);//int.Parse(splitHelixLine[helixKeyId["beg_auth_seq_id"]]);
                                int endRes = ParseInt(splitHelixLine[helixKeyId["end_auth_seq_id"]]);//int.Parse(splitHelixLine[helixKeyId["end_auth_seq_id"]]);
                                int classH = ParseInt(splitHelixLine[helixKeyId["pdbx_PDB_helix_class"]]);//int.Parse(splitHelixLine[helixKeyId["pdbx_PDB_helix_class"]]);


                                Reader.secStruct newHelix; newHelix.start = startRes; newHelix.end = endRes;
                                newHelix.chain = chainH; newHelix.type = (UnityMolResidue.secondaryStructureType)classH;
                                parsedSSList.Add(newHelix);
                            }
                        }


                        readHelixLine = false;
                        constructuedSSLine = "";
                        helixKeyId.Clear();
                    }
                    if (readHelixLine) {

                        string[] splitHelixLine = trimmedLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

                        if (!QuickStartWith(splitHelixLine[helixKeyId["id"]], "HELX")) {
                            continue;
                        }

                        string chainH = splitHelixLine[helixKeyId["beg_auth_asym_id"]];
                        int startRes = ParseInt(splitHelixLine[helixKeyId["beg_auth_seq_id"]]);//int.Parse(splitHelixLine[helixKeyId["beg_auth_seq_id"]]);
                        int endRes = ParseInt(splitHelixLine[helixKeyId["end_auth_seq_id"]]);//int.Parse(splitHelixLine[helixKeyId["end_auth_seq_id"]]);
                        int classH = ParseInt(splitHelixLine[helixKeyId["pdbx_PDB_helix_class"]]);//int.Parse(splitHelixLine[helixKeyId["pdbx_PDB_helix_class"]]);


                        Reader.secStruct newHelix; newHelix.start = startRes; newHelix.end = endRes;
                        newHelix.chain = chainH; newHelix.type = (UnityMolResidue.secondaryStructureType)classH;
                        parsedSSList.Add(newHelix);
                    }

                    //Sheet -------------------------------------------------------
                    if (QuickStartWith(trimmedLine, keyFieldSheet)) {
                        keyFieldSheetStarted = true;


                        string[] sl = trimmedLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        //Not loop version
                        if (sl.Length == 2) {

                            constructuedSSLine += sl[1] + " ";

                            string n = sl[0].Split(new [] { '.' }, System.StringSplitOptions.RemoveEmptyEntries)[1];
                            sheetKeyId[n] = cptSheetKey;
                        }
                        //Loop version
                        else {
                            string n = trimmedLine.Substring(trimmedLine.IndexOf(keyFieldSheet) + keyFieldSheet.Length);
                            sheetKeyId[n] = cptSheetKey;
                        }

                        cptSheetKey++;
                    }
                    else if (keyFieldSheetStarted) {
                        keyFieldSheetStarted = false;
                        readSheetLine = true;
                        bool allSheetFields = true;
                        foreach (int v in sheetKeyId.Values) {
                            if (v == -1) {
                                allSheetFields = false;
                                break;
                            }
                        }
                        if (!allSheetFields) {
                            Debug.LogError("Not enough mmCIF fields describing sheets => Ignoring sheets");
                            readSheetLine = false;
                        }
                    }
                    if (readSheetLine && trimmedLine.Length >= 1 && trimmedLine[0] == '#') {//Stop

                        if (constructuedSSLine != "") {
                            string[] splitSheetLine = constructuedSSLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);


                            if (QuickStartWith(splitSheetLine[sheetKeyId["id"]], "HELX")) {

                                string chainS = splitSheetLine[sheetKeyId["beg_auth_asym_id"]];
                                int startRes = ParseInt(splitSheetLine[sheetKeyId["beg_auth_seq_id"]]);//int.Parse(splitSheetLine[sheetKeyId["beg_auth_seq_id"]]);
                                int endRes = ParseInt(splitSheetLine[sheetKeyId["end_auth_seq_id"]]);//int.Parse(splitSheetLine[sheetKeyId["end_auth_seq_id"]]);

                                Reader.secStruct newSheet; newSheet.start = startRes; newSheet.end = endRes;
                                newSheet.chain = chainS; newSheet.type = UnityMolResidue.secondaryStructureType.Strand;
                                parsedSSList.Add(newSheet);
                            }
                        }


                        readSheetLine = false;
                        constructuedSSLine = "";
                        sheetKeyId.Clear();

                    }
                    if (readSheetLine) {
                        string[] splitSheetLine = trimmedLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

                        string chainS = splitSheetLine[sheetKeyId["beg_auth_asym_id"]];
                        int startRes = ParseInt(splitSheetLine[sheetKeyId["beg_auth_seq_id"]]);//int.Parse(splitSheetLine[sheetKeyId["beg_auth_seq_id"]]);
                        int endRes = ParseInt(splitSheetLine[sheetKeyId["end_auth_seq_id"]]);//int.Parse(splitSheetLine[sheetKeyId["end_auth_seq_id"]]);

                        Reader.secStruct newSheet; newSheet.start = startRes; newSheet.end = endRes;
                        newSheet.chain = chainS; newSheet.type = UnityMolResidue.secondaryStructureType.Strand;
                        parsedSSList.Add(newSheet);

                    }

                    //Connect -------------------------------------------------------
                    if (QuickStartWith(trimmedLine, keyFieldConnec)) {

                        keyFieldConnecStarted = true;

                        string[] sl = trimmedLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        //Not loop version
                        if (sl.Length == 2) {

                            constructuedCoLine += sl[1] + " ";

                            string n = sl[0].Split(new [] { '.' }, System.StringSplitOptions.RemoveEmptyEntries)[1];
                            connectKeyId[n] = cptConnectKey;
                        }
                        //Loop version
                        else {
                            string n = trimmedLine.Substring(trimmedLine.IndexOf(keyFieldConnec) + keyFieldConnec.Length);
                            connectKeyId[n] = cptConnectKey;
                        }

                        cptConnectKey++;
                    }
                    else if (keyFieldConnecStarted) {
                        keyFieldConnecStarted = false;
                        readConnectLine = true;
                        bool allConnecFields = true;

                        foreach (int v in connectKeyId.Values) {
                            if (v == -1) {
                                allConnecFields = false;
                                break;
                            }
                        }
                        if (!allConnecFields) {
                            Debug.LogError("Not enough mmCIF fields describing bonds => Ignoring bonds");
                            readConnectLine = false;
                        }
                    }

                    if (readConnectLine && trimmedLine.Length >= 1 &&  trimmedLine[0] == '#') {//Stop

                        if (constructuedCoLine != "") {
                            string[] splitCoLine = constructuedCoLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);


                            if (splitCoLine[connectKeyId["conn_type_id"]] == "covale") {
                                ParsedBondCif tmpb;
                                tmpb.chain1 = splitCoLine[connectKeyId["ptnr1_auth_asym_id"]];
                                tmpb.chain2 = splitCoLine[connectKeyId["ptnr2_auth_asym_id"]];
                                tmpb.resn1 = splitCoLine[connectKeyId["ptnr1_label_comp_id"]];
                                tmpb.resn2 = splitCoLine[connectKeyId["ptnr2_label_comp_id"]];
                                tmpb.resId1 = ParseInt(splitCoLine[connectKeyId["ptnr1_auth_seq_id"]]);//int.Parse(splitCoLine[connectKeyId["ptnr1_auth_seq_id"]]);
                                tmpb.resId2 = ParseInt(splitCoLine[connectKeyId["ptnr2_auth_seq_id"]]);//int.Parse(splitCoLine[connectKeyId["ptnr2_auth_seq_id"]]);
                                tmpb.a1 = splitCoLine[connectKeyId["ptnr1_label_atom_id"]];
                                tmpb.a2 = splitCoLine[connectKeyId["ptnr2_label_atom_id"]];
                                parsedBonded.Add(tmpb);
                            }
                        }

                        readConnectLine = false;
                        constructuedCoLine = "";
                        connectKeyId.Clear();
                    }
                    if (readConnectLine) {

                        string[] splitConnectLine = trimmedLine.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        if (connectKeyId.ContainsKey("conn_type_id") && splitConnectLine.Length < connectKeyId["conn_type_id"]) {
                            string bondType = splitConnectLine[connectKeyId["conn_type_id"]];
                            if (bondType == "covale") {
                                ParsedBondCif tmpb;
                                tmpb.chain1 = splitConnectLine[connectKeyId["ptnr1_auth_asym_id"]];
                                tmpb.chain2 = splitConnectLine[connectKeyId["ptnr2_auth_asym_id"]];
                                tmpb.resn1 = splitConnectLine[connectKeyId["ptnr1_label_comp_id"]];
                                tmpb.resn2 = splitConnectLine[connectKeyId["ptnr2_label_comp_id"]];
                                tmpb.resId1 = ParseInt(splitConnectLine[connectKeyId["ptnr1_auth_seq_id"]]);//int.Parse(splitConnectLine[connectKeyId["ptnr1_auth_seq_id"]]);
                                tmpb.resId2 = ParseInt(splitConnectLine[connectKeyId["ptnr2_auth_seq_id"]]);//int.Parse(splitConnectLine[connectKeyId["ptnr2_auth_seq_id"]]);
                                tmpb.a1 = splitConnectLine[connectKeyId["ptnr1_label_atom_id"]];
                                tmpb.a2 = splitConnectLine[connectKeyId["ptnr2_label_atom_id"]];
                                parsedBonded.Add(tmpb);
                            }
                        }
                    }
                }
            }
        }
        if (debug.Length > 0) {
            Debug.Log(debug.ToString());
        }

        // Record last residue and last chain
        if (atomsList.Count > 0) {
            residues.Add(new UnityMolResidue(resNum, lastResidueId, atomsList, lastResidue));
            resNum++;
            for (int a = 0; a < atomsList.Count; a++) {
                atomsList[a].SetResidue(residues.Last());
            }
            atomsList.Clear();
        }
        if (residues.Count > 0) {
            chains.Add(new UnityMolChain(residues, lastChain));
            for (int r = 0; r < residues.Count; r++) {
                residues[r].chain = chains.Last();
            }
            residues.Clear();
        }
        if (chains.Count > 0) {
            // Record the model
            UnityMolModel model = new UnityMolModel(chains, lastModelId.ToString());
            model.allAtoms.AddRange(allAtoms);
            allAtoms.Clear();
            models.Add(model);
            chains.Clear();
        }
        if (modelsAsTraj && frames.Count != 0) {
            frames.Add(curFrame);
        }

        if (models.Count == 0) {
            throw new System.Exception("mmCIF parsing error");
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

        //Process parsed connectivity
        List<int2> bondedAtoms = new List<int2>();
        if (parsedBonded != null && parsedBonded.Count != 0) {
            foreach (ParsedBondCif b in parsedBonded) {
                try {
                    int2 pair;
                    pair.x = (int)newStruct.models[0].chains[b.chain1].residues[b.resId1].atoms[b.a1].number;
                    pair.y = (int)newStruct.models[0].chains[b.chain2].residues[b.resId2].atoms[b.a2].number;
                    bondedAtoms.Add(pair);
                }
                catch (System.Exception) {
                }
            }
        }
        if (bondedAtoms.Count != 0) {
            newStruct.parsedConnectivity = bondedAtoms;
        }

        for (int i = 0; i < models.Count; i++) {
            newStruct.models[i].structure = newStruct;

            if (!simplyParse) {
                newStruct.models[i].fillIdAtoms();
                // newStruct.models[i].bonds = ComputeUnityMolBonds.ComputeBondsSlidingWindow(models[i].allAtoms);
                newStruct.models[i].bonds = ComputeUnityMolBonds.ComputeBondsByResidue(models[i].allAtoms);

                newStruct.models[i].ComputeCentroid();
                // newStruct.models[i].CenterAtoms();
            }
        }

        if (simplyParse) {
            return newStruct;
        }
        FillSecondaryStructure(newStruct, parsedSSList);
        newStruct.parsedSSInfo = parsedSSList;
        UnityMolSelection sel = newStruct.ToSelection();

        if (newStruct.models.Count != 1) {
            for (int i = 1; i < newStruct.models.Count; i++) {
                CreateUnityObjects(newStruct.ToSelectionName(), new UnityMolSelection(newStruct.models[i].allAtoms, null, sel.name, newStruct.name));
            }
        }
        CreateUnityObjects(newStruct.ToSelectionName(), sel);
        newStruct.surfThread = startSurfaceThread(sel);

        UnityMolMain.getStructureManager().AddStructure(newStruct);
        UnityMolMain.getSelectionManager().Add(sel);

#if UNITY_EDITOR
        Debug.Log("Time for parsing: " + (1000.0f * (Time.realtimeSinceStartup - start)).ToString("f3") + " ms");
#endif

        return newStruct;

    }


/// <summary>
/// Hacky clean of atom element field of PDBx/mmCIF
/// to help handle charged elements (Ca2+) and other weirdos (H(SDS))
/// Will return "C" (carbon) if it is really a weirdo.
/// </summary>
    private string CleanAtomElement(string fullElement) {

        StringBuilder builder = new StringBuilder();

        foreach (char elementChar in fullElement) {
            if (!Char.IsLetter(elementChar)) {
                break;
            }
            builder.Append(elementChar);
        }

        string atomElement = builder.ToString();
        if (string.IsNullOrEmpty(atomElement)) {
            atomElement = "C";
        }
        return atomElement;
    }

    public struct ParsedBondCif {
        public string chain1;
        public string chain2;
        public string resn1;
        public string resn2;
        public int resId1;
        public int resId2;
        public string a1;
        public string a2;
    }
}
}
