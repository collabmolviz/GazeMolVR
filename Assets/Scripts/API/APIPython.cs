using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;


namespace UMol {
namespace API {


/// <summary>
/// Defines all the functions available from the console.
/// <c>APIPython</c> derives from <c>MonoBehaviour</c> to access the coroutines for a few methodes.
/// The rest of the methods are static because no instance is needed.
/// </summary>
public class APIPython : MonoBehaviour {

    /// Path of data folder
    public static string path = "";

    /// Uniq instance of the class (Singleton).
    public static APIPython instance;

    ///Limit the size of selection string query, switch to atomid ranges when over this limit
    public static int limitSizeSelectionString = 500;

    /// Reference to the python console
    public static PythonConsole2 pythonConsole;


    /// Component for external TCP commands.
    private static TCPServerCommand extCom = null;

    /// Output correctly formated floats
    private static CultureInfo culture = CultureInfo.InvariantCulture;

    void Awake() {

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        instance = this;

        path = Application.dataPath;
        PythonConsole2[] objs = FindObjectsOfType<PythonConsole2>();
        if (objs.Length == 0) {
            Debug.LogWarning("Couldn't find the python console object");
        } else {
            pythonConsole = objs[0];
        }
    }

    /// <summary>
    /// Fetch a remote molecular file (pdb or mmcif zipped)
    /// forceStructureType (-1 = auto-detect / 0 = standard / 1 = CG / 2 = OPEP / 3 = HIRERNA)
    /// </summary>
    public static UnityMolStructure fetch(string PDBId, bool usemmCIF = true, bool readHetm = true, bool forceDSSP = false, bool showDefaultRep = true, bool center = true, bool modelsAsTraj = true, int forceStructureType = -1, bool bioAssembly = false) {

        UnityMolStructure newStruct = null;

        if (usemmCIF) {
            PDBxReader rx = new PDBxReader();
            rx.modelsAsTraj = modelsAsTraj;

            newStruct = rx.Fetch(PDBId, readHet : readHetm, forceType : forceStructureType, bioAssembly : bioAssembly);
        } else {
            if (bioAssembly) {
                Debug.LogWarning("Biological Assembly data are available only for mmCIF");
            }
            PDBReader r = new PDBReader();
            r.modelsAsTraj = modelsAsTraj;

            newStruct = r.Fetch(PDBId, readHet : readHetm, forceType : forceStructureType);
        }

        newStruct.readHET = readHetm;
        newStruct.modelsAsTraj = modelsAsTraj;
        newStruct.fetchedmmCIF = usemmCIF;
        newStruct.pdbID = PDBId;
        newStruct.bioAssembly = bioAssembly;

        UnityMolSelection sel = newStruct.ToSelection();

        if (forceDSSP || !newStruct.ssInfoFromFile) {
            DSSP.assignSS_DSSP(newStruct);
        } else {
            Debug.Log("Using secondary structure definition from the file");
        }

        if (showDefaultRep)
            defaultRep(sel.name);
        if (center)
            centerOnStructure(newStruct.name, recordCommand : false);

        UnityMolMain.recordPythonCommand("fetch(PDBId=\"" + PDBId + "\", usemmCIF=" + cBoolToPy(usemmCIF) + ", readHetm=" + cBoolToPy(readHetm) + ", forceDSSP=" +
                                         cBoolToPy(forceDSSP) + ", showDefaultRep=" + cBoolToPy(showDefaultRep) + ", center=" + cBoolToPy(center) +
                                         ", modelsAsTraj=" + cBoolToPy(modelsAsTraj) + ", forceStructureType=" + forceStructureType +
                                         ", bioAssembly=" + cBoolToPy(bioAssembly) + ")");

        UnityMolMain.recordUndoPythonCommand("delete(\"" + newStruct.name + "\")");

        return newStruct;
    }


    /// <summary>
    /// Load a local molecular file (pdb/mmcif/gro/mol2/sdf/xyz formats)
    /// forceStructureType (-1 = auto-detect / 0 = standard / 1 = CG / 2 = OPEP / 3 = HIRERNA)
    /// </summary>
    public static UnityMolStructure load(string filePath, bool readHetm = true, bool forceDSSP = false, bool showDefaultRep = true, bool center = true, bool modelsAsTraj = true, int forceStructureType = -1) {

        string realPath = filePath;
        string customPath = Path.Combine(path, filePath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        UnityMolStructure newStruct = null;

        Reader r = Reader.GuessReaderFrom(realPath);
        if (r != null) {
            r.modelsAsTraj = modelsAsTraj;
            newStruct = r.Read(readHet: readHetm, forceType: forceStructureType);

            if (newStruct != null) {
                string fileName = Path.GetFileName(realPath);
                Debug.Log("Loaded PDB " + fileName + " with " + newStruct.models.Count + " models");
                UnityMolSelection sel = newStruct.ToSelection();

                if (forceDSSP || !newStruct.ssInfoFromFile) {
                    DSSP.assignSS_DSSP(newStruct);
                } else {
                    Debug.Log("Using secondary structure definition from the file");
                }

                if (showDefaultRep)
                    defaultRep(sel.name);
                if (center)
                    centerOnStructure(newStruct.name, recordCommand : false);
            } else {
                Debug.LogError("Could not load file " + realPath);
            }
        }
        UnityMolMain.recordPythonCommand("load(filePath=\"" + realPath.Replace("\\", "/") + "\", readHetm=" + cBoolToPy(readHetm) + ", forceDSSP=" +
                                         cBoolToPy(forceDSSP) + ", showDefaultRep=" + cBoolToPy(showDefaultRep) + ", center=" + cBoolToPy(center) +
                                         ", modelsAsTraj=" + cBoolToPy(modelsAsTraj) + ", forceStructureType=" + forceStructureType + ")");
        UnityMolMain.recordUndoPythonCommand("delete(\"" + newStruct.name + "\")");

        return newStruct;
    }

    /// <summary>
    /// Show/Hide UnityMol console
    /// </summary>
    public static void showHideConsole(bool show) {
        PythonConsole2[] objs = FindObjectsOfType<PythonConsole2>();
        foreach (var pc in objs) {
            UnityEngine.UI.Button showB = pc.showConsoleButton;
            UnityEngine.UI.Button hideB = pc.hideConsoleButton;
            if (show) {
                if (showB != null)
                    showB.onClick.Invoke();
            } else {
                if (hideB != null)
                    hideB.onClick.Invoke();
            }
        }
    }

    static bool canRunCommand() {
        if (UnityMolMain.multiUserMode && !UnityMolMain.multiUserPresenter)
            return false;
        return true;
    }

    /// <summary>
    /// Allow to call python API commands and record them in the history from C#
    /// </summary>
    public static bool ExecuteCommand(string command, bool force = false) {
        if (!force && !canRunCommand()) {
            Debug.LogWarning("You are not the current presenter");
            return false;
        }
        bool success = false;
        pythonConsole.ExecuteCommand(command, ref success);
        return success;
    }


    /// <summary>
    /// Load a molecular file (pdb/mmcif/gro/mol2/sdf/xyz formats) from a string
    /// forceStructureType (-1 = auto-detect / 0 = standard / 1 = CG / 2 = OPEP / 3 = HIRERNA)
    /// </summary>
    public static UnityMolStructure loadFromString(string fileName, string fileContent, bool readHetm = true, bool forceDSSP = false, bool showDefaultRep = true, bool center = true, bool modelsAsTraj = true, int forceStructureType = -1) {

        UnityMolStructure newStruct = null;

        string tempPath = Application.temporaryCachePath + "/" + fileName;
        try {

            using(StreamWriter sw = new StreamWriter(tempPath, false)) {

                sw.WriteLine(fileContent);
                sw.Close();
            }

            Reader r = Reader.GuessReaderFrom(tempPath);

            if (r != null) {
                r.modelsAsTraj = modelsAsTraj;

                newStruct = r.Read(readHet: readHetm, forceType: forceStructureType);

                if (newStruct != null) {
                    Debug.Log("Loaded PDB " + fileName + " with " + newStruct.models.Count + " models");
                    UnityMolSelection sel = newStruct.ToSelection();

                    if (forceDSSP || !newStruct.ssInfoFromFile) {
                        DSSP.assignSS_DSSP(newStruct);
                    } else {
                        Debug.Log("Using secondary structure definition from the file");
                    }

                    if (showDefaultRep)
                        defaultRep(sel.name);
                    if (center)
                        centerOnStructure(newStruct.name, recordCommand : false);
                } else {
                    Debug.LogError("Could not load file content");
                }
                UnityMolMain.recordPythonCommand("loadFromString(fileName=\"" + fileName + "\", fileContent= \"" +
                                                 fileContent + "\", readHetm=" + cBoolToPy(readHetm) + ", forceDSSP=" +
                                                 cBoolToPy(forceDSSP) + ", showDefaultRep=" + cBoolToPy(showDefaultRep) +
                                                 ", center=" + cBoolToPy(center) + ", modelsAsTraj=" + cBoolToPy(modelsAsTraj) +
                                                 ", forceStructureType=" + forceStructureType + ")");
                UnityMolMain.recordUndoPythonCommand("delete(\"" + newStruct.name + "\")");

            }
        } finally {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        return newStruct;
    }



    /// <summary>
    /// WIP Load martini itp file to parse elastic network and secondary structure
    /// </summary>
    public static void loadMartiniITP(string structureName, string filePath) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        string realPath = filePath;
        string customPath = Path.Combine(path, filePath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            ITPReader.loadSystemITPFile(realPath, s);
        } else {
            Debug.LogError("Structure not found");
        }

        UnityMolMain.recordPythonCommand("loadMartiniITP(\"" + structureName + "\", \"" + realPath.Replace("\\", "/") + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Show bounding box lines around the structure
    /// </summary>
    public static void showBoundingBox(string structureName) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            s.showBoundingBox();
        } else {
            Debug.LogError("Structure not found");
        }
        UnityMolMain.recordPythonCommand("showBoundingBox(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("hideBoundingBox(\"" + structureName + "\")");
    }
    /// <summary>
    /// Hide bounding box lines around the structure
    /// </summary>
    public static void hideBoundingBox(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            s.hideBoundingBox();
        } else {
            Debug.LogError("Structure not found");
        }

        UnityMolMain.recordPythonCommand("hideBoundingBox(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("showBoundingBox(\"" + structureName + "\")");
    }

    /// <summary>
    /// Set bounding box lines size
    /// </summary>
    public static void setBoundingBoxLineSize(string structureName, float size = 0.005f) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        float prevSize = 0.005f;
        if (s != null) {
            prevSize = s.getBoundingBoxLineSize();
            s.setBoundingBoxLineSize(size);
        } else {
            Debug.LogError("Structure not found");
        }

        UnityMolMain.recordPythonCommand("setBoundingBoxLineSize(\"" + structureName + "\"," + size + ")");
        UnityMolMain.recordUndoPythonCommand("setBoundingBoxLineSize(\"" + structureName + "\"," + prevSize + ")");
    }

    /// <summary>
    /// Load a XML file containing covalent and noncovalent bonds
    /// modelId = -1 means currentModel
    /// Possible bond types are: 'covalent' or 'db_geom', 'hbond' or 'h-bond' or 'hbond_weak', 'halogen', 'ionic', 'aromatic', 'hydrophobic', 'carbonyl'
    /// </summary>
    public static void loadBondsXML(string structureName, string filePath, int modelId = -1) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        string realPath = filePath;
        string customPath = Path.Combine(path, filePath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            UnityMolModel m = s.currentModel;

            if (modelId >= 0 && modelId < s.models.Count) {
                m = s.models[modelId];
            } else if (modelId != -1) {
                Debug.LogError("Wrong model");
                return;
            }

            //Stores the bonds in the model.covBondOrders
            Dictionary<bondOrderType, List<AtomDuo>> res = BondOrderParser.parseBondOrderFile(m, realPath);


            int id = 0;
            foreach (bondOrderType bot in res.Keys) {

                int maxAtomPerBond = 0;
                Dictionary<UnityMolAtom, int> bondPerA = new Dictionary<UnityMolAtom, int>(m.Count);
                //First pass to compute the max number of bonds per atom
                foreach (AtomDuo d in res[bot]) {
                    if (!bondPerA.ContainsKey(d.a1))
                        bondPerA[d.a1] = 0;
                    if (!bondPerA.ContainsKey(d.a2))
                        bondPerA[d.a2] = 0;
                    bondPerA[d.a1]++;
                    bondPerA[d.a2]++;
                    maxAtomPerBond = Mathf.Max(bondPerA[d.a1], Mathf.Max(bondPerA[d.a2], maxAtomPerBond));
                }

                UnityMolBonds curBonds = new UnityMolBonds();
                if (maxAtomPerBond > curBonds.NBBONDS)
                    curBonds.NBBONDS = maxAtomPerBond;

                foreach (AtomDuo d in res[bot]) {
                    curBonds.Add(d.a1, d.a2);
                }

                UnityMolSelection sel = new UnityMolSelection(m.allAtoms,
                        curBonds, s.name + "_" + bot.btype + id + "_ExternalBonds");

                sel.canUpdateBonds = false;
                UnityMolMain.getSelectionManager().Add(sel);

                // showSelection(sel.name, "hbondtube");
                showSelection(sel.name, "hbondtube", true);

                id++;
            }
        } else {
            Debug.LogError("Structure not found");
        }
        UnityMolMain.recordPythonCommand("loadBondsXML(\"" + structureName + "\", \"" + realPath.Replace("\\", "/") + "\", " + modelId + ")");
        UnityMolMain.recordUndoPythonCommand("unloadCustomBonds(\"" + structureName + "\", " + modelId + ")");
    }

    /// <summary>
    /// Override the current bonds of the model modelId and saves the previous one in model.savedBonds
    /// modelId = -1 means currentModel
    /// </summary>
    public static void overrideBondsWithXML(string structureName, int modelId = -1) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            UnityMolModel m = s.currentModel;

            if (modelId >= 0 && modelId < s.models.Count) {
                m = s.models[modelId];
            } else if (modelId != -1) {
                Debug.LogError("Wrong model");
                return;
            }

            Dictionary<AtomDuo, bondOrderType> xmlBonds = m.covBondOrders;
            if (xmlBonds != null) {
                m.savedBonds = m.bonds;

                UnityMolBonds newBonds = new UnityMolBonds();
                foreach (AtomDuo d in xmlBonds.Keys) {
                    newBonds.Add(d.a1, d.a2);
                }
                m.bonds = newBonds;
                s.updateRepresentations(trajectory: false);
            } else {
                Debug.LogError("No bonds parsed from a XML file in this model");
            }

        }

        UnityMolMain.recordPythonCommand("overrideBondsWithXML(\"" + structureName + "\", " + modelId + ")");
        UnityMolMain.recordUndoPythonCommand("restoreBonds(\"" + structureName + "\", " + modelId + ")");
    }

    /// <summary>
    /// Load bounding information from a PSF file
    /// modelId = -1 means currentModel
    /// </summary>
    public static void loadPSFTopology(string structureName, string psfPath, int modelId = -1) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        string realPath = psfPath;
        string customPath = Path.Combine(path, psfPath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            UnityMolModel m = s.currentModel;

            if (modelId >= 0 && modelId < s.models.Count) {
                m = s.models[modelId];
            } else if (modelId != -1) {
                Debug.LogError("Wrong model");
                return;
            }

            UnityMolBonds newBonds = PSFReader.readTopologyFromPSF(realPath, s);
            if (newBonds != null) {
                m.savedBonds = m.bonds;
                m.bonds = newBonds;
                Debug.Log("Read " + newBonds.Count + " bonds from PSF");
                s.updateRepresentations(trajectory: false);
            } else {
                Debug.LogError("No bonds were parsed from the PSF file");
                return;
            }
        }
        UnityMolMain.recordPythonCommand("loadPSFTopology(\"" + structureName + "\", \"" + realPath.Replace("\\", "/") + "\", " + modelId + ")");
        UnityMolMain.recordUndoPythonCommand("restoreBonds(\"" + structureName + "\", " + modelId + ")");
    }

    /// <summary>
    /// Load bounding information from a TOP file
    /// modelId = -1 means currentModel
    /// specialBondString when not empty is used to create a selection containing only these special bonds, shown as hbondtube
    /// </summary>
    public static void loadTOPTopology(string structureName, string topPath, int modelId = -1, string specialBondString = "restrain") {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        string realPath = topPath;
        string customPath = Path.Combine(path, topPath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            UnityMolModel m = s.currentModel;

            if (modelId >= 0 && modelId < s.models.Count) {
                m = s.models[modelId];
            } else if (modelId != -1) {
                Debug.LogError("Wrong model");
                return;
            }

            UnityMolBonds newBonds = TOPReader.readTopologyFromTOP(realPath, s, specialBondString);
            if (newBonds != null) {
                m.savedBonds = m.bonds;
                m.bonds = newBonds;
                Debug.Log("Read " + newBonds.Count + " bonds from TOP");
                s.updateRepresentations(trajectory: false);
            } else {
                Debug.LogError("No bonds were parsed from the TOP file");
                return;
            }
        }
        UnityMolMain.recordPythonCommand("loadTOPTopology(\"" + structureName + "\", \"" + realPath.Replace("\\", "/") + "\", " + modelId + ", \"" + specialBondString + "\")");
        UnityMolMain.recordUndoPythonCommand("restoreBonds(\"" + structureName + "\", " + modelId + ")");
    }
    /// <summary>
    /// Restore bonds using the model.savedBonds
    /// modelId = -1 means currentModel
    /// </summary>
    public static void restoreBonds(string structureName, int modelId = -1) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            UnityMolModel m = s.currentModel;

            if (modelId >= 0 && modelId < s.models.Count) {
                m = s.models[modelId];
            } else if (modelId != -1) {
                Debug.LogError("Wrong model");
                return;
            }

            Dictionary<AtomDuo, bondOrderType> xmlBonds = m.covBondOrders;
            if (xmlBonds != null && m.savedBonds != null) {
                m.bonds = m.savedBonds;
                s.updateRepresentations(trajectory: false);
            } else {
                Debug.LogError("No bonds parsed from a XML file in this model");
            }

        }

        UnityMolMain.recordPythonCommand("restoreBonds(\"" + structureName + "\", " + modelId + ")");
        UnityMolMain.recordUndoPythonCommand("overrideBondsWithXML(\"" + structureName + "\", " + modelId + ")");
    }
    /// <summary>
    /// Removes the covBondOrders bonds loaded by loadBondsXML from the model
    /// </summary>
    public static void unloadCustomBonds(string structureName, int modelId) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            if (modelId >= 0 && modelId < s.models.Count) {
                s.models[modelId].covBondOrders = null;
            }
        } else {
            Debug.LogError("Structure not found");
        }
        UnityMolMain.recordPythonCommand("unloadCustomBonds(\"" + structureName + "\", " + modelId + ")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Delete all the loaded molecules
    /// </summary>
    public static void reset() {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        List<string> toDelete = new List<string>();

        foreach (UnityMolStructure s in sm.loadedStructures) {
            toDelete.Add(s.name);
        }
        foreach (string s in toDelete) {
            UnityMolStructure stru = sm.GetStructure(s);
            if (stru != null) {
                sm.Delete(stru);
            }
        }
        ManipulationManager mm = getManipulationManager();
        if (mm != null) {
            mm.resetPosition();
            mm.resetRotation();
        }
        stopVideo();
        UnityMolMain.raytracingMode = false;
        clearTour();
        UnityMolMain.getAnnotationManager().Clean();
    }

    /// <summary>
    /// Switch between parsed secondary structure information and DSSP computation
    /// </summary>
    public static void switchSSAssignmentMethod(string structureName, bool forceDSSP = false) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {

            if (forceDSSP || s.parsedSSInfo == null) {
                DSSP.assignSS_DSSP(s);
                Debug.LogWarning("Setting secondary structure assignment to DSSP");
            } else if (s.ssInfoFromFile) {
                DSSP.assignSS_DSSP(s);
                Debug.LogWarning("Setting secondary structure assignment to DSSP");
            } else {
                Reader.FillSecondaryStructure(s, s.parsedSSInfo);
                Debug.LogWarning("Setting secondary structure assignment parsed from file");
            }

            UnityMolMain.getPrecompRepManager().Clear(s.name);

            UnityMolMain.recordPythonCommand("switchSSAssignmentMethod(\"" + structureName + "\"," + cBoolToPy(forceDSSP) + ")");
            UnityMolMain.recordUndoPythonCommand("switchSSAssignmentMethod(\"" + structureName + "\")");
        }
    }

    /// <summary>
    /// Show/Hide hydrogens in representations of the provided selection
    /// This only works for lines, hyperball and sphere representations
    /// </summary>
    public static void showHideHydrogensInSelection(string selName, bool? shouldShow = null) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        bool firstRep = true;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            foreach (List<UnityMolRepresentation> reps in sel.representations.Values) {
                foreach (UnityMolRepresentation r in reps) {
                    foreach (SubRepresentation sr in r.subReps) {
                        if (sr.atomRepManager != null) {
                            if (firstRep) {
                                if (!shouldShow.HasValue)
                                    shouldShow = !sr.atomRepManager.areHydrogensOn;
                                firstRep = false;
                            }
                            sr.atomRepManager.ShowHydrogens(shouldShow.HasValue ? shouldShow.Value : false);
                        }
                        if (sr.bondRepManager != null) {
                            if (firstRep) {
                                if (!shouldShow.HasValue)
                                    shouldShow = !sr.bondRepManager.areHydrogensOn;
                                firstRep = false;
                            }
                            sr.bondRepManager.ShowHydrogens(shouldShow.HasValue ? shouldShow.Value : false);
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("showHideHydrogensInSelection(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("showHideHydrogensInSelection(\"" + selName + "\")");
    }

    /// <summary>
    /// Show/Hide side chains in representations of the current selection
    /// This only works for lines, hyperball and sphere representations only
    /// </summary>
    public static void showHideSideChainsInSelection(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        bool firstRep = true;
        bool shouldShow = true;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            foreach (List<UnityMolRepresentation> reps in sel.representations.Values) {
                foreach (UnityMolRepresentation r in reps) {
                    foreach (SubRepresentation sr in r.subReps) {
                        if (sr.atomRepManager != null) {
                            if (firstRep) {
                                shouldShow = !sr.atomRepManager.areSideChainsOn;
                                firstRep = false;
                            }
                            sr.atomRepManager.ShowSideChains(shouldShow);
                        }
                        if (sr.bondRepManager != null) {
                            if (firstRep) {
                                shouldShow = !sr.bondRepManager.areSideChainsOn;
                                firstRep = false;
                            }
                            sr.bondRepManager.ShowSideChains(shouldShow);
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("showHideSideChainsInSelection(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("showHideSideChainsInSelection(\"" + selName + "\")");
    }

    /// <summary>
    /// Show/Hide backbone in representations of the current selection
    /// This only works for lines, hyperball and sphere representations only
    /// </summary>
    public static void showHideBackboneInSelection(string selName) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        bool firstRep = true;
        bool shouldShow = true;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];
            foreach (List<UnityMolRepresentation> reps in sel.representations.Values) {
                foreach (UnityMolRepresentation r in reps) {
                    foreach (SubRepresentation sr in r.subReps) {
                        if (sr.atomRepManager != null) {
                            if (firstRep) {
                                shouldShow = !sr.atomRepManager.isBackboneOn;
                                firstRep = false;
                            }
                            sr.atomRepManager.ShowBackbone(shouldShow);
                        }
                        if (sr.bondRepManager != null) {
                            if (firstRep) {
                                shouldShow = !sr.bondRepManager.isBackboneOn;
                                firstRep = false;
                            }
                            sr.bondRepManager.ShowBackbone(shouldShow);
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("showHideBackboneInSelection(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("showHideBackboneInSelection(\"" + selName + "\")");
    }

    /// <summary>
    /// Set the current model of the structure
    /// This function is used by ModelPlayers.cs to read the models of a structure like a trajectory
    /// </summary>
    public static void setModel(string structureName, int modelId) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);

        int prev = s.currentModelId;
        if (s.trajectoryMode) {
            prev = s.currentFrameId;
        }

        s.setModel(modelId);

        int lenSameCom = 10 + structureName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setModel(\"" + structureName + "\", " + modelId + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setModel(\"" + structureName + "\", " + prev + ")", replaced);
    }

    /// <summary>
    /// Load a trajectory for a loaded structure
    /// It creates a XDRFileReader in the corresponding UnityMolStructure and a TrajectoryPlayer
    /// </summary>
    public static void loadTraj(string structureName, string filePath) {

        string realPath = filePath;
        string customPath = Path.Combine(path, filePath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);

        try {
            s.readTrajectoryXDR(realPath);
            s.createTrajectoryPlayer();
        } catch (System.Exception e) {
            Debug.LogError("Could not load trajectory file '" + realPath + "'");
#if UNITY_EDITOR
            Debug.LogError(e);
#endif
            return;
        }

        UnityMolMain.recordPythonCommand("loadTraj(\"" + structureName + "\", \"" + realPath.Replace("\\", "/") + "\")");
        UnityMolMain.recordUndoPythonCommand("unloadTraj(\"" + structureName + "\")");
    }

    /// <summary>
    /// Unload a trajectory for a specific structure
    /// </summary>
    public static void unloadTraj(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        s.unloadTrajectoryXDR();

        UnityMolMain.recordPythonCommand("unloadTraj(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }
    /// <summary>
    /// Create a special selection containing frames from the trajectory
    /// </summary>
    public static string pickTrajectoryFrames(string structureName, string selectionQuery = "all", int frameStart = 0, int frameEnd = 1, int step = 1) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return null;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s == null) {
            Debug.LogError("Structure not found");
            return null;
        }
        if (s.xdr == null) {
            Debug.LogError("No trajectory loaded for this structure");
            return null;
        }
        if (step <= 0) {
            Debug.LogError("Wrong step");
            return null;
        }

        if (frameEnd < frameStart) {
            Debug.LogError("Ending frame should be larger than starting frame");
            return null;
        }
        if (frameStart < 0 || frameStart >= s.xdr.numberFrames) {
            Debug.LogError("Wrong starting frame");
            return null;
        }
        if (frameEnd < 0 || frameEnd >= s.xdr.numberFrames) {
            Debug.LogError("Wrong ending frame");
            return null;
        }

        UnityMolSelection resSel = null;
        try {
            MDAnalysisSelection selec = new MDAnalysisSelection(selectionQuery, s.currentModel.allAtoms);
            resSel = selec.process();
        } catch (System.Exception e) {
            Debug.LogError("Wrong selection query\n" + e);
            return null;
        }
        if (resSel.Count == 0) {
            Debug.LogError("Empty selection");
            return null;
        }

        List<Vector3[]> extractedPos = new List<Vector3[]>();
        List<int> idFrames = new List<int>();
        int N = resSel.Count;

        for (int idFrame = frameStart; idFrame <= frameEnd; idFrame += step) {
            Vector3[] f = s.xdr.getFrame(idFrame);
            Vector3[] pos = new Vector3[N];
            idFrames.Add(idFrame);

            for (int i = 0; i < N; i++) {
                pos[i] = f[resSel.atoms[i].idInAllAtoms];
            }
            extractedPos.Add(pos);
        }

        Debug.Log("Extracted " + idFrames.Count + " each of " + N + " atoms");

        string selName = s.name + "_pickedFrame_" + frameStart + "/" + frameEnd + "/" + step + "_" + N;

        UnityMolSelection pickedSel = new UnityMolSelection(resSel.atoms, newBonds : resSel.bonds, selName);

        pickedSel.updateRepWithTraj = false;
        pickedSel.extractTrajFrame = true;
        pickedSel.extractTrajFrameIds = idFrames;
        pickedSel.extractTrajFramePositions = extractedPos;

        selM.Add(pickedSel);

        UnityMolMain.recordPythonCommand("pickTrajectoryFrames(\"" + structureName + "\", \"" + selectionQuery + "\", " + frameStart + ", " + frameEnd + ", " + step + ")");
        UnityMolMain.recordUndoPythonCommand("deleteSelection(\"" + pickedSel.name + "\")");

        return selName;
    }

    /// <summary>
    /// Set the current trajectory frame of the structure named structureName to frame
    /// frame has to be between 0 and numberFrames
    /// </summary>
    public static void setTrajFrame(string structureName, int frame) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s == null) {
            Debug.LogError("Structure not found");
            return;
        }
        if (s.xdr == null) {
            Debug.LogError("No trajectory loaded for this structure");
            return;
        }
        if (frame < 0 || frame >= s.xdr.numberFrames) {
            Debug.LogError("Wrong frame");
        }

        int prevFrame = s.xdr.currentFrame;

        s.trajSetFrame(frame);

        UnityMolMain.recordPythonCommand("setTrajFrame(\"" + structureName + "\", " + frame + ")");
        UnityMolMain.recordUndoPythonCommand("setTrajFrame(\"" + structureName + "\", " + prevFrame + ")");
    }

    /// <summary>
    /// Load a density map for a specific structure
    /// This function creates a DXReader instance in the UnityMolStructure
    /// </summary>
    public static void loadDXmap(string structureName, string filePath) {

        string realPath = filePath;
        string customPath = Path.Combine(path, filePath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);

        try {
            s.readDX(realPath);
        } catch {
            Debug.LogError("Could not load DX map file '" + realPath + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("loadDXmap(\"" + structureName + "\", \"" + realPath.Replace("\\", "/") + "\")");
        UnityMolMain.recordUndoPythonCommand("unloadDXmap(\"" + structureName + "\")");
    }
    /// <summary>
    /// Show lines around dx map
    /// </summary>
    public static void showDXLines(string structureName) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            if (s.dxr == null) {
                Debug.LogError("No DX map loaded for this structure");
                return;
            }
            s.dxr.showLines();
        } else {
            Debug.LogError("Wrong structure name");
            return;
        }
        UnityMolMain.recordPythonCommand("showDXLines(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("hideDXLines(\"" + structureName + "\")");
    }

    /// <summary>
    /// Hide lines around dx map
    /// </summary>
    public static void hideDXLines(string structureName) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            if (s.dxr == null) {
                Debug.LogError("No DX map loaded for this structure");
                return;
            }
            s.dxr.hideLines();
        } else {
            Debug.LogError("Wrong structure name");
            return;
        }
        UnityMolMain.recordPythonCommand("hideDXLines(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("showDXLines(\"" + structureName + "\")");
    }
    /// <summary>
    /// Unload the density map for the structure
    /// </summary>
    public static void unloadDXmap(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s != null) {
            s.unloadDX();
        } else {
            Debug.LogError("Wrong structure name");
            return;
        }

        UnityMolMain.recordPythonCommand("unloadDXmap(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Read a json file and display fieldLines for the specified structure
    /// </summary>
    public static void readJSONFieldlines(string structureName, string filePath) {

        string realPath = filePath;
        string customPath = Path.Combine(path, filePath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolSelection sel = s.currentModel.ToSelection();

        FieldLinesReader flr = new FieldLinesReader(realPath);

        s.currentModel.fieldLinesR = flr;

        deleteRepresentationInSelection(s.ToSelectionName(), "fl");

        if (!selM.selections.ContainsKey(s.ToSelectionName())) {
            selM.Add(sel);
        }

        repManager.AddRepresentation(sel, AtomType.fieldlines, BondType.nobond, flr);

        UnityMolMain.recordPythonCommand("readJSONFieldlines(\"" + structureName + "\", \"" + realPath.Replace("\\", "/") + "\")");
        UnityMolMain.recordUndoPythonCommand("unloadJSONFieldlines(\"" + structureName + "\")");
    }

    /// <summary>
    /// Remove the json file for fieldlines stored in the currentModel of the specified structure
    /// </summary>
    public static void unloadJSONFieldlines(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }
        UnityMolStructure s = sm.GetStructure(structureName);
        s.currentModel.fieldLinesR = null;

        UnityMolMain.recordPythonCommand("unloadJSONFieldlines(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }
    /// <summary>
    /// Change fieldline computation gradient threshold
    /// </summary>
    public static void setFieldlineGradientThreshold(string selName, float val) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repM = UnityMolMain.getRepresentationManager();

        float prev = 10.0f;
        if (selM.selections.ContainsKey(selName)) {
            RepType repType = getRepType("fl");

            List<UnityMolRepresentation> existingReps = repM.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation rep in existingReps) {
                    SubRepresentation sr = rep.subReps.First(); //There shouldn't be more than one
                    FieldLinesRepresentation flRep = (FieldLinesRepresentation) sr.atomRep;
                    prev = flRep.magThreshold;
                    flRep.recompute(val);
                }
            }
        }
        int lenSameCom = 31 + selName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setFieldlineGradientThreshold(\"" + selName + "\", " + val.ToString("f3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setFieldlineGradientThreshold(\"" + selName + "\", " + prev.ToString("f3", culture) + ")", replaced);
    }

    /// <summary>
    /// Utility function to be able to get the group of the structure
    /// This group is used to be able to move all the loaded molecules in the same group
    /// Groups can be between 0 and 9 included
    /// </summary>
    public static int getStructureGroup(string structureName) {
        int group = -1;
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return group;
        }

        if (sm.nameToStructure.ContainsKey(structureName)) {
            UnityMolStructure s = sm.GetStructure(structureName);
            group = s.groupID;
        }
        return group;
    }

    /// <summary>
    /// Utility function to be able to get the structures of the group
    /// This group is used to be able to move all the loaded molecules in the same group
    /// Groups can be between 0 and 9 included
    /// </summary>
    public static HashSet<UnityMolStructure> getStructuresOfGroup(int group) {
        HashSet<UnityMolStructure> result = new HashSet<UnityMolStructure>();

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        foreach (UnityMolStructure s in sm.loadedStructures) {
            if (s.groupID == group) {
                result.Add(s);
            }
        }
        return result;
    }

    /// <summary>
    /// Utility function to be set the group of a structure
    /// This group is used to be able to move all the loaded molecules in the same group
    /// Groups can be between 0 and 9 included
    /// </summary>
    public static void setStructureGroup(string structureName, int newGroup) {

        int prevGroup = 0;
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        if (sm.nameToStructure.ContainsKey(structureName)) {
            UnityMolStructure s = sm.GetStructure(structureName);
            prevGroup = s.groupID;
            s.groupID = newGroup;
        } else {
            Debug.LogWarning("No molecule loaded name '" + structureName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setStructureGroup(\"" + structureName + "\", " + newGroup + ")");
        UnityMolMain.recordUndoPythonCommand("setStructureGroup(\"" + structureName + "\", " + prevGroup + ")");

    }

    /// <summary>
    /// Delete a molecule and all its UnityMolSelection and UnityMolRepresentation
    /// </summary>
    public static void delete(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolStructure s = sm.GetStructure(structureName);

        if (s != null) { //Remove a complete loaded molecule
            sm.Delete(s);
            Debug.LogWarning("Deleting molecule '" + structureName + "'");
        } else {
            Debug.LogWarning("No molecule loaded name '" + structureName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("delete(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Show as 'type' all loaded molecules
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void show(string type) {

        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        RepType repType = getRepType(type);
        if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

            foreach (UnityMolStructure s in sm.loadedStructures) {
                List<UnityMolRepresentation> existingReps = repManager.representationExists(s.ToSelectionName(), repType);

                UnityMolSelection sel = null;
                if (!selM.selections.ContainsKey(s.ToSelectionName())) {
                    sel = s.currentModel.ToSelection();
                    selM.Add(sel);
                } else {
                    sel = selM.selections[s.ToSelectionName()];
                }

                if (existingReps == null) {
                    repManager.AddRepresentation(sel, repType.atomType, repType.bondType);
                } else {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.Show();
                    }
                }
            }
        } else {
            Debug.LogError("Wrong representation type");
            return;
        }

        UnityMolMain.recordPythonCommand("show(\"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("hide(\"" + type + "\")");
    }

    /// <summary>
    /// Show all loaded molecules only as the 'type' representation
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void showAs(string type) {

        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        RepType repType = getRepType(type);
        if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {
            //First hide all representations
            foreach (UnityMolStructure s in sm.loadedStructures) {
                foreach (UnityMolRepresentation r in s.representations) {
                    r.Hide();
                }
            }

            foreach (UnityMolStructure s in sm.loadedStructures) {

                UnityMolSelection sel = null;
                if (!selM.selections.ContainsKey(s.ToSelectionName())) {
                    sel = s.currentModel.ToSelection();
                    selM.Add(sel);
                } else {
                    sel = selM.selections[s.ToSelectionName()];
                }

                List<UnityMolRepresentation> existingReps = repManager.representationExists(s.ToSelectionName(), repType);

                if (existingReps == null) {
                    repManager.AddRepresentation(sel, repType.atomType, repType.bondType);
                } else {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.Show();
                    }
                }
            }
        } else {
            Debug.LogError("Wrong representation type");
            return;
        }

        UnityMolMain.recordPythonCommand("showAs(\"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("hide(\"" + type + "\")");
    }

    /// <summary>
    /// Restore all representations of a structure to the default representations
    /// </summary>
    public static void resetRep(string structureName) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        if (sm.nameToStructure.ContainsKey(structureName)) {
            UnityMolStructure s = sm.GetStructure(structureName);
            List<string> sels = new List<string>();
            foreach (UnityMolRepresentation r in s.representations) {
                if (!sels.Contains(r.selection.name)) {
                    sels.Add(r.selection.name);
                }
            }
            foreach (string sname in sels) {
                selM.Delete(sname);
                selM.RemoveSelectionKeyword(sname);
            }

            UnityMolSelection sel = s.ToSelection();
            defaultRep(sel.name);
        } else {
            Debug.LogWarning("No molecule loaded name '" + structureName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("resetRep(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Create selections and default representations: all in cartoon, not protein in hyperballs
    /// Also create a selection containing "not protein and not water and not ligand and not ions"
    /// </summary>
    public static bool defaultRep(string selName) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {

            UnityMolSelection sel = selM.selections[selName];
            if (sel == null || sel.structures == null || sel.structures.Count <= 0) {
                return false;
            }
            //Remove all previous representations
            selM.DeleteRepresentations(sel);

            RepType repType = getRepType("c");
            RepType repTypehb = getRepType("hb");
            RepType repTypel = getRepType("l");
            RepType repTypep = getRepType("p");

            //Create protein or nucleic selection and show as cartoon
            string sName = sel.structures[0].name;
            string ProtOrNucSelName = sName + "_protein_or_nucleic";
            if (selM.selections.ContainsKey(ProtOrNucSelName)) {
                selM.DeleteRepresentations(selM.selections[ProtOrNucSelName]);
            }

            MDAnalysisSelection selecprotornuc = new MDAnalysisSelection("protein or nucleic", sel.atoms);
            UnityMolSelection retprotornuc = selecprotornuc.process();
            retprotornuc.name = ProtOrNucSelName;
            if (retprotornuc.Count != 0) {
                selM.Add(retprotornuc);
                selM.AddSelectionKeyword(retprotornuc.name, retprotornuc.name);
                repManager.AddRepresentation(retprotornuc, repType.atomType, repType.bondType);
            }

            bool cartoonEmpty = false;
            //If the cartoon is empty => show has hyperball
            List<UnityMolRepresentation> existingReps = repManager.representationExists(retprotornuc.name, repType);
            try {
                if (existingReps.Last() != null) {
                    SubRepresentation sr = existingReps.Last().subReps.Last();
                    CartoonRepresentation rep = (CartoonRepresentation) sr.atomRep;
                    if (rep == null || rep.totalVertices == 0) {
                        cartoonEmpty = true;
                    }
                }
            } catch {
                cartoonEmpty = true;
            }

            if (cartoonEmpty && retprotornuc.Count != 0) {
                repManager.AddRepresentation(retprotornuc, repTypehb.atomType, repTypehb.bondType);
            }

            MDAnalysisSelection selecwat = new MDAnalysisSelection("water", sel.atoms);
            UnityMolSelection retwat = selecwat.process();
            retwat.name = sName + "_water";
            bool containsWat = retwat.Count != 0;
            if (containsWat) {
                selM.Add(retwat);

                if (retwat.bonds == null || retwat.bonds.Count == 0) {
                    repManager.AddRepresentation(retwat, repTypep.atomType, repTypep.bondType);
                } else {
                    repManager.AddRepresentation(retwat, repTypel.atomType, repTypel.bondType);
                }

            }

            //Create not protein/nucleic selection and show as hb if the cartoon was correctly shown
            string notPSelName = sName + "_not_protein_nucleic";
            if (selM.selections.ContainsKey(notPSelName)) {
                selM.DeleteRepresentations(selM.selections[notPSelName]);
            }
            MDAnalysisSelection selec = new MDAnalysisSelection("not protein and not nucleic" + (containsWat ? " and not water" : ""), sel.atoms);
            UnityMolSelection ret = selec.process();
            ret.name = notPSelName;
            bool shownNotProtAsHB = false;

            if (ret.Count != 0) {
                if (!cartoonEmpty) { //Show not protein as hb only if the cartoon was successfully shown
                    repManager.AddRepresentation(ret, repTypehb.atomType, repTypehb.bondType);
                    shownNotProtAsHB = true;
                }

                selM.Add(ret);
                selM.AddSelectionKeyword(ret.name, ret.name);
            }

            //Unknown atoms = not protein/nucleic/ligand/water/ions
            MDAnalysisSelection selecUnreco = new MDAnalysisSelection("not protein and not nucleic and not water and not ligand and not ions", sel.atoms);
            UnityMolSelection selUnreco = selecUnreco.process();
            selUnreco.name = sName + "_unrecognized_atoms";

            if (selUnreco.Count != 0) {
                selM.Add(selUnreco);
                selM.AddSelectionKeyword(selUnreco.name, selUnreco.name);
                if (cartoonEmpty && !shownNotProtAsHB) {
                    repManager.AddRepresentation(selUnreco, repTypehb.atomType, repTypehb.bondType);
                }
            }

            //If nothing was shown as hyperball or cartoon => show as hyperball
            if (ret.Count == 0 && cartoonEmpty && retprotornuc.Count != 0) {
                repManager.AddRepresentation(retprotornuc, repTypehb.atomType, repTypehb.bondType);
            }

            // selM.SetCurrentSelection(sel);
            // sel.mergeRepresentations(ret);
            selM.ClearCurrentSelection();
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return false;
        }
        return true;

    }

    /// <summary>
    /// Create default representations (cartoon for protein + HB for not protein atoms)
    /// </summary>
    public static void showDefault(string selName) {

        if (!defaultRep(selName)) {
            return;
        }

        UnityMolMain.recordPythonCommand("showDefault(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("hideSelection(\"" + selName + "\", \"c\")\nhideSelection(\"" + selName + "\", \"hb\")\nhideSelection(\"" + (selName + "_not_protein") + "\", \"hb\")");
    }

    /// <summary>
    /// Unhide all representations already created for a specified structure
    /// </summary>
    public static void showStructureAllRepresentations(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        if (sm.nameToStructure.ContainsKey(structureName)) {
            UnityMolStructure s = sm.GetStructure(structureName);
            foreach (UnityMolRepresentation r in s.representations) {
                r.Show();
            }
        } else {
            Debug.LogWarning("No molecule loaded name '" + structureName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("showStructureAllRepresentations(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("hideStructureAllRepresentations(\"" + structureName + "\")");
    }

    /// <summary>
    /// Show the selection as 'type'
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// If the representation is already there, update it if the selection content changed and show it
    /// Surface example: showSelection("all_1kx2", "s", True, True, True, SurfMethod.MSMS) # arguments are cutByChain, AO, cutSurface, computeSurfaceMethod
    /// Iso-surface example: showSelection("all_1kx2", "dxiso", last().dxr, 0.0f)
    /// </summary>
    public static void showSelection(string selName, string type, params object[] args) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);

            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                // List should be equal 1 always.
                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);

                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        if (repType.atomType != AtomType.noatom && sel.atoms.Count != existingRep.nbAtomsInRep) {
                            existingRep.updateWithNewSelection(sel);
                        } else if (repType.bondType != BondType.nobond && sel.bonds.Count != existingRep.nbBondsInRep) {
                            existingRep.updateWithNewSelection(sel);
                        }

                        existingRep.Show();
                    }
                } else {
                    repManager.AddRepresentation(sel, repType.atomType, repType.bondType, args);
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        string command = "showSelection(\"" + selName + "\", \"" + type + "\"";
        foreach (object o in args) {
            if (o is string) {
                command += ", \"" + o.ToString() + "\"";
            } else if (o is float) {
                command += ", " + ((float) o).ToString("f3", culture);
            } else if (o is Vector3) {
                command += ", " + cVec3ToPy((Vector3)o);
            } else if (o is SurfMethod) {
                command += ", SurfMethod." + o.ToString();
            }
            else {
                command += ", " + o.ToString();
            }
        }
        command += ")";
        UnityMolMain.recordPythonCommand(command);
        UnityMolMain.recordUndoPythonCommand("hideSelection(\"" + selName + "\", \"" + type + "\")");
    }


    /// <summary>
    /// Show all representations of the selection named 'selName'
    /// </summary>
    public static void showSelection(string selName) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];
            foreach (List<UnityMolRepresentation> lr in sel.representations.Values) {
                foreach (UnityMolRepresentation r in lr) {
                    r.Show();
                }
            }
        }
        else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("showSelection(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("hideSelection(\"" + selName + "\")");
    }

    /// <summary>
    /// Hide every representations of the specified selection
    /// </summary>
    public static void hideSelection(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];
            foreach (List<UnityMolRepresentation> lr in sel.representations.Values) {
                foreach (UnityMolRepresentation r in lr) {
                    r.Hide();
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("hideSelection(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("showSelection(\"" + selName + "\")");
    }

    /// <summary>
    /// Hide every representation of type 'type' of the specified selection
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void hideSelection(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                UnityMolSelection sel = selM.selections[selName];

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);

                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.Hide();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("hideSelection(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("showSelection(\"" + selName + "\", \"" + type + "\")");
    }

    /// <summary>
    /// Delete every representations of type 'type' of the specified selection
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void deleteRepresentationInSelection(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                UnityMolSelection sel = selM.selections[selName];

                selM.DeleteRepresentation(sel, repType);
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("deleteRepresentationInSelection(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("showSelection(\"" + selName + "\", \"" + type + "\")");
    }

    /// <summary>
    /// Delete every representations of the specified selection
    /// </summary>
    public static void deleteRepresentationsInSelection(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {

            UnityMolSelection sel = selM.selections[selName];

            selM.DeleteRepresentations(sel);
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("deleteRepresentationsInSelection(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Hide every representations of the specified structure
    /// </summary>
    public static void hideStructureAllRepresentations(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        if (sm.nameToStructure.ContainsKey(structureName)) {
            UnityMolStructure s = sm.GetStructure(structureName);
            foreach (UnityMolRepresentation r in s.representations) {
                r.Hide();
            }
        } else {
            Debug.LogWarning("No molecule loaded name '" + structureName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("hideStructureAllRepresentations(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("showStructureAllRepresentations(\"" + structureName + "\")");
    }
    /// <summary>
    /// Delete all the selection of the given structure
    /// </summary>
    public static void deleteAllSelectionsStructure(string structureName) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        if (sm.nameToStructure.ContainsKey(structureName)) {
            UnityMolStructure s = sm.GetStructure(structureName);
            List<string> selNames = new List<string>();

            foreach (UnityMolSelection sel in selM.selections.Values) {
                if (sel.structures.Contains(s)) {
                    selNames.Add(sel.name);
                }
            }

            for (int i = 0; i < selNames.Count; i++) {
                selM.Delete(selNames[i]);
                selM.RemoveSelectionKeyword(selNames[i]);
            }
        } else {
            Debug.LogWarning("No molecule loaded name '" + structureName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("deleteAllSelectionsStructure(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Utility function to test if a representation is shown for a specified structure
    /// </summary>
    public static bool areRepresentationsOn(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return false;
        }

        if (sm.nameToStructure.ContainsKey(structureName)) {
            UnityMolStructure s = sm.GetStructure(structureName);
            foreach (UnityMolRepresentation r in s.representations) {
                if (r.isActive()) {
                    return true;
                }
            }
        } else {
            Debug.LogWarning("No molecule loaded name '" + structureName + "'");
        }
        return false;
    }

    /// <summary>
    /// Utility function to test if a representation of type 'type' is shown for a specified selection
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static bool areRepresentationsOn(string selName, string type) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        if (existingRep.isActive()) {
                            return true;
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
        }
        return false;
    }

    /// <summary>
    /// Hide all representations of type 'type'
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void hide(string type) {

        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }
        RepType repType = getRepType(type);
        if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

            foreach (UnityMolStructure s in sm.loadedStructures) {
                foreach (UnityMolRepresentation rep in s.representations) {
                    if (rep.repType == repType) {
                        rep.Hide();
                    }
                }
            }
        } else {
            Debug.LogError("Wrong representation type");
            return;
        }

        UnityMolMain.recordPythonCommand("hide(\"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("show(\"" + type + "\")");
    }

    /// <summary>
    /// Switch between the 2 types of surface computation methods: EDTSurf and MSMS
    /// </summary>
    public static void switchSurfaceComputeMethod(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("s");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;
                        surfM.SwitchComputeMethod();
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("switchSurfaceComputeMethod(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("switchSurfaceComputeMethod(\"" + selName + "\")");
    }

    /// <summary>
    /// Switch between cut surface mode and no-cut surface mode
    /// </summary>
    public static void switchCutSurface(string selName, bool isCut) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("s");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;
                        surfM.SwitchCutSurface(isCut);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("switchCutSurface(\"" + selName + "\", " + isCut + ")");
        UnityMolMain.recordUndoPythonCommand("switchCutSurface(\"" + selName + "\", " + !isCut + ")");
    }

    /// <summary>
    /// Switch all surface representation in selection to a solid surface material
    /// </summary>
    public static void setSolidSurface(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        bool wasWireframe = false;
        bool wasTransparent = false;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("s");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;
                        if (surfM.isWireframe) {
                            wasWireframe = true;
                            surfM.SwitchWireframe();
                        } else if (surfM.isTransparent) {
                            wasTransparent = true;
                            surfM.SwitchTransparent();
                        }
                    }
                }
            }
            repType = getRepType("dxiso");

            existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;
                        if (surfM.isWireframe) {
                            wasWireframe = true;
                            surfM.SwitchWireframe();
                        } else if (surfM.isTransparent) {
                            wasTransparent = true;
                            surfM.SwitchTransparent();
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setSolidSurface(\"" + selName + "\")");

        if (wasTransparent) {
            UnityMolMain.recordUndoPythonCommand("setTransparentSurface(\"" + selName + "\")");
        } else if (wasWireframe) {
            UnityMolMain.recordUndoPythonCommand("setWireframeSurface(\"" + selName + "\")");
        } else {
            UnityMolMain.recordUndoPythonCommand("");
        }
    }

    /// <summary>
    /// Switch all surface representation in selection to a wireframe surface material when available
    /// </summary>
    public static void setWireframeSurface(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        bool wasSolid = false;
        bool wasTransparent = false;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("s");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;

                        if (surfM.isTransparent) {
                            wasTransparent = true;
                        }
                        if (!surfM.isTransparent && !surfM.isWireframe) {
                            wasSolid = true;
                        }
                        if (!surfM.isWireframe) {
                            surfM.SwitchWireframe();
                        }
                    }
                }
            }

            repType = getRepType("dxiso");

            existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;

                        if (surfM.isTransparent) {
                            wasTransparent = true;
                        }
                        if (!surfM.isTransparent && !surfM.isWireframe) {
                            wasSolid = true;
                        }
                        if (!surfM.isWireframe) {
                            surfM.SwitchWireframe();
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setWireframeSurface(\"" + selName + "\")");

        if (wasTransparent) {
            UnityMolMain.recordUndoPythonCommand("setTransparentSurface(\"" + selName + "\")");
        } else if (wasSolid) {
            UnityMolMain.recordUndoPythonCommand("setSolidSurface(\"" + selName + "\")");
        } else {
            UnityMolMain.recordUndoPythonCommand("");
        }
    }

    /// <summary>
    /// Switch all surface representation in selection to a transparent surface material
    /// </summary>
    public static void setTransparentSurface(string selName, float? alpha = null) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        bool wasSolid = false;
        bool wasWireframe = false;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("s");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;

                        if (!surfM.isTransparent && !surfM.isWireframe) {
                            wasSolid = true;
                        }
                        if (surfM.isWireframe) {
                            wasWireframe = true;
                            surfM.SwitchWireframe();
                        }
                        if (!surfM.isTransparent) {
                            surfM.SwitchTransparent();
                        }
                        if (alpha.HasValue)
                            surfM.SetAlpha(alpha.Value);
                    }
                }
            }

            repType = getRepType("dxiso");

            existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;

                        if (!surfM.isTransparent && !surfM.isWireframe) {
                            wasSolid = true;
                        }
                        if (surfM.isWireframe) {
                            wasWireframe = true;
                            surfM.SwitchWireframe();
                        }
                        if (!surfM.isTransparent) {
                            surfM.SwitchTransparent();
                        }
                        if (alpha.HasValue)
                            surfM.SetAlpha(alpha.Value);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 22 + selName.Length + 2;
        string command = "setTransparentSurface(\"" + selName;
        if (alpha.HasValue) {
            command += "\", " + alpha.Value.ToString("f2", culture) + ")";
        }
        else {
            command += "\")";
        }
        bool replaced = UnityMolMain.recordPythonCommand(command, true, lenSameCom);

        if (wasWireframe) {
            UnityMolMain.recordUndoPythonCommand("setWireframeSurface(\"" + selName + "\")", replaced);
        } else if (wasSolid) {
            UnityMolMain.recordUndoPythonCommand("setSolidSurface(\"" + selName + "\")", replaced);
        } else {
            UnityMolMain.recordUndoPythonCommand("", replaced);
        }
    }
    /// <summary>
    /// Switch cartoon material from transparent to normal/solid
    /// </summary>
    public static void setSolidCartoon(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        float prevAlpha = 0.3f;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("c");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolCartoonManager cM = (UnityMolCartoonManager) sr.atomRepManager;

                        prevAlpha = cM.curAlpha;
                        if (cM.isTransparent) {
                            cM.SwitchTransparent();
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setSolidCartoon(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("setTransparentCartoon(\"" + selName + "\", " + prevAlpha.ToString("f2", culture) + ")");
    }

    /// <summary>
    /// Switch cartoon material from normal/solid to transparent
    /// </summary>
    public static void setTransparentCartoon(string selName, float alpha = 0.3f) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        bool wasSolid = false;
        float prevAlpha = 0.3f;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("c");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolCartoonManager cM = (UnityMolCartoonManager) sr.atomRepManager;

                        prevAlpha = cM.curAlpha;

                        if (!cM.isTransparent) {
                            wasSolid = true;
                            cM.SwitchTransparent();
                        }
                        cM.SetAlpha(alpha);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 23 + selName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setTransparentCartoon(\"" + selName + "\", " + alpha.ToString("f2", culture) + ")", true, lenSameCom);
        if (wasSolid)
            UnityMolMain.recordUndoPythonCommand("setSolidCartoon(\"" + selName + "\")", replaced);
        else
            UnityMolMain.recordUndoPythonCommand("setTransparentCartoon(\"" + selName + "\", " + prevAlpha.ToString("f2", culture) + ")", replaced);
    }

    /// <summary>
    /// Switch sphere material from transparent to normal/solid
    /// </summary>
    public static void setSolidSphere(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        float prevAlpha = 0.3f;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("sphere");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSphereManager sM = (UnityMolSphereManager) sr.atomRepManager;

                        prevAlpha = sM.curAlpha;
                        if (sM.isTransparent) {
                            sM.SwitchTransparent();
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setSolidSphere(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("setTransparentSphere(\"" + selName + "\", " + prevAlpha.ToString("f2", culture) + ")");
    }

    public static void setTransparentSphere(string selName, float alpha = 0.3f) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        bool wasSolid = false;
        float prevAlpha = 0.3f;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("sphere");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSphereManager sM = (UnityMolSphereManager) sr.atomRepManager;

                        prevAlpha = sM.curAlpha;

                        if (!sM.isTransparent) {
                            wasSolid = true;
                            sM.SwitchTransparent();
                        }
                        sM.SetAlpha(alpha);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 21 + selName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setTransparentSphere(\"" + selName + "\", " + alpha.ToString("f2", culture) + ")", true, lenSameCom);
        if (wasSolid)
            UnityMolMain.recordUndoPythonCommand("setSolidSphere(\"" + selName + "\")", replaced);
        else
            UnityMolMain.recordUndoPythonCommand("setTransparentSphere(\"" + selName + "\", " + prevAlpha.ToString("f2", culture) + ")", replaced);
    }

    /// <summary>
    /// Recompute cartoon representation with new tube size
    /// </summary>
    public static void setTubeSizeCartoon(string selName, float newVal) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        float prevVal = 1.0f;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("c");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolCartoonManager cM = (UnityMolCartoonManager) sr.atomRepManager;

                        prevVal = cM.atomRep.customTubeSize;

                        cM.SetTubeSize(newVal);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 20 + selName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setTubeSizeCartoon(\"" + selName + "\", " + newVal.ToString("f2", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setTubeSizeCartoon(\"" + selName + "\", " + prevVal.ToString("f2", culture) + ")", replaced);
    }

    /// <summary>
    /// Draw cartoon representation as tube
    /// </summary>
    public static void drawCartoonAsTube(string selName, bool drawAsTube = true) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("c");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolCartoonManager cM = (UnityMolCartoonManager) sr.atomRepManager;

                        cM.DrawAsTube(drawAsTube);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("drawCartoonAsTube(\"" + selName + "\", " + cBoolToPy(drawAsTube) + ")");
        UnityMolMain.recordUndoPythonCommand("drawCartoonAsTube(\"" + selName + "\", " + cBoolToPy(!drawAsTube) + ")");
    }

    /// <summary>
    /// Draw cartoon representation as tube with Bfactor as tube size
    /// </summary>
    public static void drawCartoonAsBfactorTube(string selName, bool drawAsBTube = true) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("c");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolCartoonManager cM = (UnityMolCartoonManager) sr.atomRepManager;

                        cM.DrawAsBfactorTube(drawAsBTube);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("drawCartoonAsBfactorTube(\"" + selName + "\", " + cBoolToPy(drawAsBTube) + ")");
        UnityMolMain.recordUndoPythonCommand("drawCartoonAsBfactorTube(\"" + selName + "\", " + cBoolToPy(!drawAsBTube) + ")");
    }

    /// <summary>
    /// Recompute the DX surface with a new iso value
    /// </summary>
    public static void updateDXIso(string selName, float newVal) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        float prevVal = 0.0f;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("dxiso");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        prevVal = ((DXSurfaceRepresentation) sr.atomRep).isoValue;
                        ((DXSurfaceRepresentation) sr.atomRep).isoValue = newVal;
                        ((DXSurfaceRepresentation) sr.atomRep).recompute();
                    }
                }
            }
        }

        int lenSameCom = 13 + selName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("updateDXIso(\"" + selName + "\", " + newVal.ToString("F3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("updateDXIso(\"" + selName + "\", " + prevVal.ToString("F3", culture) + ")", replaced);
    }

    /// <summary>
    /// Change hyperball representation parameters in the specified selection to a preset
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void setSmoothness(string selName, string type, float val) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.SetSmoothness(val);
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.SetSmoothness(val);
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 15 + selName.Length + 4 + type.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setSmoothness(\"" + selName + "\", \"" + type + "\", " +
                        val.ToString("f2", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Change hyperball representation parameters in the specified selection to a preset
    /// Metaphores can be "Smooth", "Balls&Sticks", "VdW", "Licorice", "Hidden"
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void setMetal(string selName, string type, float val) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.SetMetal(val);
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.SetMetal(val);
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 10 + selName.Length + 4 + type.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setMetal(\"" + selName + "\", \"" + type + "\", " +
                        val.ToString("f2", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("", replaced);
    }

    /// <summary>
    /// Change surface wireframe size
    /// </summary>
    public static void setSurfaceWireframe(string selName, string type, float val) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                ((UnityMolSurfaceManager) sr.atomRepManager).SetWireframeSize(val);
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 21 + selName.Length + 4 + type.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setSurfaceWireframe(\"" + selName + "\", \"" + type + "\", " +
                        val.ToString("f2", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("", replaced);
    }
    /// <summary>
    /// Only show a part of the representation inside a sphere, only works with surface types for now
    /// </summary>
    public static void enableLimitedView(string selName, string type) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                if (repType.atomType == AtomType.surface || repType.atomType == AtomType.DXSurface)
                                    ((UnityMolSurfaceManager) sr.atomRepManager).activateLimitedView();
                                if (repType.atomType == AtomType.cartoon)
                                    ((UnityMolCartoonManager) sr.atomRepManager).activateLimitedView();
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("enableLimitedView(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("disableLimitedView(\"" + selName + "\", \"" + type + "\")");
    }

    /// <summary>
    /// Disable the limited view
    /// </summary>
    public static void disableLimitedView(string selName, string type) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                if (repType.atomType == AtomType.cartoon)
                                    ((UnityMolCartoonManager) sr.atomRepManager).disableLimitedView();
                                else
                                    ((UnityMolSurfaceManager) sr.atomRepManager).disableLimitedView();
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("disableLimitedView(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("enableLimitedView(\"" + selName + "\", \"" + type + "\")");
    }
    /// <summary>
    /// Get if the limited view is activated
    /// </summary>
    public static bool getLimitedView(string selName, string type) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                if (repType.atomType == AtomType.cartoon)
                                    return ((UnityMolCartoonManager) sr.atomRepManager).limitedView;
                                else
                                    return ((UnityMolSurfaceManager) sr.atomRepManager).limitedView;
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return false;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
        }
        return false;
    }

    /// <summary>
    /// Set the center of the limited view in local space
    /// </summary>
    public static void setLimitedViewCenter(string selName, string type, Vector3 center) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        Vector3 prevCenter = Vector3.zero;
        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                if (repType.atomType == AtomType.cartoon) {
                                    prevCenter = ((UnityMolCartoonManager) sr.atomRepManager).limitedViewCenter;
                                    ((UnityMolCartoonManager) sr.atomRepManager).setLimitedViewCenter(center);
                                }
                                else {
                                    prevCenter = ((UnityMolSurfaceManager) sr.atomRepManager).limitedViewCenter;
                                    ((UnityMolSurfaceManager) sr.atomRepManager).setLimitedViewCenter(center);
                                }
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand(String.Format(CultureInfo.InvariantCulture, "setLimitedViewCenter(\"{0}\", \"{1}\", Vector3{2})", selName, type, center));
        UnityMolMain.recordUndoPythonCommand(String.Format(CultureInfo.InvariantCulture, "setLimitedViewCenter(\"{0}\", \"{1}\", Vector3{2})", selName, type, prevCenter));
    }

    /// <summary>
    /// Retrieve the current center of the limited view
    /// </summary>
    public static Vector3 getLimitedViewCenter(string selName, string type) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        Vector3 prevCenter = Vector3.zero;
        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                if (repType.atomType == AtomType.cartoon) {
                                    prevCenter = ((UnityMolCartoonManager) sr.atomRepManager).limitedViewCenter;
                                    return ((UnityMolCartoonManager) sr.atomRepManager).limitedViewCenter;
                                }
                                else {
                                    prevCenter = ((UnityMolSurfaceManager) sr.atomRepManager).limitedViewCenter;
                                    return ((UnityMolSurfaceManager) sr.atomRepManager).limitedViewCenter;
                                }
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return Vector3.zero;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return Vector3.zero;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Set the radius (in Angstrom) of the limited view (surface or cartoon type)
    /// </summary>
    public static void setLimitedViewRadius(string selName, string type, float radius) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        float prevRadius = 1.0f;
        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                if (repType.atomType == AtomType.cartoon) {
                                    prevRadius = ((UnityMolCartoonManager) sr.atomRepManager).limitedViewRadius;
                                    ((UnityMolCartoonManager) sr.atomRepManager).setLimitedViewRadius(radius);
                                }
                                else {
                                    prevRadius = ((UnityMolSurfaceManager) sr.atomRepManager).limitedViewRadius;
                                    ((UnityMolSurfaceManager) sr.atomRepManager).setLimitedViewRadius(radius);
                                }
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        int lenSameCom = 22 + selName.Length + 4 + type.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setLimitedViewRadius(\"" + selName + "\", \"" + type + "\", " + radius.ToString("f2", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setLimitedViewRadius(\"" + selName + "\", \"" + type + "\", " + prevRadius.ToString("f2", culture) + ")", replaced);

    }
    /// <summary>
    /// Change hyperball representation parameters in all selections that contains a hb representation
    /// Metaphores can be "Smooth", "Balls&Sticks", "VdW", "Licorice", "Hidden"
    /// </summary>
    public static void setHyperBallMetaphore(string metaphore, bool forceAOOff = true, bool lerp = false, float duration = 0.5f) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        float scaleBond = 1.0f;
        float scaleAtom = 1.0f;
        float shrink = 0.1f;
        bool doAO = false;

        switch (metaphore) {
        case "Smooth":
        case "smooth":
            scaleAtom = 1.0f;
            scaleBond = 1.0f;
            shrink = 0.4f;
            break;
        case "Balls&Sticks":
        case "Ball&Stick":
        case "BallsAndSticks":
        case "Ballandstick":
        case "bas":
        case "ballsandsticks":
        case "ballandstick":
            scaleAtom = 0.5f;
            scaleBond = 0.2f;
            shrink = 0.001f;
            break;
        case "VdW":
        case "vdw":
        case "VDW":
            scaleAtom = 3.0f;
            scaleBond = 0.0f;
            shrink = 1.0f;
            if (!forceAOOff)
                doAO = true;
            break;
        case "Licorice":
        case "licorice":
            scaleAtom = 0.3f;
            scaleBond = 0.3f;
            shrink = 0.001f;
            break;
        case "Hidden":
        case "hidden":
        case "hide":
            scaleAtom = 0.0f;
            scaleBond = 0.0f;
            shrink = 1.0f;
            break;
        default:
            Debug.LogError("Metaphore not recognized");
            return;
        }


        RepType repType = getRepType("hb");

        if (lerp) {
            Vector3 prevParams = getHyperballParams();
            Vector3 targetParams = new Vector3(scaleAtom, scaleBond, shrink);
            instance.setHyperballParam("", prevParams, targetParams, duration);
        }
        else {
            foreach (string selName in selM.selections.Keys) {
                UnityMolSelection sel = selM.selections[selName];

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                            UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;
                            if (hsManager != null) {
                                hsManager.SetShrink(shrink);
                                hsManager.SetSizes(sel.atoms, scaleBond);
                            }

                            hbManager.SetSizes(sel.atoms, scaleAtom);
                            if (doAO) {
                                hbManager.computeAO();
                            }
                        }
                    }
                }
            }
        }

        UnityMolMain.recordPythonCommand("setHyperBallMetaphore(\"" + metaphore + "\", " + cBoolToPy(forceAOOff) + ", " + cBoolToPy(lerp) + ", " + duration.ToString("f3", culture) + ")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    private static Vector3 getHyperballParams(string selName = "") {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        RepType repType = getRepType("hb");

        Vector3 res = new Vector3(1.0f, 1.0f, 0.1f);
        if (selName == "") {
            foreach (string seln in selM.selections.Keys) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(seln, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                            UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;
                            if (hsManager != null) {
                                res.y = hsManager.scaleBond;
                                res.z = hsManager.shrink;
                            }
                            if (hbManager != null) {
                                res.x = hbManager.lastScale;
                            }
                            if (hsManager != null && hbManager != null) {
                                return res;
                            }
                        }
                    }
                }
            }
        }
        else {
            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                        UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;
                        if (hsManager != null) {
                            res.y = hsManager.scaleBond;
                            res.z = hsManager.shrink;
                        }
                        if (hbManager != null) {
                            res.x = hbManager.lastScale;
                        }
                        if (hsManager != null && hbManager != null) {
                            return res;
                        }
                    }
                }
            }
        }
        return res;
    }

    public void setHyperballParam(string selName, Vector3 prevScaleShrink, Vector3 scaleShrink, float duration) {
        StartCoroutine(delayedSetHyperballParam(selName, prevScaleShrink, scaleShrink, duration));
    }

    IEnumerator delayedSetHyperballParam(string selName, Vector3 prevScaleShrink, Vector3 scaleShrink, float duration) {
        //End of frame
        yield return 0;

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        RepType repType = getRepType("hb");

        //Set everything to starting scales and shrink
        if (selName == "") {
            foreach (string seln in selM.selections.Keys) {
                UnityMolSelection sel = selM.selections[seln];

                List<UnityMolRepresentation> existingReps = repManager.representationExists(seln, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                            UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;
                            if (hsManager != null) {
                                hsManager.SetShrink(prevScaleShrink.z);
                                hsManager.SetSizes(sel.atoms, prevScaleShrink.y);
                            }

                            hbManager.SetSizes(sel.atoms, prevScaleShrink.x);
                        }
                    }
                }
            }
        }

        float multi = 1.0f / duration;
        float ratio = 0.0f;
        Vector3 current = prevScaleShrink;
        while (current != scaleShrink) {
            ratio += Time.deltaTime * multi;
            current = Vector3.Lerp(prevScaleShrink, scaleShrink, ratio);

            if (selName == "") {

                foreach (string sname in selM.selections.Keys) {
                    UnityMolSelection sel = selM.selections[sname];

                    List<UnityMolRepresentation> existingReps = repManager.representationExists(sname, repType);
                    if (existingReps != null) {
                        foreach (UnityMolRepresentation existingRep in existingReps) {
                            foreach (SubRepresentation sr in existingRep.subReps) {
                                UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                                UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;
                                if (hsManager != null) {
                                    hsManager.SetShrink(current.z);
                                    hsManager.SetSizes(sel.atoms, current.y);
                                }
                                hbManager.SetSizes(sel.atoms, current.x);
                            }
                        }
                    }
                }
            }
            else {
                UnityMolSelection sel = selM.selections[selName];

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                            UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;
                            if (hsManager != null) {
                                hsManager.SetShrink(current.z);
                                hsManager.SetSizes(sel.atoms, current.y);
                            }
                            hbManager.SetSizes(sel.atoms, current.x);
                        }
                    }
                }
            }

            yield return 0;
        }
    }

    /// <summary>
    /// Change hyperball representation parameters in the specified selection to a preset
    /// Metaphores can be "Smooth", "Balls&Sticks", "VdW", "Licorice", "Hidden"
    /// </summary>
    public static void setHyperBallMetaphore(string selName, string metaphore, bool forceAOOff = true, bool lerp = false, float duration = 0.5f) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        float scaleBond = 1.0f;
        float scaleAtom = 1.0f;
        float shrink = 0.1f;
        bool doAO = false;

        switch (metaphore) {
        case "Smooth":
        case "smooth":
            scaleAtom = 1.0f;
            scaleBond = 1.0f;
            shrink = 0.4f;
            break;
        case "Balls&Sticks":
        case "Ball&Stick":
        case "BallsAndSticks":
        case "Ballandstick":
        case "bas":
        case "ballsandsticks":
        case "ballandstick":
            scaleAtom = 0.5f;
            scaleBond = 0.2f;
            shrink = 0.001f;
            break;
        case "VdW":
        case "vdw":
        case "VDW":
            scaleAtom = 3.0f;
            scaleBond = 0.0f;
            shrink = 1.0f;
            if (!forceAOOff)
                doAO = true;
            break;
        case "Licorice":
        case "licorice":
            scaleAtom = 0.3f;
            scaleBond = 0.3f;
            shrink = 0.001f;
            break;
        case "Hidden":
        case "hidden":
        case "hide":
            scaleAtom = 0.0f;
            scaleBond = 0.0f;
            shrink = 1.0f;
            break;
        default:
            Debug.LogError("Metaphore not recognized");
            return;
        }

        if (selM.selections.ContainsKey(selName)) {
            if (lerp) {
                Vector3 prevParams = getHyperballParams(selName);
                Vector3 targetParams = new Vector3(scaleAtom, scaleBond, shrink);
                instance.setHyperballParam(selName, prevParams, targetParams, duration);
            }
            else {
                UnityMolSelection sel = selM.selections[selName];

                RepType repType = getRepType("hb");

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                            UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;
                            if (hsManager != null) {
                                hsManager.SetShrink(shrink);
                                hsManager.SetSizes(sel.atoms, scaleBond);
                            }

                            hbManager.SetSizes(sel.atoms, scaleAtom);
                            if (doAO) {
                                hbManager.computeAO();
                            }
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setHyperBallMetaphore(\"" + selName + "\", \"" + metaphore + "\", " + cBoolToPy(forceAOOff) + ", " + cBoolToPy(lerp) + ", " + duration.ToString("f3", culture) + ")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Set shininess for the hyperball representations of the specified selection
    /// </summary>
    public static void setHyperBallShininess(string selName, float shin) {

        float prev = 0.0f;

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("hb");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {

                        UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                        UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;

                        prev = hbManager.shininess;

                        hbManager.SetShininess(shin);
                        hsManager.SetShininess(shin);
                    }
                }
            }

        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 23 + selName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setHyperBallShininess(\"" + selName + "\", " +
                        shin.ToString("f3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setHyperBallShininess(\"" + selName + "\", " +
                                             prev.ToString("f3", culture) + ")", replaced);
    }

    /// <summary>
    /// Set the shrink factor for the hyperball representations of the specified selection
    /// </summary>
    public static void setHyperballShrink(string selName, float shrink) {

        float prev = 0.0f;

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("hb");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {

                        UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;

                        prev = hsManager.shrink;

                        hsManager.SetShrink(shrink);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 20 + selName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setHyperballShrink(\"" + selName + "\", " +
                        shrink.ToString("f3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setHyperballShrink(\"" + selName + "\", " +
                                             prev.ToString("f3", culture) + ")", replaced);
    }

    /// <summary>
    /// Change all hyperball representation in the selection with a new texture mapped
    /// idTex of the texture is the index in UnityMolMain.atomColors.textures
    /// </summary>
    public static void setHyperballTexture(string selName, int idTex) {

        if (idTex >= UnityMolMain.atomColors.textures.Length) {
            Debug.LogError("Invalid Texture index " + idTex + " " + UnityMolMain.atomColors.textures.Length);
            return;
        }

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("hb");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;
                        UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                        hbManager.SetTexture(idTex);
                        hsManager.SetTexture(idTex);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setHyperballTexture(\"" + selName + "\", " + idTex + ")");
        UnityMolMain.recordUndoPythonCommand("setHyperballTexture(\"" + selName + "\", 0)");
    }

    /// <summary>
    /// Change all bond order representation in the selection with a new texture mapped
    /// idTex of the texture is the index in UnityMolMain.atomColors.textures
    /// </summary>
    public static void setBondOrderTexture(string selName, int idTex) {

        if (idTex >= UnityMolMain.atomColors.textures.Length) {
            Debug.LogError("Invalid Texture index " + idTex + " " + UnityMolMain.atomColors.textures.Length);
            return;
        }

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("bondorder");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                Texture tex = null;
                if (idTex >= 0) {
                    tex = (Texture) UnityMolMain.atomColors.textures[idTex];
                }
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolAtomBondOrderManager hbManager = (UnityMolAtomBondOrderManager) sr.atomRepManager;
                        UnityMolBondBondOrderManager hsManager = (UnityMolBondBondOrderManager) sr.bondRepManager;
                        hbManager.SetTexture(tex);
                        hsManager.SetTexture(tex);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setBondOrderTexture(\"" + selName + "\", " + idTex + ")");
        UnityMolMain.recordUndoPythonCommand("setBondOrderTexture(\"" + selName + "\", 0)");
    }
    /// <summary>
    /// Remove AO from hyperballs
    /// </summary>
    public static void clearHyperballAO(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("hb");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolHBallMeshManager hbManager = (UnityMolHBallMeshManager) sr.atomRepManager;
                        hbManager.cleanAO();
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("clearHyperballAO(\"" + selName + "\"s)");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Compute object space AO for surface
    /// </summary>
    public static void computeSurfaceAO(string selName, string type) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {
                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            UnityMolSurfaceManager sMana = (UnityMolSurfaceManager) sr.atomRepManager;
                            sMana.DoAO();
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("computeSurfaceAO(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("clearSurfaceAO(\"" + selName + "\", \"" + type + "\")");
    }

    /// <summary>
    /// Remove AO for surface
    /// </summary>
    public static void clearSurfaceAO(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            UnityMolSurfaceManager sMana = (UnityMolSurfaceManager) sr.atomRepManager;
                            sMana.ClearAO();
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("clearSurfaceAO(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("computeSurfaceAO(\"" + selName + "\", \"" + type + "\")");
    }

    /// <summary>
    /// Get statut of AO surface
    /// </summary>
    public static bool isSurfaceAOOn(string selName, string type) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            UnityMolSurfaceManager sMana = (UnityMolSurfaceManager) sr.atomRepManager;
                            return sMana.AOOn;
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return false;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
        }
        return false;
    }

    /// <summary>
    /// Set ambient light intensity
    /// </summary>
    public static void setAmbientLightIntensity(float i) {
        float prevI = UnityMolMain.ambientLightScale;
        UnityMolMain.ambientLightScale = i;

        RenderSettings.ambientLight = (UnityMolMain.initAmbientColor * i);

        if (UnityMolMain.raytracingMode) {
            RaytracerManager.Instance.setAmbientLight(i * 0.2f);
        }

        int lenSameCom = 25;
        bool replaced = UnityMolMain.recordPythonCommand(String.Format(CultureInfo.InvariantCulture, "setAmbientLightIntensity({0})", i), true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand(String.Format(CultureInfo.InvariantCulture, "setAmbientLightIntensity({0})", prevI), replaced);
    }

    ///<summary>
    /// Set light intensity of all directional lights found in the scene
    ///</summary>
    public static void setDirLightIntensity(float v) {
        Light[] lights = FindObjectsOfType<Light>();
        float prevI = 1.0f;
        foreach (Light l in lights) {
            if (l.type == LightType.Directional) {
                prevI = l.intensity;
                l.intensity = v;
            }
        }
        int lenSameCom = 21;
        bool replaced = UnityMolMain.recordPythonCommand(String.Format(CultureInfo.InvariantCulture, "setDirLightIntensity({0})", v), true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand(String.Format(CultureInfo.InvariantCulture, "setDirLightIntensity({0})", prevI), replaced);
    }

    ///<summary>
    /// Set light shadow strength of all directional lights found in the scene
    /// 0 is no shadow at all, 1 is full black shadow
    ///</summary>
    public static void setDirLightShadow(float v) {
        Light[] lights = FindObjectsOfType<Light>();
        float prevI = 1.0f;
        foreach (Light l in lights) {
            if (l.type == LightType.Directional) {
                prevI = l.shadowStrength;
                l.shadowStrength = v;
            }
        }
        int lenSameCom = 18;
        bool replaced = UnityMolMain.recordPythonCommand(String.Format(CultureInfo.InvariantCulture, "setDirLightShadow({0})", v), true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand(String.Format(CultureInfo.InvariantCulture, "setDirLightShadow({0})", prevI), replaced);
    }

    ///<summary>
    /// Set light direction in X of all directional lights found in the scene
    ///</summary>
    public static void setDirLightDirection(Vector3 eulers) {
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light l in lights) {
            if (l.type == LightType.Directional) {
                l.transform.rotation = Quaternion.Euler(eulers);
            }
        }

        int lenSameCom = 21;
        bool replaced = UnityMolMain.recordPythonCommand(String.Format(CultureInfo.InvariantCulture, "setDirLightDirection({0})", eulers), true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("", replaced);
    }

    ///<summary>
    /// Set light color of all directional lights found in the scene
    ///</summary>
    public static void setDirLightColor(Color c) {
        Light[] lights = FindObjectsOfType<Light>();
        Color prevCol = Color.white;
        foreach (Light l in lights) {
            if (l.type == LightType.Directional) {
                prevCol = l.color;
                l.color = c;
            }
        }
        int lenSameCom = 17;
        bool replaced = UnityMolMain.recordPythonCommand(String.Format(CultureInfo.InvariantCulture, "setDirLightColor({0})", c), true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand(String.Format(CultureInfo.InvariantCulture, "setDirLightColor({0})", prevCol), replaced);
    }

    /// <summary>
    /// Set the color of the cartoon representation of the specified selection based on the nature of secondary structure assigned
    /// ssType can be "helix", "sheet" or "coil"
    /// </summary>
    public static void setCartoonColorSS(string selName, string ssType, Color col) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        Color32 newCol = col;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("c");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {

                        if (sr.atomRepManager != null) {
                            UnityMolCartoonManager cManager = (UnityMolCartoonManager) sr.atomRepManager;
                            if (ssType == "helix") {
                                MDAnalysisSelection selec = new MDAnalysisSelection("ss " + ssType, sel.atoms);
                                UnityMolSelection selSS = selec.process();
                                cManager.SetColor(newCol, selSS);
                            }
                            if (ssType == "sheet") {
                                MDAnalysisSelection selec = new MDAnalysisSelection("ss " + ssType, sel.atoms);
                                UnityMolSelection selSS = selec.process();
                                cManager.SetColor(newCol, selSS);
                            }
                            if (ssType == "coil") {
                                MDAnalysisSelection selec = new MDAnalysisSelection("ss " + ssType, sel.atoms);
                                UnityMolSelection selSS = selec.process();
                                cManager.SetColor(newCol, selSS);
                            }
                            sr.atomRep.colorationType = colorType.custom;
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 19 + selName.Length + 4 + ssType.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand(String.Format(CultureInfo.InvariantCulture, "setCartoonColorSS(\"{0}\", \"{1}\", {2})", selName, ssType, newCol), true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("", replaced);
    }

    /// <summary>
    /// Change the size of the representation of type 'type' in the selection
    /// Mainly used for hyperball representation
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void setRepSize(string selName, string type, float size) {


        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.SetSizes(sel.atoms, size);
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.SetSizes(sel.atoms, size);
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 12 + selName.Length + 4 + type.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setRepSize(\"" + selName + "\", \"" + type + "\", " + size.ToString("f2", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setRepSize(\"" + selName + "\", \"" + type + "\", 1.0)", replaced);

    }

    /// <summary>
    /// Change the color of all representation of type 'type' in selection
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void colorSelection(string selName, string type, Color col) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        Color32 newCol = col;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.SetColor(newCol, sel);
                                sr.atomRep.colorationType = colorType.full;
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.SetColor(newCol, sel);
                                sr.bondRep.colorationType = colorType.full;
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        string command = String.Format(CultureInfo.InvariantCulture, "colorSelection(\"{0}\", \"{1}\", {2})", selName, type, col);
        int lenSameCom = command.Length - (String.Format("{0}", newCol).Length + 2);
        bool replaced = UnityMolMain.recordPythonCommand(command, true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("", replaced);

    }

    /// <summary>
    /// Change the color of all representation of type 'type' in selection
    /// colorS can be "black", "white", "yellow", "green", "red", "blue", "pink", "gray"
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void colorSelection(string selName, string type, string colorS) {

        colorS = colorS.ToLower();
        Color col = strToColor(colorS);
        colorSelection(selName, type, col);

        UnityMolMain.recordPythonCommand("colorSelection(\"" + selName + "\", \"" + type + "\", \"" + colorS + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Change the color of all representation of type 'type' in selection
    /// colors is a list of colors the length of the selection named selName
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void colorSelection(string selName, string type, List<Color32> colors) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];
            if (sel.atoms.Count != colors.Count) {
                Debug.LogError("Length of the 'colors' parameter does not have the length of the selection");
                return;
            }

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.SetColors(sel.atoms, colors);
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRep.colorationType = colorType.custom;
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRep.colorationType = colorType.custom;
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        string command = "colorSelection(\"" + selName + "\", \"" + type + "\", [";
        for (int i = 0; i < colors.Count; i++) {
            Color col = colors[i];
            command += "Color(" + col.r.ToString("F3", culture) + ", " +
                       col.g.ToString("F3", culture) + ", " +
                       col.b.ToString("F3", culture) + ", " +
                       col.a.ToString("F3", culture) + ")";
            if (i != colors.Count - 1) {
                command += ", ";
            }
        }
        command += "])";

        UnityMolMain.recordPythonCommand(command);
        UnityMolMain.recordUndoPythonCommand("");
    }


    /// <summary>
    /// Change the color of all representation in selection
    /// </summary>
    public static void colorSelection(string selName, Color col) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        Color32 newCol = col;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            foreach (List<UnityMolRepresentation> reps in sel.representations.Values) {
                foreach (UnityMolRepresentation r in reps) {
                    foreach (SubRepresentation sr in r.subReps) {
                        if (sr.atomRepManager != null) {
                            sr.atomRepManager.SetColor(newCol, sel);
                            sr.atomRep.colorationType = colorType.full;
                        }
                        if (sr.bondRepManager != null) {
                            sr.bondRepManager.SetColor(newCol, sel);
                            sr.bondRep.colorationType = colorType.full;
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        string command = String.Format(CultureInfo.InvariantCulture, "colorSelection(\"{0}\", {1})", selName, newCol);
        int lenSameCom = command.Length - (String.Format("{0}", col).Length + 2);
        bool replaced = UnityMolMain.recordPythonCommand(command, true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("", replaced);
    }

    /// <summary>
    /// Using a selection query, change the color of all representations in selection if type is not specified or all representations of type "type" in selection
    /// Only atoms of the result of the selection query will be colored
    /// </summary>
    public static void colorSelection(string selName, Color col, string selQuery, string type = "") {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        Color32 newCol = col;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            MDAnalysisSelection selec = new MDAnalysisSelection(selQuery, sel.atoms);
            UnityMolSelection selRes = selec.process();


            if (type == "") {//No rep type specified => color all the representations of the selection
                foreach (List<UnityMolRepresentation> reps in sel.representations.Values) {
                    foreach (UnityMolRepresentation r in reps) {
                        foreach (SubRepresentation sr in r.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.SetColor(newCol, selRes);
                                sr.atomRep.colorationType = colorType.custom;
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.SetColor(newCol, selRes);
                                sr.bondRep.colorationType = colorType.custom;
                            }
                        }
                    }
                }
            }
            else {
                RepType repType = getRepType(type);
                if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                    List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                    if (existingReps != null) {
                        foreach (UnityMolRepresentation existingRep in existingReps) {
                            foreach (SubRepresentation sr in existingRep.subReps) {
                                if (sr.atomRepManager != null) {
                                    sr.atomRepManager.SetColor(newCol, selRes);
                                    sr.atomRep.colorationType = colorType.custom;
                                }
                                if (sr.bondRepManager != null) {
                                    sr.bondRepManager.SetColor(newCol, selRes);
                                    sr.bondRep.colorationType = colorType.custom;
                                }
                            }
                        }
                    }
                } else {
                    Debug.LogError("Wrong representation type");
                    return;
                }
            }
        }
        else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        string command = String.Format(CultureInfo.InvariantCulture, "colorSelection(\"{0}\", {1}, \"{2}\", \"{3}\")", selName, newCol, selQuery, type);
        int lenSameCom = 15 + 2 + selName.Length;
        bool replaced = UnityMolMain.recordPythonCommand(command, true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("", replaced);
    }

    /// <summary>
    /// Reset the color of all representation of type 'type' in selection to the default value
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void resetColorSelection(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.ResetColors();
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.ResetColors();
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("resetColorSelection(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");

    }

    /// <summary>
    /// In the representation of type repType, color all atoms of type atomType in the selection selName with
    /// </summary>
    public static void colorAtomType(string selName, string repType, string atomType, Color col) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        Color32 newCol = col;

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType rt = getRepType(repType);
            if (rt.atomType != AtomType.noatom || rt.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, rt);
                if (existingReps != null) {

                    //Use MDASelection to benefit from the wildcard
                    MDAnalysisSelection selec = new MDAnalysisSelection("type " + atomType, sel.atoms);
                    UnityMolSelection selRes = selec.process();


                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.SetColors(newCol, selRes.atoms);
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.SetColors(newCol, selRes.atoms);
                            }
                        }
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("colorAtomType(\"" + selName + "\", \"" + repType + "\", \"" + atomType + "\", " +
                                         String.Format(CultureInfo.InvariantCulture, "{0})", newCol));
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Use the color palette to color representations of type 'type' in the selection 'selName' by chain
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void colorByChain(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorByChain();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByChain(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    // /// <summary>
    // /// Use the color palette to color representations of type 'type' in the selection 'selName' by model
    // /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    // /// </summary>
    // public static void colorByModel(string selName, string type) {

    //     UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
    //     UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

    //     if (selM.selections.ContainsKey(selName)) {
    //         UnityMolSelection sel = selM.selections[selName];

    //         RepType repType = getRepType(type);
    //         if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

    //             List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
    //             if (existingReps != null) {
    //                 foreach (UnityMolRepresentation existingRep in existingReps) {
    //                     existingRep.ColorByModel();
    //                 }
    //             }
    //         }
    //         else {
    //             Debug.LogError("Wrong representation type");
    //             return;
    //         }
    //     }
    //     else {
    //         Debug.LogWarning("No selection named '" + selName + "'");
    //         return;
    //     }

    //     UnityMolMain.recordPythonCommand("colorByModel(\"" + selName + "\", \"" + type + "\")");
    //     UnityMolMain.recordUndoPythonCommand("");
    // }

    /// <summary>
    /// Use the color palette to color representations of type 'type' in the selection 'selName' by residue
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void colorByResidue(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorByResidue();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByResidue(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");

    }

    /// <summary>
    /// Color representations of type 'type' in the selection 'selName' by atom
    /// </summary>
    public static void colorByAtom(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorByAtom();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByAtom(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Color representations of type 'type' in the selection 'selName' by hydrophobicity
    /// </summary>
    public static void colorByHydrophobicity(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorByHydro();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByHydrophobicity(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Use the color palette to color representations of type 'type' in the selection 'selName' by residue id
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void colorByResid(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorByResid();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByResid(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");

    }

    /// <summary>
    /// Use the color palette to color representations of type 'type' in the selection 'selName' by residue resnum
    /// type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
    /// </summary>
    public static void colorByResnum(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorByResnum();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByResnum(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");

    }

    /// <summary>
    /// Color representations of type 'type' in the selection 'selName' by sequence (rainbow effect)
    /// </summary>
    public static void colorBySequence(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorBySequence();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorBySequence(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Use the dx map to color by charge around atoms
    /// Only works for surface for now
    /// If normalizeDensity is set to true, the density values will be normalized
    /// if it is set to true, the default -10|10 range is used
    /// </summary>
    public static void colorByCharge(string selName, bool normalizeDensity = false, float minDens = -10.0f, float maxDens = 10.0f) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("s");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;
                        surfM.ColorByCharge(normalizeDensity, minDens, maxDens); //Use default -10 | 10 range to show charge
                    }
                }
            }
            repType = getRepType("dxiso");

            existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;
                        surfM.ColorByCharge(normalizeDensity, minDens, maxDens); //Use default -10 | 10 range to show charge
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByCharge(\"" + selName + "\", " +
                                         cBoolToPy(normalizeDensity) + ", " +
                                         minDens.ToString("F3", culture) + ", " +
                                         maxDens.ToString("F3", culture) + ")");

        UnityMolMain.recordUndoPythonCommand("");

    }

    /// <summary>
    /// Color residues by "restype": negatively charge = red, positively charged = blue, nonpolar = light yellow,
    /// polar = green, cys = orange
    /// </summary>
    public static void colorByResidueType(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorByResType();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByResidueType(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Color residues by "restype": negatively charge = red, positively charged = blue, neutral = white
    /// </summary>
    public static void colorByResidueCharge(string selName, string type) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorByResCharge();
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByResidueCharge(\"" + selName + "\", \"" + type + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Color residues by Bfactor
    /// </summary>
    public static void colorByBfactor(string selName, string type, Color startColor, Color midColor, Color endColor) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ColorByBfactor(startColor, midColor, endColor);
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("colorByBfactor(\"" + selName + "\", \"" + type + "\", " +
                                         String.Format(CultureInfo.InvariantCulture, " {0}, {1}, {2})", startColor, midColor, endColor));
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Color residues by Bfactor: low to high = blue to red
    /// </summary>
    public static void colorByBfactor(string selName, string type) {
        colorByBfactor(selName, type, Color.blue, Color.yellow, Color.red);
    }

    /// <summary>
    /// Set size of the line representation
    /// </summary>
    public static void setLineSize(string selName, float val) {

        RepType repType = getRepType("l");

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolBondLineManager lM = (UnityMolBondLineManager) sr.bondRepManager;
                        lM.SetWidth(val);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 13 + selName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setLineSize(\"" + selName + "\", " + val.ToString("F3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("", replaced);
    }

    /// <summary>
    /// Set size of the trace representation
    /// </summary>
    public static void setTraceSize(string selName, float val) {

        RepType repType = getRepType("trace");

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolTubeManager tM = (UnityMolTubeManager) sr.atomRepManager;
                        tM.SetWidth(val);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        int lenSameCom = 14 + selName.Length + 2;
        bool replaced = UnityMolMain.recordPythonCommand("setTraceSize(\"" + selName + "\", " + val.ToString("F3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("", replaced);
    }

    /// <summary>
    /// Change sheherasade computation method
    /// </summary>
    public static void switchSheherasadeMethod(string selName) {
        RepType repType = getRepType("sheherasade");

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSheherasadeManager sM = (UnityMolSheherasadeManager) sr.atomRepManager;
                        sM.SetSheherasadeForm(!sM.atomRep.bezier);
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("switchSheherasadeMethod(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("switchSheherasadeMethod(\"" + selName + "\")");
    }

    /// <summary>
    /// Set sheherasade texture
    /// </summary>
    public static void setSheherasadeTexture(string selName, int idTex) {
        RepType repType = getRepType("sheherasade");

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSheherasadeManager sM = (UnityMolSheherasadeManager) sr.atomRepManager;
                        if (idTex >= 0) {
                            sM.SetTexture(Sheherasade.arrowTexture);
                        } else {
                            sM.SetTexture(null);
                        }
                    }
                }
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setSheherasadeTexture(\"" + selName + "\", " + idTex + ")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Offsets all representations to center the structure 'structureName'
    /// Instead of moving the camera, move the loaded molecules to center them on the center of the camera
    /// </summary>
    public static void centerOnStructure(string structureName, bool lerp = false, bool recordCommand = true) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }
        UnityMolStructure s = null;

        if (sm.nameToStructure.ContainsKey(structureName)) {
            s = sm.GetStructure(structureName);
        } else {
            Debug.LogWarning("No molecule loaded name '" + structureName + "'");
            return;
        }

        ManipulationManager mm = getManipulationManager();

        if (mm != null) {
            mm.centerOnStructure(s, lerp);
        }

        if (recordCommand) {
            UnityMolMain.recordPythonCommand("centerOnStructure(\"" + structureName + "\", " + cBoolToPy(lerp) + ")");
            UnityMolMain.recordUndoPythonCommand("");
        }

    }
    /// <summary>
    /// Get the current ManipulationManager, creates one if there is none
    /// </summary>
    public static ManipulationManager getManipulationManager() {
        var foundObjects = FindObjectsOfType<ManipulationManager>();
        ManipulationManager mm = null;

        if (foundObjects.Length > 0) {
            mm = foundObjects[0].GetComponent<ManipulationManager>();
        } else {
#if UNITY_EDITOR
            Debug.Log("No manipulation manager found, creating a new one");
#endif
            mm = UnityMolMain.getRepresentationParent().AddComponent<ManipulationManager>();
        }
        return mm;
    }

    /// <summary>
    /// Offsets all representations to center the selection 'selName'
    /// If lerp is true and duration is > 0, centering is done during 'duration' seconds
    /// </summary>
    public static void centerOnSelection(string selName, bool lerp = false, float distance = -1.0f, float duration = 0.25f) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        if (!selM.selections.ContainsKey(selName)) {
            Debug.LogError("Selection '" + selName + "' does not exist");
            return;
        }
        UnityMolSelection sel = selM.selections[selName];

        ManipulationManager mm = getManipulationManager();

        if (mm != null) {
            mm.centerOnSelection(sel, lerp, distance, duration);
        }

        UnityMolMain.recordPythonCommand("centerOnSelection(\"" + selName + "\", " + cBoolToPy(lerp) + ", " + distance.ToString("F4", culture) + ", " + duration.ToString("F4", culture) + ")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// CEAlign algorithm to align two proteins with "little to no sequence similarity", only uses Calpha atoms
    /// For more details: https://pymolwiki.org/index.php/Cealign
    /// </summary>
    public static void cealign(string selNameTarget, string selNameMobile) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        if (!selM.selections.ContainsKey(selNameTarget)) {
            Debug.LogError("Selection '" + selNameTarget + "' does not exist");
            return;
        }
        if (!selM.selections.ContainsKey(selNameMobile)) {
            Debug.LogError("Selection '" + selNameMobile + "' does not exist");
            return;
        }
        UnityMolSelection selTar = selM.selections[selNameTarget];
        UnityMolSelection selMob = selM.selections[selNameMobile];

        CEAlignWrapper.alignWithCEAlign(selTar, selMob);

        UnityMolMain.recordPythonCommand("cealign(\"" + selNameTarget + "\", \"" + selNameMobile + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Create a UnityMolSelection based on MDAnalysis selection language (https://www.mdanalysis.org/docs/documentation_pages/selections.html)
    /// Returns a UnityMolSelection object, adding it to the selection manager if createSelection is true
    /// If a selection with the same name already exists and addToExisting is true, add atoms to the already existing selection
    /// Set forceCreate to true if the selection is empty but still need to generate the selection
    /// </summary>
    public static UnityMolSelection select(string selMDA, string name = "selection", bool createSelection = true, bool addToExisting = false, bool silent = false, bool setAsCurrentSelection = true, bool forceCreate = false, bool allModels = false, bool addToSelectionKeyword = true) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            UnityMolMain.recordUndoPythonCommand("");
            return null;
        }

        if (addToExisting && selM.selections.ContainsKey(name)) {
            if (!selM.selections[name].isAlterable) {
                //Just print the warning from the setter
                selM.selections[name].atoms = null; //Not actually changing the selection
                return selM.selections[name];
            }
        }
        if (!addToExisting && selM.selections.ContainsKey(name)) {
            selM.Delete(name);
        }

        if (setAsCurrentSelection) {
            selM.ClearCurrentSelection();
        }

        List<UnityMolAtom> allAtoms = new List<UnityMolAtom>();

        if (allModels) {
            foreach (UnityMolStructure s in sm.loadedStructures) {
                foreach (UnityMolModel m in s.models) {
                    allAtoms.AddRange(m.allAtoms);
                }
            }
        } else {
            foreach (UnityMolStructure s in sm.loadedStructures) {
                allAtoms.AddRange(s.currentModel.allAtoms);
            }
        }

        MDAnalysisSelection selec = new MDAnalysisSelection(selMDA, allAtoms);
        UnityMolSelection newContent = selec.process();
        newContent.name = name;

        if (addToExisting && selM.selections.ContainsKey(name)) {
            //TODO Use selectionmanager updateSelectionsWithMDA

            List<UnityMolAtom> newListAtoms = selM.selections[name].atoms;
            newListAtoms.AddRange(newContent.atoms);
            newListAtoms = newListAtoms.Distinct().ToList();

            selM.selections[name].atoms = newListAtoms;
            if (!selM.selections[name].bondsNull) {
                selM.selections[name].fillBonds();
            }
            selM.selections[name].fillStructures();

            if (selM.selections[name].MDASelString.Length + selMDA.Length > limitSizeSelectionString) {
                selM.selections[name].MDASelString = selM.selections[name].ToSelectionCommand(true);
            } else {
                if (selM.selections[name].MDASelString == "nothing") {
                    selM.selections[name].MDASelString = selMDA;
                } else {
                    selM.selections[name].MDASelString = "(" + selM.selections[name].MDASelString + ") or (" + selMDA + ")";
                }
            }

            if (createSelection && setAsCurrentSelection) {
                selM.SetCurrentSelection(selM.selections[name]);
            }

            if (addToSelectionKeyword)
                selM.AddSelectionKeyword(name, name);

            UnityMolSelectionManager.launchSelectionModified();//Start the event

            // Debug.LogWarning("Adding to existing selection: " + result);
            UnityMolMain.recordPythonCommand("select(\"" + selMDA + "\", \"" + name + "\", " +
                                             cBoolToPy(createSelection) + ", " + cBoolToPy(addToExisting) + ", " + cBoolToPy(silent) + ", " +
                                             cBoolToPy(setAsCurrentSelection) + ", " + cBoolToPy(forceCreate) + ", " + cBoolToPy(allModels) + ", " +
                                             cBoolToPy(addToSelectionKeyword) + ")");
            UnityMolMain.recordUndoPythonCommand("deleteSelection(\"" + name + "\")");
            return selM.selections[name];
        }

        //Should I record the selection
        if (forceCreate || (newContent.atoms.Count != 0 && createSelection)) {
            if (setAsCurrentSelection)
                selM.SetCurrentSelection(newContent);
            else
                selM.Add(newContent);
            if (addToSelectionKeyword)
                selM.AddSelectionKeyword(name, name);
        }

        UnityMolMain.recordPythonCommand("select(\"" + selMDA + "\", \"" + name + "\", " +
                                         cBoolToPy(createSelection) + ", " + cBoolToPy(addToExisting) + ", " + cBoolToPy(silent) + ", " +
                                         cBoolToPy(setAsCurrentSelection) + ", " + cBoolToPy(forceCreate) + ", " + cBoolToPy(allModels) + ", " +
                                         cBoolToPy(addToSelectionKeyword) + ")");

        UnityMolMain.recordUndoPythonCommand("deleteSelection(\"" + newContent.name + "\")");
        if (!silent) {
            Debug.Log(newContent);
        }
        return newContent;
    }

    /// <summary>
    /// Add a keyword to the selection language
    /// </summary>
    public static void addSelectionKeyword(string keyword, string selName) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        selM.AddSelectionKeyword(keyword, selName);
        UnityMolMain.recordPythonCommand("addSelectionKeyword(\"" + keyword + "\", \"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("removeSelectionKeyword(\"" + keyword + "\", \"" + selName + "\")");
    }

    /// <summary>
    /// Remove a keyword from the selection language
    /// </summary>
    public static void removeSelectionKeyword(string keyword, string selName) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        selM.RemoveSelectionKeyword(keyword);
        UnityMolMain.recordPythonCommand("removeSelectionKeyword(\"" + keyword + "\", \"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("addSelectionKeyword(\"" + keyword + "\", \"" + selName + "\")");
    }

    /// <summary>
    /// Set the selection as currentSelection in the UnityMolSelectionManager
    /// </summary>
    public static void setCurrentSelection(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        if (!selM.selections.ContainsKey(selName)) {
            Debug.LogError("Cannot set selection '" + selName + "' as current as it does not exist");
            return;
        }
        UnityMolSelection sel = selM.selections[selName];
        selM.SetCurrentSelection(sel);

        UnityMolMain.recordPythonCommand("setCurrentSelection(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Look for an existing selection named 'name' and add atoms to it based on MDAnalysis selection language
    /// </summary>
    public static void addToSelection(string selMDA, string name = "selection", bool silent = false, bool allModels = false) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }
        if (!selM.selections.ContainsKey(name)) {
            Debug.LogError("Cannot modify selection '" + name + "' as it does not exist");
            return;
        }

        //Get all necessay atoms to execute the selection query
        List<UnityMolAtom> allAtoms = new List<UnityMolAtom>();

        if (allModels) {
            foreach (UnityMolStructure s in sm.loadedStructures) {
                foreach (UnityMolModel m in s.models) {
                    allAtoms.AddRange(m.allAtoms);
                }
            }
        } else {
            foreach (UnityMolStructure s in sm.loadedStructures) {
                allAtoms.AddRange(s.currentModel.allAtoms);
            }
        }

        //Process the selection
        MDAnalysisSelection selec = new MDAnalysisSelection(selMDA, allAtoms);
        UnityMolSelection ret = selec.process();

        List<UnityMolAtom> newAtomList = selM.selections[name].atoms;
        newAtomList.AddRange(ret.atoms);

        newAtomList = newAtomList.Distinct().ToList();

        //Add atoms of the selection to the existing one
        selM.selections[name].atoms = newAtomList;
        if (!selM.selections[name].bondsNull) {
            selM.selections[name].fillBonds();
        }
        selM.selections[name].fillStructures();

        if (selM.selections[name].MDASelString.Length + selMDA.Length > limitSizeSelectionString) {
            selM.selections[name].MDASelString = selM.selections[name].ToSelectionCommand(true);
        } else {
            if (selM.selections[name].MDASelString == "nothing") {
                selM.selections[name].MDASelString = selMDA;
            } else {
                selM.selections[name].MDASelString = "(" + selM.selections[name].MDASelString + ") or (" + selMDA + ")";
            }
        }

        UnityMolSelectionManager.launchSelectionModified();//Start the event

#if !DISABLE_HIGHLIGHT
        UnityMolHighlightManager hM = UnityMolMain.getHighlightManager();
        hM.HighlightAtoms(selM.selections[name]);
#endif

        //Call that to update all representations when changing the selection
        // updateRepresentations(name);

        UnityMolMain.recordPythonCommand("addToSelection(\"" + selMDA + "\", \"" + name + "\", " + cBoolToPy(silent) + ", " + cBoolToPy(allModels) + ")");
        UnityMolMain.recordUndoPythonCommand("");

        if (!silent) {
            Debug.Log(selM.selections[name]);
        }
    }

    /// <summary>
    /// Look for an existing selection named 'name' and remove atoms from it based on MDAnalysis selection language
    /// </summary>
    public static void removeFromSelection(string selMDA, string name = "selection", bool silent = false, bool allModels = false) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }
        if (!selM.selections.ContainsKey(name)) {
            Debug.LogError("Cannot modify selection '" + name + "' as it does not exist");
            return;
        }
        if (!selM.selections[name].isAlterable) {
            //Just print the warning from the setter
            selM.selections[name].atoms = null; //Not actually changing the selection
            return;
        }

        List<UnityMolAtom> allAtoms = new List<UnityMolAtom>();

        if (allModels) {
            foreach (UnityMolStructure s in sm.loadedStructures) {
                foreach (UnityMolModel m in s.models) {
                    allAtoms.AddRange(m.allAtoms);
                }
            }
        } else {
            foreach (UnityMolStructure s in sm.loadedStructures) {
                allAtoms.AddRange(s.currentModel.allAtoms);
            }
        }

        MDAnalysisSelection selec = new MDAnalysisSelection(selMDA, allAtoms);
        UnityMolSelection ret = selec.process();

        //Remove atoms of the selection to the existing one
        List<UnityMolAtom> newListAtoms = selM.selections[name].atoms;
        foreach (UnityMolAtom a in ret.atoms) {
            if (newListAtoms.Contains(a)) {
                newListAtoms.Remove(a);
            }
        }

        selM.selections[name].atoms = newListAtoms;
        selM.selections[name].fillStructures();
        if (!selM.selections[name].bondsNull) {
            selM.selections[name].fillBonds();
        }

        selM.selections[name].MDASelString = selM.selections[name].ToSelectionCommand(true);

#if !DISABLE_HIGHLIGHT
        UnityMolHighlightManager hM = UnityMolMain.getHighlightManager();
        hM.HighlightAtoms(selM.selections[name]);
#endif

        UnityMolSelectionManager.launchSelectionModified();//Start the event

        //Call that to update all representations when changing the selection
        // updateRepresentations(name);

        UnityMolMain.recordPythonCommand("removeFromSelection(\"" + selMDA + "\", \"" + name + "\", " + cBoolToPy(silent) + ", " + cBoolToPy(allModels) + ")");
        UnityMolMain.recordUndoPythonCommand("");

        if (!silent) {
            Debug.Log(selM.selections[name]);
        }
    }

    /// <summary>
    /// Delete selection 'selName' and all its representations
    /// </summary>
    public static void deleteSelection(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        if (!selM.selections.ContainsKey(selName)) {
            Debug.LogError("Cannot delete selection '" + selName + "' as it does not exist");
            return;
        }

        selM.Delete(selName);
        selM.RemoveSelectionKeyword(selName);

        UnityMolMain.recordPythonCommand("deleteSelection(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Duplicate selection 'selName' and without the representations
    /// </summary>
    public static string duplicateSelection(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (!selM.selections.ContainsKey(selName)) {
            Debug.LogError("Cannot delete selection '" + selName + "' as it does not exist");
            return null;
        }

        string newSelName = selM.findNewSelectionName(selName);

        UnityMolSelection sel = selM.selections[selName];
        UnityMolSelection newSel = new UnityMolSelection(sel.atoms, sel.bonds, newSelName, sel.MDASelString);
        newSel.forceGlobalSelection = sel.forceGlobalSelection;
        newSel.isAlterable = true;//sel.isAlterable;

        selM.Add(newSel);
        selM.AddSelectionKeyword(newSelName, newSelName);

        UnityMolMain.recordPythonCommand("duplicateSelection(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("deleteSelection(\"" + newSelName + "\")");
        return newSelName;
    }

    /// <summary>
    /// Change the 'oldSelName' selection name into 'newSelName'
    /// </summary>
    public static bool renameSelection(string oldSelName, string newSelName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        newSelName = newSelName.Replace(" ", "_");

        if (selM.selections.ContainsKey(newSelName)) {
            Debug.LogError("Cannot rename the selection to " +
                           newSelName + ", a selection with the same name already exists");
            return false;
        }
        if (!selM.selections.ContainsKey(oldSelName)) {
            Debug.LogError("Cannot rename the selection to " +
                           newSelName + ", the selection named " + oldSelName + " does not exist");
            return false;
        }
        UnityMolSelection sel = selM.selections[oldSelName];
        bool saveAlte = sel.isAlterable;
        sel.isAlterable = true;

        selM.selections.Remove(oldSelName);
        sel.name = newSelName;
        selM.Add(sel);

        sel.isAlterable = saveAlte;
        Debug.Log("Renamed selection '" + oldSelName + "'' to '" + newSelName + "'");

        selM.RemoveSelectionKeyword(oldSelName);
        selM.AddSelectionKeyword(newSelName, newSelName);

        UnityMolMain.recordPythonCommand("renameSelection(\"" + oldSelName + "\", \"" + newSelName + "\")");
        UnityMolMain.recordUndoPythonCommand("renameSelection(\"" + newSelName + "\", \"" + oldSelName + "\")");

        return true;
    }

    /// <summary>
    /// Update the atoms of the selection based on a new MDAnalysis language selection
    /// The selection only applies to the structures of the selection
    /// </summary>
    public static bool updateSelectionWithMDA(string selName, string selectionString, bool forceAlteration, bool silent = false, bool recordCommand = true, bool allModels = false) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (!selM.selections.ContainsKey(selName)) {
            Debug.LogError("Cannot update the selection '" + selName + "' as it does not exist");
            return false;
        }

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return false;
        }

        try {
            UnityMolSelection sel = selM.selections[selName];

            UnityMolSelection result = null;
            List<UnityMolAtom> allAtoms = new List<UnityMolAtom>();

            if (sel.forceGlobalSelection || sel.structures == null || sel.structures.Count == 0) {

                if (allModels) {
                    foreach (UnityMolStructure s in sm.loadedStructures) {
                        foreach (UnityMolModel m in s.models) {
                            allAtoms.AddRange(m.allAtoms);
                        }
                    }
                } else {
                    foreach (UnityMolStructure s in sm.loadedStructures) {
                        allAtoms.AddRange(s.currentModel.allAtoms);
                    }
                }
                MDAnalysisSelection selec = new MDAnalysisSelection(selectionString, allAtoms);
                result = selec.process();
            } else {
                if (allModels) {
                    foreach (UnityMolStructure s in sel.structures) {
                        foreach (UnityMolModel m in s.models) {
                            allAtoms.AddRange(m.allAtoms);
                        }
                    }
                } else {
                    foreach (UnityMolStructure s in sel.structures) {
                        allAtoms.AddRange(s.currentModel.allAtoms);
                    }
                }

                MDAnalysisSelection selec = new MDAnalysisSelection(selectionString, allAtoms);
                result = selec.process();

            }
            //Don't update representations if the selection did not change
            if (sel.sameAtoms(result)) {
                return true;
            }

            bool saveAlte = sel.isAlterable;
            if (forceAlteration) {
                sel.isAlterable = true;
            }

            sel.atoms = result.atoms;
            if (!sel.bondsNull) {
                sel.bonds = result.bonds;
            }

            sel.fromSelectionLanguage = true;
            sel.MDASelString = result.MDASelString;
            sel.fillStructures();

            UnityMolSelectionManager.launchSelectionModified();//Start the event

            selM.selections[selName] = sel;

            if (!silent) {
                Debug.LogWarning("Modified the selection '" + selName + "' now with " + sel.Count + " atoms");
            }

            // selM.SetCurrentSelection(sel);

#if !DISABLE_HIGHLIGHT
            if (selM.currentSelection != null && selM.currentSelection.name == selName) {
                UnityMolHighlightManager hM = UnityMolMain.getHighlightManager();
                hM.HighlightAtoms(selM.selections[selName]);
            }
#endif

            updateRepresentations(selName);

            if (forceAlteration) {
                sel.isAlterable = saveAlte;
            }

        } catch (System.Exception e) {
#if UNITY_EDITOR
            Debug.LogError("Failed to update the selection: " + e);
            return false;
#endif
        }

        if (recordCommand) {
            UnityMolMain.recordPythonCommand("updateSelectionWithMDA(\"" + selName + "\", \"" + selectionString + "\", " +
                                             cBoolToPy(forceAlteration) + ", " + cBoolToPy(silent) + ", " + cBoolToPy(allModels) + ")");
            UnityMolMain.recordUndoPythonCommand("");
        }

        return true;
    }

    /// <summary>
    /// Directly clear the highlight manager, this does not unselect the current selection
    /// </summary>
    public static void cleanHighlight() {
        UnityMolHighlightManager hM = UnityMolMain.getHighlightManager();

        hM.Clean();
        UnityMolMain.recordPythonCommand("cleanHighlight()");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Select atoms of all loaded molecules inside a sphere defined by a molecular space position and a radius in Anstrom
    /// </summary>
    public static UnityMolSelection selectInSphere(Vector3 position, float radius) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return null;
        }

        string selName = "InSphere";

        selM.ClearCurrentSelection();

        Transform loadedMol = UnityMolMain.getRepresentationParent().transform;
        Vector3 sphereCenter = position;

        List<UnityMolAtom> allAtoms = new List<UnityMolAtom>();

        foreach (UnityMolStructure s in sm.loadedStructures) {
            allAtoms.AddRange(s.currentModel.allAtoms);
        }

        string selMDA = "insphere " + sphereCenter.x.ToString("F5", culture) + " " +
                        sphereCenter.y.ToString("F5", culture) + " " + sphereCenter.z.ToString("F5", culture) + " " +
                        radius.ToString("F3", culture);

        MDAnalysisSelection selec = new MDAnalysisSelection(selMDA, allAtoms);
        UnityMolSelection result = selec.process();
        result.name = selName;

        if (selM.selections.ContainsKey(selName)) {
            selM.selections[selName].atoms = result.atoms;
            if (!selM.selections[selName].bondsNull) {
                selM.selections[selName].fillBonds();
            }
            selM.selections[selName].fillStructures();
            updateRepresentations(selName);
        }

        selM.SetCurrentSelection(result);
        Debug.Log(result);

        UnityMolMain.recordPythonCommand(String.Format(CultureInfo.InvariantCulture, "selectInSphere(Vector3({0}, {1}, {2}), {3})",
                                         position.x, position.y, position.z, radius));

        UnityMolMain.recordUndoPythonCommand("deleteSelection(\"" + result.name + "\")");

        return result;
    }

    /// <summary>
    /// Select atoms of all loaded molecules inside a rectangle defined by a molecular space position and 3 axis
    /// </summary>
    public static UnityMolSelection selectInRectangle(Vector3 lowerLeft, Vector3 xaxis, Vector3 yaxis, Vector3 zaxis) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return null;
        }

        string selName = "InRectangle";

        selM.ClearCurrentSelection();

        Transform loadedMol = UnityMolMain.getRepresentationParent().transform;

        List<UnityMolAtom> allAtoms = new List<UnityMolAtom>();

        foreach (UnityMolStructure s in sm.loadedStructures) {
            allAtoms.AddRange(s.currentModel.allAtoms);
        }

        string selMDA = "inrect " + lowerLeft.x.ToString("F5", culture) + " " +
                        lowerLeft.y.ToString("F5", culture) + " " + lowerLeft.z.ToString("F5", culture) + " " +
                        xaxis.x.ToString("F3", culture) + " " + xaxis.y.ToString("F3", culture) + " " +
                        xaxis.z.ToString("F3", culture) + " " + yaxis.x.ToString("F3", culture) + " " + yaxis.y.ToString("F3", culture) + " " +
                        yaxis.z.ToString("F3", culture) + " " + zaxis.x.ToString("F3", culture) + " " + zaxis.y.ToString("F3", culture) + " " +
                        zaxis.z.ToString("F3", culture);

        MDAnalysisSelection selec = new MDAnalysisSelection(selMDA, allAtoms);
        UnityMolSelection result = selec.process();
        result.name = selName;

        if (selM.selections.ContainsKey(selName)) {
            selM.selections[selName].atoms = result.atoms;
            if (!selM.selections[selName].bondsNull) {
                selM.selections[selName].fillBonds();
            }
            selM.selections[selName].fillStructures();
            updateRepresentations(selName);
        }

        selM.SetCurrentSelection(result);
        Debug.Log(result);

        UnityMolMain.recordPythonCommand(String.Format(CultureInfo.InvariantCulture, "selectInRectangle(Vector3({0}, {1}, {2}), Vector3({3}, {4}, {5}), Vector3({6}, {7}, {8}))",
                                         lowerLeft.x, lowerLeft.y, lowerLeft.z,
                                         xaxis.x, xaxis.y, xaxis.z,
                                         yaxis.x, yaxis.y, yaxis.z,
                                         zaxis.x, zaxis.y, zaxis.z));

        UnityMolMain.recordUndoPythonCommand("deleteSelection(\"" + result.name + "\")");

        return result;
    }

    /// <summary>
    /// Update representations of the specified selection, called automatically after a selection content change
    /// </summary>
    public static void updateRepresentations(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (!selM.selections.ContainsKey(selName)) {
            Debug.LogError("Cannot update representations of the selection '" + selName + "' as it does not exist");
            return;
        }
        UnityMolSelection sel = selM.selections[selName];

        foreach (List<UnityMolRepresentation> reps in sel.representations.Values) {
            foreach (UnityMolRepresentation r in reps) {
                r.updateWithNewSelection(sel);
            }
        }
    }

    /// <summary>
    /// Clear the currentSelection in UnityMolSelectionManager
    /// </summary>
    public static void clearSelections() {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        selM.ClearCurrentSelection();

        UnityMolMain.recordPythonCommand("clearSelections()");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Utility function to test if a trajectory is playing for any loaded molecule
    /// </summary>
    public static bool isATrajectoryPlaying() {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        foreach (UnityMolStructure s in sm.loadedStructures) {
            if (s.trajPlayer && s.trajPlayer.play) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Set the updateContentWithTraj of the selection to enable/disable selection content update
    /// </summary>
    public static void setUpdateSelectionTraj(string selName, bool v) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (!selM.selections.ContainsKey(selName)) {
            Debug.LogError("Selection '" + selName + "' not found");
            return;
        }
        selM.selections[selName].updateContentWithTraj = v;
        UnityMolMain.recordPythonCommand("setUpdateSelectionTraj(\"" + selName + "\", " + cBoolToPy(v) + ")");
        UnityMolMain.recordUndoPythonCommand("setUpdateSelectionTraj(\"" + selName + "\", " + cBoolToPy(!v) + ")");
    }

    /// <summary>
    /// Show or hide representation shadows
    /// </summary>
    public static void setShadows(string selName, string type, bool enable) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType(type);

            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {

                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);

                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        existingRep.ShowShadows(enable);
                    }
                }
            } else {
                Debug.LogError("Wrong representation type");
                return;
            }
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("setShadows(\"" + selName + "\", \"" + type + "\", " + enable + ")");
        UnityMolMain.recordUndoPythonCommand("setShadows(\"" + selName + "\", \"" + type + "\", " + !enable + ")");
    }

    /// <summary>
    /// Utility function to change the material of highlighted selection
    /// </summary>
    public static void changeHighlightMaterial(Material newMat) {
#if !DISABLE_HIGHLIGHT
        UnityMolHighlightManager highlm = UnityMolMain.getHighlightManager();
        highlm.changeMaterial(newMat);
#endif
    }

    /// <summary>
    /// Take a screenshot of the current viewpoint with a specific resolution
    /// </summary>
    public static void screenshot(string path, int resolutionWidth = 1280, int resolutionHeight = 720, bool transparentBG = false) {
        RecordManager.takeScreenshot(path, resolutionWidth, resolutionHeight, transparentBG);
    }

    /// <summary>
    /// Start to record a video with FFMPEG at a specific resolution and framerate
    /// </summary>
    public static void startVideo(string filePath, int resolutionWidth = 1280, int resolutionHeight = 720, int frameRate = 30, bool pauseAtStart = false) {
        RecordManager.startRecordingVideo(filePath, resolutionWidth, resolutionHeight, frameRate, pauseAtStart);
    }
    /// <summary>
    /// Stop recording
    /// </summary>
    public static void stopVideo() {
        RecordManager.stopRecordingVideo();
    }

    /// <summary>
    /// Pause recording
    /// </summary>
    public static void pauseVideo() {
        RecordManager.pauseRecordingVideo();
    }
    /// <summary>
    /// Unpause recording
    /// </summary>
    public static void unpauseVideo() {
        RecordManager.unpauseRecordingVideo();
    }


    // --------------- Python history functions
    /// <summary>
    /// Play the opposite function of the lastly called APIPython function recorded in UnityMolMain.pythonUndoCommands
    /// </summary>
    public static void undo() {
        if (UnityMolMain.pythonUndoCommands.Count == 0) {
            return;
        }
        string lastUndoCommand = UnityMolMain.pythonUndoCommands.Last();

        if (lastUndoCommand != null) {
            Debug.Log("Undo command = " + lastUndoCommand);
            bool success = false;
            pythonConsole.ExecuteCommand(lastUndoCommand, ref success);

            UnityMolMain.pythonUndoCommands.RemoveAt(UnityMolMain.pythonUndoCommands.Count - 1);
            UnityMolMain.pythonCommands.RemoveAt(UnityMolMain.pythonCommands.Count - 1);

            //Remove the 2 last commands, the undo + the previous command

            if (lastUndoCommand != "") {
                UnityMolMain.pythonCommands.RemoveAt(UnityMolMain.pythonCommands.Count - 1);
                UnityMolMain.pythonUndoCommands.RemoveAt(UnityMolMain.pythonUndoCommands.Count - 1);

                int count = lastUndoCommand.Split('\n').Length - 1;
                for (int i = 0; i < count; i++) {
                    UnityMolMain.pythonCommands.RemoveAt(UnityMolMain.pythonCommands.Count - 1);
                    UnityMolMain.pythonUndoCommands.RemoveAt(UnityMolMain.pythonUndoCommands.Count - 1);
                }
            }
        }

    }

    /// <summary>
    /// Set the local position and rotation (euler angles) of the given structure
    /// </summary>
    public static void setStructurePositionRotation(string structureName, Vector3 pos, Vector3 rot) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolStructure s = sm.GetStructure(structureName);

        Vector3 savePos = Vector3.zero;
        Vector3 saveRot = Vector3.zero;
        if (s != null) {
            GameObject sgo = sm.structureToGameObject[s.name];
            savePos = sgo.transform.localPosition;
            saveRot = sgo.transform.localEulerAngles;

            sgo.transform.localPosition = pos;
            sgo.transform.localRotation = Quaternion.Euler(rot);
        } else {
            Debug.LogError("Wrong structure name");
            return;
        }

        UnityMolMain.recordPythonCommand("setStructurePositionRotation( \"" + structureName + "\", Vector3(" + pos.x.ToString("F4", culture) + ", " +
                                         pos.y.ToString("F4", culture) + ", " + pos.z.ToString("F4", culture) + "), " +
                                         "Vector3(" + rot.x.ToString("F4", culture) + ", " +
                                         rot.y.ToString("F4", culture) + ", " + rot.z.ToString("F4", culture) + "))");
        UnityMolMain.recordUndoPythonCommand("setStructurePositionRotation( \"" + structureName + "\", Vector3(" + savePos.x.ToString("F4", culture) + ", " +
                                             savePos.y.ToString("F4", culture) + ", " + savePos.z.ToString("F4", culture) + "), " +
                                             "Vector3(" + saveRot.x.ToString("F4", culture) + ", " +
                                             saveRot.y.ToString("F4", culture) + ", " + saveRot.z.ToString("F4", culture) + "))");
    }

    /// <summary>
    /// Get the current position and rotation of the given structure
    /// </summary>
    public static void getStructurePositionRotation(string structureName, ref Vector3 pos, ref Vector3 rot) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolStructure s = sm.GetStructure(structureName);

        if (s != null) {
            GameObject sgo = sm.structureToGameObject[s.name];
            pos = sgo.transform.localPosition;
            rot = sgo.transform.localEulerAngles;
            return;
        } else {
            Debug.LogError("Wrong structure name");
            return;
        }
    }

    /// <summary>
    /// Get the current position and rotation of the given structure
    /// </summary>
    public static string getStructurePositionRotation(string structureName) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolStructure s = sm.GetStructure(structureName);

        if (s != null) {
            GameObject sgo = sm.structureToGameObject[s.name];
            Vector3 pos = sgo.transform.localPosition;
            Vector3 rot = sgo.transform.localEulerAngles;
            return "Vector3(" + pos.x.ToString("F4", culture) + ", " +
                   pos.y.ToString("F4", culture) + ", " + pos.z.ToString("F4", culture) + "), " +
                   "Vector3(" + rot.x.ToString("F4", culture) + ", " +
                   rot.y.ToString("F4", culture) + ", " + rot.z.ToString("F4", culture) + "))";
        } else {
            Debug.LogError("Wrong structure name");
            return "";
        }
    }

    /// <summary>
    /// Save the history of commands executed in a file
    /// </summary>
    public static void saveScript(string fullpath) {
        saveHistoryScript(fullpath);
    }

    /// <summary>
    /// Save the history of commands executed in a file
    /// </summary>
    public static void saveHistoryScript(string fullpath) {
        string scriptContent = UnityMolMain.commandHistory();

        //Set center to false in fetch and load commands => this is handled by the loadedMolParentToString function
        string[] commands = scriptContent.Split(new [] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < commands.Length; i++) {
            string c = commands[i];
            if (c.StartsWith("fetch(") || c.StartsWith("load(")) {
                commands[i] = commands[i].Replace(", center=True", ", center=False");
                commands[i] = commands[i].Replace(", center= True", ", center= False");
            }
        }
        scriptContent = string.Join("\n", commands);

        scriptContent += loadedMolParentToString();

        File.WriteAllText(fullpath, scriptContent);

        Debug.Log("Saved history script to '" + fullpath + "'");
    }

    /// <summary>
    /// Save the current positions of the loaded structures in a single PDB file
    /// </summary>
    public static void saveDockingState(string fullpath = null) {

        DockingManager dm = UnityMolMain.getDockingManager();
        if (fullpath == null)
            fullpath = dm.saveDockingState();
        else
            dm.saveDockingState(fullpath);

        UnityMolMain.recordPythonCommand($"saveDockingState(\"{fullpath.Replace("\\", " / ")}\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    public static string loadedMolParentToString(bool addToHistory = false) {
        ManipulationManager mm = getManipulationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        string res = "";
        foreach (UnityMolStructure s in sm.loadedStructures) {
            Vector3 spos = Vector3.zero;
            Vector3 srot = Vector3.zero;
            getStructurePositionRotation(s.name, ref spos, ref srot);
            res += "\n\nsetStructurePositionRotation(\"" + s.name + "\", Vector3(" + spos.x.ToString("F4", culture) + ", " +
                   spos.y.ToString("F4", culture) + ", " + spos.z.ToString("F4", culture) + "), " +
                   "Vector3(" + srot.x.ToString("F4", culture) + ", " +
                   srot.y.ToString("F4", culture) + ", " + srot.z.ToString("F4", culture) + "))";
        }

        //Save parent state
        Transform parentT = UnityMolMain.getRepresentationParent().transform;
        res += "\n#Save parent position\nsetMolParentTransform( Vector3(" + parentT.position.x.ToString("F4", culture) + ", " +
               parentT.position.y.ToString("F4", culture) + ", " + parentT.position.z.ToString("F4", culture) + "), Vector3(" + parentT.localScale.x.ToString("F4", culture) + ", " +
               parentT.localScale.y.ToString("F4", culture) + ", " + parentT.localScale.z.ToString("F4", culture) + "), Vector3(" + parentT.eulerAngles.x.ToString("F4", culture) + ", " +
               parentT.eulerAngles.y.ToString("F4", culture) + ", " + parentT.eulerAngles.z.ToString("F4", culture) + "), Vector3(" + mm.currentCenterPosition.x.ToString("F4", culture) + ", " +
               mm.currentCenterPosition.y.ToString("F4", culture) + ", " + mm.currentCenterPosition.z.ToString("F4", culture) + ") )\n";


        if (addToHistory) {
            UnityMolMain.recordPythonCommand(res);
            UnityMolMain.recordUndoPythonCommand("");
        }

        return res;
    }

    /// <summary>
    /// Load a python script of commands (possibly the output of the saveHistoryScript function)
    /// </summary>
    public static void loadHistoryScript(string filePath) {

        string realPath = filePath;
        string customPath = Path.Combine(path, filePath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        instance.StartCoroutine(pythonConsole.ExecuteScript(realPath));
    }

    /// <summary>
    /// Load a python script of commands (possibly the output of the saveHistoryScript function)
    /// </summary>
    public static void loadScript(string filePath) {
        loadHistoryScript(filePath);
    }

    /// <summary>
    /// Set the position, scale and rotation of the parent of all loaded molecules
    /// Linear interpolation between the current state of the camera to the specified values
    /// </summary>
    public static void setMolParentTransform(Vector3 pos, Vector3 scale, Vector3 rot, Vector3 centerOfRotation, bool lerp = true, float duration = 1.0f) {
        instance.setPosScaleRot(pos, scale, rot, centerOfRotation, lerp, duration);
    }

    public static string getMolParentTransform() {
        Transform parentT = UnityMolMain.getRepresentationParent().transform;
        return "Vector3(" + parentT.position.x.ToString("F4", culture) + ", " +
               parentT.position.y.ToString("F4", culture) + ", " + parentT.position.z.ToString("F4", culture) + "), " +
               "Vector3(" + parentT.rotation.eulerAngles.x.ToString("F4", culture) + ", " +
               parentT.rotation.eulerAngles.y.ToString("F4", culture) + ", " + parentT.rotation.eulerAngles.z.ToString("F4", culture) + "))";
    }

    public void setPosScaleRot(Vector3 pos, Vector3 scale, Vector3 rot, Vector3 centerOfRotation, bool lerp, float duration) {
        Transform parentT = UnityMolMain.getRepresentationParent().transform;
        StartCoroutine(delayedSetTransform(parentT, pos, scale, rot, centerOfRotation, lerp, duration));
    }

    IEnumerator delayedSetTransform(Transform t, Vector3 endpos, Vector3 scale, Vector3 rot, Vector3 centerOfRotation, bool lerp, float duration = 1.0f) {
        //End of frame
        yield return 0;

        t.localScale = scale;
        Quaternion targetRot = Quaternion.Euler(rot);
        Quaternion fromRot = t.rotation;
        Vector3 startpos = t.position;

        float multi = 1.0f / duration;
        float ratio = 0.0f;
        if (lerp) {
            while (t.position != endpos) {
                ratio += Time.deltaTime * multi;
                t.position = Vector3.Lerp(startpos, endpos, ratio);
                t.rotation = Quaternion.Lerp(fromRot, targetRot, ratio);
                yield return 0;
            }
        }

        t.position = endpos;
        t.rotation = targetRot;

        var mm = getManipulationManager();
        mm.setRotationCenter(centerOfRotation);
    }

    /// <summary>
    /// Change the scale of the parent of the representations of each molecules
    /// Try to not move the center of mass
    /// </summary>
    public static void changeGeneralScale_cog(float newVal) {
        if (newVal > 0.0f && newVal != Mathf.Infinity) {
            UnityMolStructureManager sm = UnityMolMain.getStructureManager();

            if (sm.loadedStructures.Count == 0) {
                Debug.LogWarning("No molecule loaded");
                return;
            }

            Transform molPar = UnityMolMain.getRepresentationParent().transform;

            List<Vector3> savedCog = new List<Vector3>();

            foreach (Transform t in molPar) {
                UnityMolStructure s = sm.selectionNameToStructure(t.name);
                if (s != null) {
                    savedCog.Add(t.TransformPoint(s.currentModel.centroid));
                }
            }

            molPar.localScale = Vector3.one * newVal;

            int i = 0;
            foreach (Transform t in molPar) {
                UnityMolStructure s = sm.selectionNameToStructure(t.name);
                if (s != null) {
                    Vector3 newCog = t.TransformPoint(s.currentModel.centroid);
                    t.Translate(savedCog[i++] - newCog, Space.World);
                }
            }
        }
    }

    /// <summary>
    /// Change the scale of the parent of the representations of each molecules
    /// Keep relative positions of molecules, use the first loaded molecule center of gravity to compensate the translation due to scaling
    /// </summary>
    public static void changeGeneralScale(float newVal) {
        if (newVal > 0.0f && newVal != Mathf.Infinity) {
            UnityMolStructureManager sm = UnityMolMain.getStructureManager();

            if (sm.loadedStructures.Count == 0) {
                Debug.LogWarning("No molecule loaded");
                return;
            }

            Transform molPar = UnityMolMain.getRepresentationParent().transform;

            if (molPar.childCount == 0) {
                return;
            }
            UnityMolStructure s = null;
            Transform t = null;
            foreach (Transform tr in molPar) {
                s = sm.selectionNameToStructure(tr.name);
                if (s != null) {
                    t = tr;
                    break;
                }
            }
            Vector3 save = Vector3.zero;

            if (s != null) {
                save = t.TransformPoint(s.currentModel.centroid);

                molPar.localScale = Vector3.one * newVal;

                Vector3 newP = t.TransformPoint(s.currentModel.centroid);

                // //Also move world annotations
                // foreach (Transform tr in molPar) {
                //     if (tr.name.StartsWith("World")) {
                //         tr.TransformPoint(s.currentModel.centroid);
                //     }
                // }

                molPar.Translate(save - newP, Space.World);
            }

        }
    }

    /// <summary>
    /// Use Reduce method to add hydrogens
    /// </summary>
    public static void addHydrogensReduce(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolStructure s = sm.GetStructure(structureName);

        if (s != null) { //Remove a complete loaded molecule
            ReduceWrapper.callReduceOnStructure(s);
        } else {
            Debug.LogError("Wrong structure name");
            return;
        }

        s.updateRepresentations(trajectory: false);

        UnityMolMain.recordPythonCommand("addHydrogensReduce(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Use HAAD method to add hydrogens
    /// </summary>
    public static void addHydrogensHaad(string structureName) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        UnityMolStructure s = sm.GetStructure(structureName);

        if (s != null) { //Remove a complete loaded molecule
            // ReduceWrapper.callReduceOnStructure(s);
            HaadWrapper.callHaadOnStructure(s);
        } else {
            Debug.LogError("Wrong structure name");
            return;
        }

        s.updateRepresentations(trajectory: false);

        UnityMolMain.recordPythonCommand("addHydrogensHaad(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Set the atoms of the selection named 'selName' to ligand
    /// </summary>
    public static void setAsLigand(string selName, bool isLig = true, bool updateAllSelections = true) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            foreach (UnityMolAtom a in sel.atoms) {
                a.isLigand = isLig;

            }
            Debug.Log("Set " + sel.atoms.Count + " atom(s) as " + (isLig ? "" : "non-") + "ligand");
        } else {
            Debug.LogError("No selection named " + selName);
            return;
        }

        //Record command before calling another APIPython function
        UnityMolMain.recordPythonCommand("setAsLigand(\"" + selName + "\", " + cBoolToPy(isLig) + ", " + cBoolToPy(updateAllSelections) + ")");

        //Caution
        UnityMolMain.recordUndoPythonCommand("setAsLigand(\"" + selName + "\", " + cBoolToPy(!isLig) + ", True)");

        if (updateAllSelections) {
            List<UnityMolSelection> sels = selM.selections.Values.ToList();

            foreach (UnityMolSelection sele in sels) {
                if (sele.fromSelectionLanguage) {
                    updateSelectionWithMDA(sele.name, sele.MDASelString, true, recordCommand : false);
                }
            }
        }
    }

    /// <summary>
    /// Merge UnityMolStructure structureName2 in structureName using a different chain name to avoid conflict
    /// </summary>
    public static void mergeStructure(string structureName, string structureName2, string chainName = "Z") {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure toMerge = sm.GetStructure(structureName2);
        if (s != null && toMerge != null) {
            s.MergeStructure(toMerge, chainName);
            UnityMolMain.recordPythonCommand("mergeStructure(\"" + structureName + "\", \"" + structureName2 + "\", \"" + chainName + "\")");
            UnityMolMain.recordUndoPythonCommand("");
        }
    }

    /// <summary>
    /// Save current atom positions of the selection to a PDB file
    /// World atom positions are transformed to be relative to the first structure in the selection
    /// </summary>
    public static void saveToPDB(string selName, string fullPath, bool writeSSinfo = false) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];
            if (sel.Count == 0) {
                Debug.LogWarning("Empty selection");
                return;
            }

            Vector3[] atomPos = new Vector3[sel.atoms.Count];

            Transform strucPar = UnityMolMain.getStructureManager().GetStructureGameObject(
                                     sel.structures[0].name).transform;

            int id = 0;
            foreach (UnityMolAtom a in sel.atoms) {
                atomPos[id++] = strucPar.InverseTransformPoint(a.curWorldPosition);
            }

            string pdbLines = PDBReader.Write(sel, overridedPos : atomPos, writeSS : writeSSinfo);
            if (string.IsNullOrEmpty(pdbLines)) {
                return;
            }
            try {
                StreamWriter writer = new StreamWriter(fullPath, false);
                writer.WriteLine(pdbLines);
                writer.Close();
                Debug.Log("Wrote PDB file: '" + Path.GetFullPath(fullPath) + "'");
            } catch {
                Debug.LogError("Failed to write to '" + Path.GetFullPath(fullPath) + "'");
            }

        } else {
            Debug.LogError("No selection named " + selName);
            return;
        }
    }

    /// <summary>
    /// Connect to a running simulation using the IMD protocol implemented in MDDriver
    /// The running simulation is binded to a UnityMolStructure
    /// </summary>
    public static bool connectIMD(string structureName, string adress, int port) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return false;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        bool res = false;
        try {

            if (s.mddriverM != null) {
                Debug.LogError("Already connected to a running simulation");
                return false;
            }
            res = s.connectIMD(adress, port);

        } catch (System.Exception e) {
            Debug.LogError("Could not connect to the simulation on " + adress + " : " + port + "\n " + e);
            s.disconnectIMD();
            return false;
        }

        UnityMolMain.recordPythonCommand("connectIMD(\"" + structureName + "\", \"" + adress + "\", " + port + ")");
        UnityMolMain.recordUndoPythonCommand("disconnectIMD(\"" + structureName + "\")");
        return res;
    }

    /// <summary>
    /// Disconnect from the IMD simulation for the specified structure
    /// </summary>
    public static void disconnectIMD(string structureName) {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        s.disconnectIMD();

        UnityMolMain.recordPythonCommand("disconnectIMD(\"" + structureName + "\")");
        UnityMolMain.recordUndoPythonCommand("");

    }

    /// <summary>
    /// Get current surface material
    /// </summary>
    public static string getSurfaceType(string selName) {

        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {

            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("s");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);

            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;

                        if (!surfM.isTransparent && !surfM.isWireframe) {
                            return "Solid";
                        }
                        if (surfM.isWireframe) {
                            return "Wireframe";
                        }
                        return "Transparent";
                    }
                }
            }

            repType = getRepType("dxiso");

            existingReps = repManager.representationExists(selName, repType);

            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolSurfaceManager surfM = (UnityMolSurfaceManager) sr.atomRepManager;

                        if (!surfM.isTransparent && !surfM.isWireframe) {
                            return "Solid";
                        }
                        if (surfM.isWireframe) {
                            return "Wireframe";
                        }
                        return "Transparent";
                    }
                }
            }
        }
        return "";
    }

    /// <summary>
    /// Get current hyperball metaphore
    /// </summary>
    public static string getHyperBallMetaphore(string selName) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            UnityMolSelection sel = selM.selections[selName];

            RepType repType = getRepType("hb");

            List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);

            if (existingReps != null) {
                foreach (UnityMolRepresentation existingRep in existingReps) {
                    foreach (SubRepresentation sr in existingRep.subReps) {
                        UnityMolHStickMeshManager hsManager = (UnityMolHStickMeshManager) sr.bondRepManager;
                        if (hsManager != null) {
                            if (hsManager.shrink == 0.4f && hsManager.scaleBond == 1.0f)
                                return "Smooth";
                            if (hsManager.shrink == 0.001f && hsManager.scaleBond == 0.2f)
                                return "BallsAndSticks";
                            if (hsManager.shrink == 1.0f && hsManager.scaleBond == 0.0f)
                                return "VdW";
                            if (hsManager.shrink == 0.001f && hsManager.scaleBond == 0.3f)
                                return "Licorice";
                            if (hsManager.shrink == 1.0f && hsManager.scaleBond == 0.0f)
                                return "Hidden";
                        }
                    }
                }
            }
        }
        return "";
    }

    public static void setCameraOrtho(bool ortho) {
        if (UnityMolMain.inVR() && ortho) {
            Debug.LogWarning("Cannot activate orthographic camera in VR");
            return;
        }
        if (Camera.main.orthographic != ortho) {
            Camera.main.orthographic = ortho;
        }

        UnityMolMain.recordPythonCommand("setCameraOrtho(" + cBoolToPy(ortho) + ")");
        UnityMolMain.recordUndoPythonCommand("setCameraOrtho(" + cBoolToPy(!ortho) + ")");
    }

    public static void setCameraOrthoSize(float orthoSize) {
        if (UnityMolMain.inVR())
            return;

        orthoSize = Mathf.Max(0.001f, orthoSize);
        float prevVal = 5.0f;

        Camera mainCam = Camera.main;
        if (mainCam.orthographic) {
            prevVal = mainCam.orthographicSize;
            mainCam.orthographicSize = orthoSize;
        }

        int lenSameCom = 19;
        bool replaced = UnityMolMain.recordPythonCommand("setCameraOrthoSize(" + orthoSize.ToString("F4") + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setCameraOrthoSize(" + prevVal.ToString("F4") + ")", replaced);
    }

    /// <summary>
    /// Set camera near plane, note this has an impact on shadow map quality
    /// </summary>
    public static void setCameraNearPlane(float newV) {
        float prevVal = Camera.main.nearClipPlane;
        newV = Mathf.Clamp(newV, 0.001f, 100.0f);
        Camera.main.nearClipPlane = newV;

        int lenSameCom = 19;
        bool replaced = UnityMolMain.recordPythonCommand("setCameraNearPlane(" + newV.ToString("F4", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setCameraNearPlane(" + prevVal.ToString("F4", culture) + ")", replaced);
    }

    /// <summary>
    /// Set camera far plane, note this has an impact on shadow map quality
    /// </summary>
    public static void setCameraFarPlane(float newV) {
        float prevVal = Camera.main.farClipPlane;
        newV = Mathf.Clamp(newV, 0.1f, 5000.0f);
        Camera.main.farClipPlane = newV;
        int lenSameCom = 18;
        bool replaced = UnityMolMain.recordPythonCommand("setCameraFarPlane(" + newV.ToString("F4", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setCameraFarPlane(" + prevVal.ToString("F4", culture) + ")", replaced);
    }

    /// <summary>
    /// Enable depth cueing effect
    /// </summary>
    public static void enableDepthCueing() {
        UnityMolMain.isFogOn = true;
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        foreach (UnityMolStructure s in sm.loadedStructures) {
            foreach (UnityMolRepresentation r in s.representations) {
                foreach (SubRepresentation sr in r.subReps) {
                    if (sr.atomRepManager != null) {
                        sr.atomRepManager.EnableDepthCueing();
                    }
                    if (sr.bondRepManager != null) {
                        sr.bondRepManager.EnableDepthCueing();
                    }
                }
            }
        }

        UnityMolMain.recordPythonCommand("enableDepthCueing()");
        UnityMolMain.recordUndoPythonCommand("disableDepthCueing()");
    }

    /// <summary>
    /// Disable depth cueing effect
    /// </summary>
    public static void disableDepthCueing() {
        UnityMolMain.isFogOn = false;
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        foreach (UnityMolStructure s in sm.loadedStructures) {
            foreach (UnityMolRepresentation r in s.representations) {
                foreach (SubRepresentation sr in r.subReps) {
                    if (sr.atomRepManager != null) {
                        sr.atomRepManager.DisableDepthCueing();
                    }
                    if (sr.bondRepManager != null) {
                        sr.bondRepManager.DisableDepthCueing();
                    }
                }
            }
        }
        UnityMolMain.recordPythonCommand("disableDepthCueing()");
        UnityMolMain.recordUndoPythonCommand("enableDepthCueing()");
    }

    /// <summary>
    /// Set depth cueing starting position in world space
    /// </summary>
    public static void setDepthCueingStart(float v) {
        float prev = UnityMolMain.fogStart;
        UnityMolMain.fogStart = v;
        getManipulationManager().initFollowDepthCueing();

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        foreach (UnityMolStructure s in sm.loadedStructures) {
            foreach (UnityMolRepresentation r in s.representations) {
                foreach (SubRepresentation sr in r.subReps) {
                    if (sr.atomRepManager != null) {
                        sr.atomRepManager.SetDepthCueingStart(UnityMolMain.fogStart);
                    }
                    if (sr.bondRepManager != null) {
                        sr.bondRepManager.SetDepthCueingStart(UnityMolMain.fogStart);
                    }
                }
            }
        }
        int lenSameCom = 20;
        bool replaced = UnityMolMain.recordPythonCommand("setDepthCueingStart(" + UnityMolMain.fogStart.ToString("F2", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setDepthCueingStart(" + prev.ToString("F2", culture) + ")", replaced);
    }

    /// <summary>
    /// Set depth cueing density
    /// </summary>
    public static void setDepthCueingDensity(float v) {
        float prev = UnityMolMain.fogDensity;
        UnityMolMain.fogDensity = v;
        getManipulationManager().initFollowDepthCueing();

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        foreach (UnityMolStructure s in sm.loadedStructures) {
            foreach (UnityMolRepresentation r in s.representations) {
                foreach (SubRepresentation sr in r.subReps) {
                    if (sr.atomRepManager != null) {
                        sr.atomRepManager.SetDepthCueingDensity(UnityMolMain.fogDensity);
                    }
                    if (sr.bondRepManager != null) {
                        sr.bondRepManager.SetDepthCueingDensity(UnityMolMain.fogDensity);
                    }
                }
            }
        }
        int lenSameCom = 22;
        bool replaced = UnityMolMain.recordPythonCommand("setDepthCueingDensity(" + UnityMolMain.fogDensity.ToString("F2", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setDepthCueingDensity(" + prev.ToString("F2", culture) + ")", replaced);
    }

    /// <summary>
    /// Set depth cueing color
    /// </summary>
    public static void setDepthCueingColor(Color col) {

        RenderSettings.fogColor = col;

        UnityMolMain.recordPythonCommand("setDepthCueingColor(" + String.Format(CultureInfo.InvariantCulture, "{0})", col));
        UnityMolMain.recordUndoPythonCommand("setDepthCueingColor(" + String.Format(CultureInfo.InvariantCulture, "{0})", col));
    }


    /// <summary>
    /// Enable/Disable depth cueing update when zooming in or out
    /// </summary>
    public static void setDepthCueingFollow(bool v) {
        getManipulationManager().depthcueUpdate = v;
        getManipulationManager().initFollowDepthCueing();

        UnityMolMain.recordPythonCommand("setDepthCueingFollow(" + cBoolToPy(v) + ")");
        UnityMolMain.recordUndoPythonCommand("setDepthCueingFollow(" + cBoolToPy(v) + ")");

    }

    /// <summary>
    /// Enable DOF effect, only available in desktop mode
    /// </summary>
    public static void enableDOF() {
        if (UnityMolMain.inVR()) {
            Debug.LogWarning("Cannot enable DOF in VR");
            return;
        }
        if (UnityMolMain.raytracingMode) {
            RaytracedObject rto = Camera.main.gameObject.GetComponent<RaytracedObject>();
            rto.setAperture(1.0f);
        }
        try {
            MouseAutoFocus maf = Camera.main.gameObject.GetComponent<MouseAutoFocus>();
            MouseOverSelection mos = Camera.main.gameObject.GetComponent<MouseOverSelection>();
            if (maf == null) {
                maf = Camera.main.gameObject.AddComponent<MouseAutoFocus>();
            }
            maf.Init();
            maf.enableDOF();
        } catch {
            Debug.LogError("Couldn't enable DOF");
            return;
        }
        UnityMolMain.recordPythonCommand("enableDOF()");
        UnityMolMain.recordUndoPythonCommand("disableDOF()");
    }

    /// <summary>
    /// Disable DOF effect, only available in desktop mode
    /// </summary>
    public static void disableDOF() {
        if (UnityMolMain.raytracingMode) {
            RaytracedObject rto = Camera.main.gameObject.GetComponent<RaytracedObject>();
            rto.setAperture(-1.0f);
        }
        try {
            MouseAutoFocus maf = Camera.main.gameObject.GetComponent<MouseAutoFocus>();
            MouseOverSelection mos = Camera.main.gameObject.GetComponent<MouseOverSelection>();
            if (maf == null) {
                maf = Camera.main.gameObject.AddComponent<MouseAutoFocus>();
            }
            maf.Init();
            maf.disableDOF();

        } catch {
            Debug.LogError("Couldn't disable DOF");
            return;
        }
        UnityMolMain.recordPythonCommand("disableDOF()");
        UnityMolMain.recordUndoPythonCommand("enableDOF()");
    }

    /// <summary>
    /// Set DOF focus distance, this is used by the MouseAutoFocus script
    /// </summary>
    public static void setDOFFocusDistance(float v) {
        float prev = 0.0f;
        try {
            MouseAutoFocus maf = Camera.main.gameObject.GetComponent<MouseAutoFocus>();

            prev = maf.getFocusDistance();
            maf.setFocusDistance(v);

        } catch {
            Debug.LogError("Couldn't set DOF focus distance");
            return;
        }
        int lenSameCom = 20;
        bool replaced = UnityMolMain.recordPythonCommand("setDOFFocusDistance(" + v.ToString("F3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setDOFFocusDistance(" + prev.ToString("F3", culture) + ")", replaced);
    }
    /// <summary>
    /// Set DOF aperture
    /// </summary>
    public static void setDOFAperture(float a) {
        if (UnityMolMain.raytracingMode) {
            RaytracedObject rto = Camera.main.gameObject.GetComponent<RaytracedObject>();
            rto.setAperture(a * 0.005f);
        }
        float prev = 0.0f;
        try {
            MouseAutoFocus maf = Camera.main.gameObject.GetComponent<MouseAutoFocus>();

            prev = maf.getAperture();
            maf.setAperture(a);

        } catch {
            Debug.LogError("Couldn't set DOF aperture");
            return;
        }
        int lenSameCom = 15;
        bool replaced = UnityMolMain.recordPythonCommand("setDOFAperture(" + a.ToString("F3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setDOFAperture(" + prev.ToString("F3", culture) + ")", replaced);
    }
    /// <summary>
    /// Set DOF focal length
    /// </summary>
    public static void setDOFFocalLength(float f) {
        if (UnityMolMain.raytracingMode) {
            RaytracedObject rto = Camera.main.gameObject.GetComponent<RaytracedObject>();
            rto.setFDist(f * 0.05f);
        }
        float prev = 0.0f;
        try {
            MouseAutoFocus maf = Camera.main.gameObject.GetComponent<MouseAutoFocus>();

            prev = maf.getFocalLength();
            maf.setFocalLength(f);

        } catch {
            Debug.LogError("Couldn't set DOF focal length");
            return;
        }
        int lenSameCom = 18;
        bool replaced = UnityMolMain.recordPythonCommand("setDOFFocalLength(" + f.ToString("F3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setDOFFocalLength(" + prev.ToString("F3", culture) + ")", replaced);
    }
    /// <summary>
    /// Enable outline post-process effect
    /// </summary>
    public static void enableOutline() {

        try {
            OutlineEffectUtil outlineScript = Camera.main.gameObject.GetComponent<OutlineEffectUtil>();
            if (outlineScript == null) {
                outlineScript = Camera.main.gameObject.AddComponent<OutlineEffectUtil>();
            }

            outlineScript.enableOutline();

        } catch {
            Debug.LogError("Couldn't enable Outline effect");
            return;
        }
        UnityMolMain.recordPythonCommand("enableOutline()");
        UnityMolMain.recordUndoPythonCommand("disableOutline()");
    }

    /// <summary>
    /// Disable outline effect
    /// </summary>
    public static void disableOutline() {

        try {
            OutlineEffectUtil outlineScript = Camera.main.gameObject.GetComponent<OutlineEffectUtil>();
            if (outlineScript == null) {
                outlineScript = Camera.main.gameObject.AddComponent<OutlineEffectUtil>();
            }

            outlineScript.disableOutline();

        } catch {
            Debug.LogError("Couldn't disable Outline effect");
            return;
        }
        UnityMolMain.recordPythonCommand("disableOutline()");
        UnityMolMain.recordUndoPythonCommand("enableOutline()");
    }

    /// <summary>
    /// Set outline effect thickness
    /// </summary>
    public static void setOutlineThickness(float v) {

        float prev = 0.0f;
        try {
            OutlineEffectUtil outlineScript = Camera.main.gameObject.GetComponent<OutlineEffectUtil>();
            if (outlineScript == null) {
                outlineScript = Camera.main.gameObject.AddComponent<OutlineEffectUtil>();
            }

            prev = outlineScript.getThickness();
            outlineScript.setThickness(v);

        } catch {
            Debug.LogError("Couldn't enable Outline effect");
            return;
        }

        int lenSameCom = 20;
        bool replaced = UnityMolMain.recordPythonCommand("setOutlineThickness(" + v.ToString("F3", culture) + ")", true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setOutlineThickness(" + prev.ToString("F3", culture) + ")", replaced);
    }

    /// <summary>
    /// Set outline effect color
    /// </summary>
    public static void setOutlineColor(Color col) {

        Color prev = Color.black;
        try {
            OutlineEffectUtil outlineScript = Camera.main.gameObject.GetComponent<OutlineEffectUtil>();
            if (outlineScript == null) {
                outlineScript = Camera.main.gameObject.AddComponent<OutlineEffectUtil>();
            }

            prev = outlineScript.getColor();
            outlineScript.setColor(col);

        } catch {
            Debug.LogError("Couldn't enable Outline effect");
            return;
        }

        int lenSameCom = 16;
        bool replaced = UnityMolMain.recordPythonCommand("setOutlineColor(" + String.Format(CultureInfo.InvariantCulture, "{0})", col), true, lenSameCom);
        UnityMolMain.recordUndoPythonCommand("setOutlineColor(" + String.Format(CultureInfo.InvariantCulture, "{0})", prev), replaced);
    }

    // ---------------
    /// <summary>
    /// Print the content of the current directory, outputs only the files
    /// </summary>
    public static List<string> ls() {
        // var info = new DirectoryInfo(path);
        List<string> ret = Directory.GetFiles(path).ToList();
        foreach (string f in Directory.GetDirectories(path)) {
            Debug.Log("<b>" + f + "</b>");
        }

        foreach (string f in ret) {
            Debug.Log(f);
        }
        return ret;
    }
    /// <summary>
    /// Change the current directory
    /// </summary>
    public static void cd(string newPath) {
        newPath = newPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        if (Directory.Exists(Path.GetFullPath(newPath))) {
            path = Path.GetFullPath(newPath);
            Debug.Log("Current path: '" + newPath + "'");
        } else {
            Debug.LogError("Incorrect path " + Path.GetFullPath(newPath));
        }

    }
    /// <summary>
    /// Print the current directory
    /// </summary>
    public static void pwd() {
        Debug.Log(path);
    }

    /// <summary>
    /// Return the lastly loaded UnityMolStructure
    /// </summary>
    public static UnityMolStructure last() {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return null;
        }
        return sm.loadedStructures.Last();
    }

    /// <summary>
    /// Change the background color of the camera based on a color name, also changes the fog color
    /// </summary>
    public static void bg_color(string colorS) {
        colorS = colorS.ToLower();
        Color col = strToColor(colorS);
        Color colprev = Camera.main.backgroundColor;
        Camera.main.backgroundColor = col;
        RenderSettings.fogColor = col;
        UnityMolMain.recordPythonCommand("bg_color(" + String.Format(CultureInfo.InvariantCulture, "{0})", col));
        UnityMolMain.recordUndoPythonCommand("bg_color(" + String.Format(CultureInfo.InvariantCulture, "{0})", colprev));
    }

    /// <summary>
    /// Change the background color of the camera, also changes the fog color
    /// </summary>
    public static void bg_color(Color col) {
        Color colprev = Camera.main.backgroundColor;
        Camera.main.backgroundColor = col;
        RenderSettings.fogColor = col;
        UnityMolMain.recordPythonCommand("bg_color(" + String.Format(CultureInfo.InvariantCulture, "{0})", col));
        UnityMolMain.recordUndoPythonCommand("bg_color(" + String.Format(CultureInfo.InvariantCulture, "{0})", colprev));
    }

    /// <summary>
    /// Convert a color string to a standard Unity Color
    /// Values can be "black", "white", "yellow" ,"green", "red", "blue", "pink", "gray"
    /// </summary>
    static Color strToColor(string input) {
        Color res = Color.black;
        switch (input) {
        case "black":
            return Color.black;
        case "white":
            return Color.white;
        case "yellow":
            return Color.yellow;
        case "green":
            return Color.green;
        case "red":
            return Color.red;
        case "blue":
            return Color.blue;
        case "pink":
            return new Color(1.0f, 0.75f, 0.75f);
        case "gray":
            return Color.gray;
        default:
            Debug.LogWarning("Unrecognized color");
            return Color.gray;
        }
    }

    /// <summary>
    /// Switch on or off the rotation around the X axis of all loaded molecules
    /// </summary>
    public static void switchRotateAxisX() {

        ManipulationManager mm = getManipulationManager();

        if (mm == null) {
            return;
        }

        if (mm.rotateX) {
            mm.rotateX = false;
        } else {
            mm.rotateX = true;
        }
        UnityMolMain.recordPythonCommand("switchRotateAxisX()");
        UnityMolMain.recordUndoPythonCommand("switchRotateAxisX()");
    }
    /// <summary>
    /// Switch on or off the rotation around the Y axis of all loaded molecules
    /// </summary>
    public static void switchRotateAxisY() {

        ManipulationManager mm = getManipulationManager();

        if (mm == null) {
            return;
        }

        if (mm.rotateY) {
            mm.rotateY = false;
        } else {
            mm.rotateY = true;
        }
        UnityMolMain.recordPythonCommand("switchRotateAxisY()");
        UnityMolMain.recordUndoPythonCommand("switchRotateAxisY()");
    }
    /// <summary>
    /// Switch on or off the rotation around the Z axis of all loaded molecules
    /// </summary>
    public static void switchRotateAxisZ() {

        ManipulationManager mm = getManipulationManager();

        if (mm == null) {
            return;
        }

        if (mm.rotateZ) {
            mm.rotateZ = false;
        } else {
            mm.rotateZ = true;
        }
        UnityMolMain.recordPythonCommand("switchRotateAxisZ()");
        UnityMolMain.recordUndoPythonCommand("switchRotateAxisZ()");
    }
    /// <summary>
    /// Change the rotation speed around the X axis
    /// </summary>
    public static void changeRotationSpeedX(float val) {

        ManipulationManager mm = getManipulationManager();

        if (mm == null) {
            return;
        }
        float prevVal = mm.speedX;
        mm.speedX = val;

        UnityMolMain.recordPythonCommand("changeRotationSpeedX(" + val.ToString("F3", culture) + ")");
        UnityMolMain.recordUndoPythonCommand("changeRotationSpeedX(" + prevVal.ToString("F3", culture) + ")");
    }
    /// <summary>
    /// Change the rotation speed around the Y axis
    /// </summary>
    public static void changeRotationSpeedY(float val) {

        ManipulationManager mm = getManipulationManager();

        if (mm == null) {
            return;
        }
        float prevVal = mm.speedY;
        mm.speedY = val;

        UnityMolMain.recordPythonCommand("changeRotationSpeedY(" + val.ToString("F3", culture) + ")");
        UnityMolMain.recordUndoPythonCommand("changeRotationSpeedY(" + prevVal.ToString("F3", culture) + ")");
    }

    /// <summary>
    /// Change the rotation speed around the Z axis
    /// </summary>
    public static void changeRotationSpeedZ(float val) {

        ManipulationManager mm = getManipulationManager();

        if (mm == null) {
            return;
        }
        float prevVal = mm.speedZ;
        mm.speedZ = val;

        UnityMolMain.recordPythonCommand("changeRotationSpeedZ(" + val.ToString("F3", culture) + ")");
        UnityMolMain.recordUndoPythonCommand("changeRotationSpeedZ(" + prevVal.ToString("F3", culture) + ")");
    }

    /// <summary>
    /// Change the mouse scroll speed
    /// </summary>
    public static void setMouseScrollSpeed(float val) {

        float prevVal = 0.0f;
        if (val > 0.0f) {
            ManipulationManager mm = getManipulationManager();

            if (mm == null) {
                return;
            }
            prevVal = mm.scrollSpeed;
            mm.scrollSpeed = val;
        } else {
            Debug.LogError("Wrong speed value");
        }
        UnityMolMain.recordPythonCommand("setMouseScrollSpeed(" + val.ToString("F3", culture) + ")");
        UnityMolMain.recordUndoPythonCommand("setMouseScrollSpeed(" + prevVal.ToString("F3", culture) + ")");
    }

    /// <summary>
    /// Change the speed of mouse rotations and translations
    /// </summary>
    public static void setMouseMoveSpeed(float val) {

        float prevVal = 0.0f;
        if (val > 0.0f) {
            ManipulationManager mm = getManipulationManager();

            if (mm == null) {
                return;
            }
            prevVal = mm.moveSpeed;
            mm.moveSpeed = val;
        } else {
            Debug.LogError("Wrong speed value");
            return;
        }
        UnityMolMain.recordPythonCommand("setMouseMoveSpeed(" + val.ToString("F3", culture) + ")");
        UnityMolMain.recordUndoPythonCommand("setMouseMoveSpeed(" + prevVal.ToString("F3", culture) + ")");
    }

    /// <summary>
    /// Stop rotation around all axis
    /// </summary>
    public static void stopRotations() {

        ManipulationManager mm = getManipulationManager();
        if (mm == null) {
            return;
        }

        mm.rotateX = false;
        mm.rotateY = false;
        mm.rotateZ = false;

        UnityMolMain.recordPythonCommand("stopRotations()");
        UnityMolMain.recordUndoPythonCommand("stopRotations()");
    }

    /// <summary>
    /// Turn docking mode on and off
    /// </summary>
    public static void switchDockingMode() {
        DockingManager dm = UnityMolMain.getDockingManager();

        if (dm.isRunning) {
            dm.stopDockingMode();
        } else {
            dm.startDockingMode();
        }
        UnityMolMain.recordPythonCommand("switchDockingMode()");
        UnityMolMain.recordUndoPythonCommand("switchDockingMode()");
    }

/// <summary>
    /// Set Raytracing material type
    /// 0 = Principled / 1 = carPaint / 2 = metal / 3 = alloy / 4 = glass / 5 = thinGlass / 6 = metallicPaint / 7 = luminous
    /// </summary>
    public static void setRTMaterialType(string selName, string rType, int matType) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            RepType repType = getRepType(rType);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {
                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.SetRTMaterialType(matType);
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.SetRTMaterialType(matType);
                            }
                        }
                    }
                }
            }
            else {
                Debug.LogError("Wrong representation type");
                return;
            }

        }
        else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setRTMaterialType(\"" + selName + "\", \"" + rType + "\", " + matType + ")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Set Raytracing material property
    /// </summary>
    public static void setRTMaterialProperty(string selName, string rType, string propName, object val) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            RepType repType = getRepType(rType);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {
                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null) {
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.SetRTMaterialProperty(propName, val);
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.SetRTMaterialProperty(propName, val);
                            }
                        }
                    }
                }
            }
            else {
                Debug.LogError("Wrong representation type");
                return;
            }

        }
        else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        string command = "setRTMaterialProperty(\"" + selName + "\", \"" + rType + "\", \"" + propName + "\"";
        if (val is string) {
            command += ", \"" + val.ToString() + "\")";
        } else if (val is float || val is double) {
            command += ", float(" + ((float) val).ToString("F3", culture) + "))";
        } else if (val is Vector3) {
            Vector3 v = (Vector3) val;
            command += ", " + cVec3ToPy(v) + ")";
        } else if (val is bool) {
            bool v = (bool) val;
            command += ", " + cBoolToPy(v) + ")";
        }
        else {
            command += ", " + val.ToString() + ")";
        }

        UnityMolMain.recordPythonCommand(command);
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Read a Raytracing material(s) from a json file (VTK material files) and store it in the RT material bank
    /// </summary>
    public static void loadRTMaterialsJSONFile(string filePath) {
        string realPath = filePath;
        string customPath = Path.Combine(path, filePath);
        if (File.Exists(customPath)) {
            realPath = customPath;
        }

        ReadOSPRayMaterialJson.readRTMatJson(realPath);

        UnityMolMain.recordPythonCommand("loadRTMaterialsJSONFile(" + cStringToPy(filePath) + ")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    public static void setRTMaterial(string selName, string rType, string matName) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        UnityMolRepresentationManager repManager = UnityMolMain.getRepresentationManager();

        if (selM.selections.ContainsKey(selName)) {
            RepType repType = getRepType(rType);
            if (repType.atomType != AtomType.noatom || repType.bondType != BondType.nobond) {
                List<UnityMolRepresentation> existingReps = repManager.representationExists(selName, repType);
                if (existingReps != null && RaytracingMaterial.materialsBank.ContainsKey(matName)) {
                    RaytracingMaterial curMat = RaytracingMaterial.materialsBank[matName];
                    foreach (UnityMolRepresentation existingRep in existingReps) {
                        foreach (SubRepresentation sr in existingRep.subReps) {
                            if (sr.atomRepManager != null) {
                                sr.atomRepManager.SetRTMaterial(curMat);
                            }
                            if (sr.bondRepManager != null) {
                                sr.bondRepManager.SetRTMaterial(curMat);
                            }
                        }
                    }
                }
            }
            else {
                Debug.LogError("Wrong representation type");
                return;
            }

        }
        else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }

        UnityMolMain.recordPythonCommand("setRTMaterial(" + cStringToPy(selName) + ", " + cStringToPy(rType) + ", " + cStringToPy(matName) + ")");
        UnityMolMain.recordUndoPythonCommand("");
    }


    public static void switchRTDenoiser(bool turnOn) {
        if (UnityMolMain.raytracingMode) {
            if (turnOn)
                RaytracerManager.Instance.forceDenoiserOff(false);
            else
                RaytracerManager.Instance.forceDenoiserOff(true);
        }
        UnityMolMain.recordPythonCommand("switchRTDenoiser(" + cBoolToPy(turnOn) + ")");
        UnityMolMain.recordUndoPythonCommand("switchRTDenoiser(" + cBoolToPy(!turnOn) + ")");
    }


    /// <summary>
    /// Transform a string of representation type to a RepType object
    /// </summary>
    public static RepType getRepType(string type) {

        type = type.ToLower();

        AtomType atype = AtomType.noatom;
        BondType btype = BondType.nobond;

        switch (type) {
        case "c":
        case "cartoon":
            atype = AtomType.cartoon;
            btype = BondType.nobond;
            break;
        case "s":
        case "surf":
        case "surface":
            atype = AtomType.surface;
            btype = BondType.nobond;
            break;
        case "dxiso":
            atype = AtomType.DXSurface;
            btype = BondType.nobond;
            break;
        case "hb":
        case "hyperball":
        case "hyperballs":
            atype = AtomType.optihb;
            btype = BondType.optihs;
            break;
        case "bondorder":
            atype = AtomType.bondorder;
            btype = BondType.bondorder;
            break;
        case "sphere":
        case "spheres":
            atype = AtomType.sphere;
            btype = BondType.nobond;
            break;

        case "l":
        case "line":
        case "lines":
            atype = AtomType.noatom;
            btype = BondType.line;
            break;
        case "hbond":
        case "hbonds":
            atype = AtomType.noatom;
            btype = BondType.hbond;
            break;
        case "hbondtube":
        case "hbondtubes":
            atype = AtomType.noatom;
            btype = BondType.hbondtube;
            break;
        case "fl":
        case "fieldlines":
        case "fieldline":
            atype = AtomType.fieldlines;
            btype = BondType.nobond;
            break;
        case "tube":
        case "trace":
            atype = AtomType.trace;
            btype = BondType.nobond;
            break;
        case "sugar":
        case "sugarribbons":
            atype = AtomType.sugarribbons;
            btype = BondType.nobond;
            break;
        case "sheherasade":
            atype = AtomType.sheherasade;
            btype = BondType.nobond;
            break;
        case "ellipsoid":
            atype = AtomType.ellipsoid;
            btype = BondType.nobond;
            break;
        case "p":
        case "point":
        case "points":
            atype = AtomType.point;
            btype = BondType.nobond;
            break;
        case "explo":
        case "exploded":
        case "explodedsurface":
        case "explosurface":
        case "explosurf":
            atype = AtomType.explosurf;
            btype = BondType.nobond;
            break;
        default:
            Debug.LogWarning("Unrecognized representation type '" + type + "'");
            break;
        }
        RepType result;
        result.atomType = atype;
        result.bondType = btype;
        return result;
    }

    /// <summary>
    /// Transform a representation type into a string
    /// </summary>
    public static string getTypeFromRepType(RepType rept) {
        if (rept.atomType == AtomType.cartoon && rept.bondType == BondType.nobond)
            return "cartoon";
        if (rept.atomType == AtomType.surface && rept.bondType == BondType.nobond)
            return "surface";
        if (rept.atomType == AtomType.DXSurface && rept.bondType == BondType.nobond)
            return "dxiso";
        if (rept.atomType == AtomType.optihb && rept.bondType == BondType.optihs)
            return "hyperball";
        if (rept.atomType == AtomType.bondorder && rept.bondType == BondType.bondorder)
            return "bondorder";
        if (rept.atomType == AtomType.sphere && rept.bondType == BondType.nobond)
            return "sphere";
        if (rept.atomType == AtomType.noatom && rept.bondType == BondType.line)
            return "line";
        if (rept.atomType == AtomType.noatom && rept.bondType == BondType.hbond)
            return "hbond";
        if (rept.atomType == AtomType.noatom && rept.bondType == BondType.hbondtube)
            return "hbondtube";
        if (rept.atomType == AtomType.fieldlines && rept.bondType == BondType.nobond)
            return "fieldlines";
        if (rept.atomType == AtomType.trace && rept.bondType == BondType.nobond)
            return "trace";
        if (rept.atomType == AtomType.sugarribbons && rept.bondType == BondType.nobond)
            return "sugarribbons";
        if (rept.atomType == AtomType.sheherasade && rept.bondType == BondType.nobond)
            return "sheherasade";
        if (rept.atomType == AtomType.ellipsoid && rept.bondType == BondType.nobond)
            return "ellipsoid";
        if (rept.atomType == AtomType.point && rept.bondType == BondType.nobond)
            return "point";
        if (rept.atomType == AtomType.explosurf && rept.bondType == BondType.nobond)
            return "explo";
        Debug.LogWarning("Not a predefined type");
        return "";
    }
    public static string cBoolToPy(bool val) {
        if (val) {
            return "True";
        }
        return "False";
    }

    public static string cVec3ToPy(Vector3 val) {
        return "Vector3(" + val.x.ToString("F3", culture) + ", " +
               val.y.ToString("F3", culture) + ", " +
               val.z.ToString("F3", culture) + ")";
    }

    private static string cStringToPy(string s) {
        return "\"" + s + "\"";
    }

    public static void activateExternalCommands() {

        if (extCom != null) {
            GameObject.DestroyImmediate(extCom);
        }
        extCom = instance.gameObject.AddComponent<TCPServerCommand>();

        UnityMolMain.recordPythonCommand("activateExternalCommands()");
        UnityMolMain.recordUndoPythonCommand("disableExternalCommands()");
    }

    public static void disableExternalCommands() {
        if (extCom != null) {
            GameObject.DestroyImmediate(extCom);
        }
        extCom = null;
        UnityMolMain.recordPythonCommand("disableExternalCommands()");
        UnityMolMain.recordUndoPythonCommand("activateExternalCommands()");
    }

    public static string getSelectionListString() {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        int N = selM.selections.Count;
        int id = 0;
        foreach (UnityMolSelection sel in selM.selections.Values) {
            sb.Append(sel.name);
            if (id != N - 1)
                sb.Append(", ");
            id++;
        }
        sb.Append("]");
        return sb.ToString();
    }
    public static string getStructureListString() {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        int N = sm.loadedStructures.Count;
        int id = 0;
        foreach (UnityMolStructure s in sm.loadedStructures) {
            sb.Append(s.name);
            if (id != N - 1)
                sb.Append(", ");
            id++;
        }
        sb.Append("]");
        return sb.ToString();
    }


    //-------------------Tour
    public static void clearTour() {
        getManipulationManager().clearTour();
    }

    public static void addSelectionToTour(string selName) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (selM.selections.ContainsKey(selName)) {
            ManipulationManager mm = getManipulationManager();
            mm.addTour(selM.selections[selName]);
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("addSelectionToTour(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("removeSelectionFromTour(\"" + selName + "\")");
    }

    public static void removeSelectionFromTour(string selName) {
        UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

        if (selM.selections.ContainsKey(selName)) {
            ManipulationManager mm = getManipulationManager();
            mm.removeFromTour(selM.selections[selName]);
        } else {
            Debug.LogWarning("No selection named '" + selName + "'");
            return;
        }
        UnityMolMain.recordPythonCommand("removeSelectionFromTour(\"" + selName + "\")");
        UnityMolMain.recordUndoPythonCommand("addSelectionToTour(\"" + selName + "\")");
    }

    //--------------- Annotations

    /// Measure modes : 0 = distance, 1 = angle, 2 = torsion angle
    public static void setMeasureMode(int newMode) {
        if (newMode < 0 || newMode > 2) {
            Debug.LogError("Measure mode should be between 0 and 2");
            return;
        }
        int prevVal = (int) UnityMolMain.measureMode;
        UnityMolMain.measureMode = (MeasureMode) newMode;

        UnityMolMain.recordPythonCommand("setMeasureMode(" + newMode + ")");
        UnityMolMain.recordUndoPythonCommand("setMeasureMode(" + prevVal + ")");
    }
    public static void annotateAtom(string structureName, int atomId) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s == null) {
            Debug.LogError("Wrong structure");
            return;
        }
        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        if (a != null) {
            anM.Annotate(a);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("annotateAtom(\"" + structureName + "\", " + atomId + ")");
        UnityMolMain.recordUndoPythonCommand("removeAnnotationAtom(\"" + structureName + "\", " + atomId + ")");
    }

    public static void removeAnnotationAtom(string structureName, int atomId) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s == null) {
            Debug.LogError("Wrong structure");
            return;
        }
        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);

        if (a != null) {
            SphereAnnotation sa = new SphereAnnotation();
            sa.atoms.Add(a);
            anM.RemoveAnnotation(sa);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("removeAnnotationAtom(\"" + structureName + "\", " + atomId + ")");
        UnityMolMain.recordUndoPythonCommand("annotateAtom(\"" + structureName + "\", " + atomId + ")");
    }

    public static void annotateSphere(Vector3 worldP, float scale = 1.0f) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();

        GameObject tmpSpherePar = new GameObject("WorldSphereAnnotation_" + worldP.x.ToString("F3", culture) + "_" +
                worldP.y.ToString("F3", culture) + "_" + worldP.z.ToString("F3", culture));

        tmpSpherePar.transform.position = worldP;
        anM.AnnotateSphere(tmpSpherePar.transform, scale);

        UnityMolMain.recordPythonCommand("annotateSphere(" + cVec3ToPy(worldP) + ", " + scale.ToString("F3", culture) + ")");

        UnityMolMain.recordUndoPythonCommand("removeAnnotationSphere(" + cVec3ToPy(worldP) + ", " + scale.ToString("F3", culture) + ")");
    }

    public static void removeAnnotationSphere(Vector3 worldP, float scale = 1.0f) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();

        SphereAnnotation sa = new SphereAnnotation();
        GameObject tmpSpherePar = new GameObject("WorldSphereAnnotation_" + worldP.x.ToString("F3", culture) + "_" +
                worldP.y.ToString("F3", culture) + "_" + worldP.z.ToString("F3", culture));

        tmpSpherePar.transform.parent = UnityMolMain.getRepresentationParent().transform;

        sa.annoParent = tmpSpherePar.transform;
        anM.RemoveAnnotation(sa);

        GameObject.Destroy(tmpSpherePar);

        UnityMolMain.recordPythonCommand("removeAnnotationSphere(" + cVec3ToPy(worldP) + ", " + scale.ToString("F3", culture) + ")");
        UnityMolMain.recordUndoPythonCommand("annotateSphere(" + cVec3ToPy(worldP) + ", " + scale.ToString("F3", culture) + ")");
    }

    public static void annotateAtomText(string structureName, int atomId, string text, Color textCol, bool showLine = false) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        if (a != null) {
            anM.AnnotateText(a, text, textCol, showLine);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("annotateAtomText(\"" + structureName + "\", " + atomId + ", \"" + text + "\", " + cBoolToPy(showLine) + ")");
        UnityMolMain.recordUndoPythonCommand("removeAnnotationAtomText(\"" + structureName + "\", " + atomId + ", \"" + text + "\")");
    }

    public static void removeAnnotationAtomText(string structureName, int atomId, string text) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s == null) {
            Debug.LogError("Wrong structure");
            return;
        }
        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        if (a != null) {
            TextAnnotation sa = new TextAnnotation();
            sa.atoms.Add(a);
            sa.content = text;
            anM.RemoveAnnotation(sa);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("removeAnnotationAtomText(\"" + structureName + "\", " + atomId + ", \"" + text + "\")");
        UnityMolMain.recordUndoPythonCommand("annotateAtomText(\"" + structureName + "\", " + atomId + ", \"" + text + "\")");
    }

    public static void annotateWorldText(Vector3 worldP, float scale, string text, Color textCol) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();

        GameObject tmpTextPar = new GameObject("WorldTextAnnotation_" + worldP.x.ToString("F3", culture) + "_" +
                                               worldP.y.ToString("F3", culture) + "_" + worldP.z.ToString("F3", culture));

        tmpTextPar.transform.parent = UnityMolMain.getRepresentationParent().transform.parent;

        tmpTextPar.transform.localPosition = worldP;
        tmpTextPar.transform.localRotation = Quaternion.identity;
        tmpTextPar.transform.localScale = Vector3.one;

        anM.AnnotateWorldText(tmpTextPar.transform, scale, text, textCol);

        UnityMolMain.recordPythonCommand("annotateWorldText(" + cVec3ToPy(worldP) + ", " + scale.ToString("F3", culture) + ", \"" + text + "\", " +
                                         String.Format(CultureInfo.InvariantCulture, "{0})", textCol));

        UnityMolMain.recordUndoPythonCommand("removeAnnotationWorldText(" + cVec3ToPy(worldP) + ", " + scale.ToString("F3", culture) + ", \"" + text + "\", " +
                                             String.Format(CultureInfo.InvariantCulture, "{0})", textCol));
    }

    public static void removeAnnotationWorldText(Vector3 worldP, float scale, string text, Color textCol) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();

        CustomTextAnnotation ta = new CustomTextAnnotation();

        GameObject tmpTextPar = new GameObject("WorldTextAnnotation_" + worldP.x.ToString("F3", culture) + "_" +
                                               worldP.y.ToString("F3", culture) + "_" + worldP.z.ToString("F3", culture));

        ta.annoParent = tmpTextPar.transform;

        ta.content = text;
        anM.RemoveAnnotation(ta);

        GameObject.Destroy(tmpTextPar);

        UnityMolMain.recordPythonCommand("removeAnnotationWorldText(" + cVec3ToPy(worldP) + ", " + scale.ToString("F3", culture) + ", \"" + text + "\", " +
                                         String.Format(CultureInfo.InvariantCulture, "{0})", textCol));

        UnityMolMain.recordUndoPythonCommand("annotateWorldText(" + cVec3ToPy(worldP) + ", " + scale.ToString("F3", culture) + ", \"" + text + "\", " +
                                             String.Format(CultureInfo.InvariantCulture, "{0})", textCol));
    }

    ///Add a 2D text over everything
    ///The position is set as 0/0 = bottom/left and 1/1 is top/right of the screen
    public static void annotate2DText(Vector2 screenP, float scale, string text, Color textCol) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();


        anM.Annotate2DText(text, scale, textCol, screenP);

        UnityMolMain.recordPythonCommand("annotate2DText(Vector2(" + screenP.x.ToString("F3", culture) + ", " +
                                         screenP.y.ToString("F3", culture) + "), " + scale.ToString("F3", culture) + ", \"" + text + "\", " +
                                         String.Format(CultureInfo.InvariantCulture, "{0})", textCol));

        UnityMolMain.recordUndoPythonCommand("removeAnnotation2DText(Vector2(" + screenP.x.ToString("F3", culture) + ", " +
                                             screenP.y.ToString("F3", culture) + "), " + scale.ToString("F3", culture) + ", \"" + text + "\", " +
                                             String.Format(CultureInfo.InvariantCulture, "{0})", textCol));
    }

    public static void removeAnnotation2DText(Vector2 screenP, float scale, string text, Color textCol) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();

        Annotate2D ta = new Annotate2D();


        ta.content = text;
        ta.posPercent = screenP;
        anM.RemoveAnnotation(ta);


        UnityMolMain.recordPythonCommand("removeAnnotation2DText(Vector2(" + screenP.x.ToString("F3", culture) + ", " +
                                         screenP.y.ToString("F3", culture) + "), " + scale.ToString("F3", culture) + ", \"" + text + "\", " +
                                         String.Format(CultureInfo.InvariantCulture, "{0})", textCol));

        UnityMolMain.recordUndoPythonCommand("annotateWorldText(Vector2(" + screenP.x.ToString("F3", culture) + ", " +
                                             screenP.y.ToString("F3", culture) + "), " + scale.ToString("F3", culture) + ", \"" + text + "\", " +
                                             String.Format(CultureInfo.InvariantCulture, "{0})", textCol));
    }

    public static void annotateLine(string structureName, int atomId, string structureName2, int atomId2) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        if (s == null || s2 == null) {
            Debug.LogError("Wrong structure");
            return;
        }
        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        if (a != null && a2 != null) {
            anM.AnnotateLine(a, a2);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("annotateLine(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
        UnityMolMain.recordUndoPythonCommand("removeAnnotationLine(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
    }

    public static void removeAnnotationLine(string structureName, int atomId, string structureName2, int atomId2) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        if (s == null || s2 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        if (a != null && a2 != null) {
            LineAtomAnnotation la = new LineAtomAnnotation();
            la.atoms.Add(a);
            la.atoms.Add(a2);
            anM.RemoveAnnotation(la);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("removeAnnotationLine(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
        UnityMolMain.recordUndoPythonCommand("annotateLine(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
    }

    public static void annotateWorldLine(Vector3 p1, Vector3 p2, float sizeLine, Color lineCol) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();

        anM.AnnotateWorldLine(p1, p2, UnityMolMain.getRepresentationParent().transform.parent, sizeLine, lineCol);

        UnityMolMain.recordPythonCommand("annotateWorldLine(" +  cVec3ToPy(p1) + ", " +
                                         cVec3ToPy(p2) + ", " +
                                         sizeLine.ToString("F3", culture) +
                                         String.Format(CultureInfo.InvariantCulture, ", {0})", lineCol));

        UnityMolMain.recordPythonCommand("removeWorldAnnotationLine(" + cVec3ToPy(p1) + ", " + cVec3ToPy(p2) + ", " +
                                         sizeLine.ToString("F3", culture) +
                                         String.Format(CultureInfo.InvariantCulture, ", {0})", lineCol));

    }

    public static void removeWorldAnnotationLine(Vector3 p1, Vector3 p2, float sizeLine, Color lineCol) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();

        CustomLineAnnotation la = new CustomLineAnnotation();
        la.start = p1;
        la.end = p2;
        anM.RemoveAnnotation(la);
        UnityMolMain.recordPythonCommand("removeWorldAnnotationLine(" + cVec3ToPy(p1) + ", " + cVec3ToPy(p2) + ", " +
                                         sizeLine.ToString("F3", culture) +
                                         String.Format(CultureInfo.InvariantCulture, ", {0})", lineCol));

        UnityMolMain.recordPythonCommand("annotateWorldLine(" + cVec3ToPy(p1) + ", " + cVec3ToPy(p2) + ", " +
                                         sizeLine.ToString("F3", culture) +
                                         String.Format(CultureInfo.InvariantCulture, ", {0})", lineCol));
    }

    public static void annotateDistance(string structureName, int atomId, string structureName2, int atomId2) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        if (s == null || s2 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        if (a != null && a2 != null) {
            anM.AnnotateDistance(a, a2);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("annotateDistance(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
        UnityMolMain.recordUndoPythonCommand("removeAnnotationDistance(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
    }

    public static void removeAnnotationDistance(string structureName, int atomId, string structureName2, int atomId2) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        if (s == null || s2 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        if (a != null && a2 != null) {
            DistanceAnnotation da = new DistanceAnnotation();
            da.atoms.Add(a);
            da.atoms.Add(a2);
            anM.RemoveAnnotation(da);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("removeAnnotationDistance(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
        UnityMolMain.recordUndoPythonCommand("annotateDistance(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
    }

    public static void annotateAngle(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        UnityMolStructure s3 = sm.GetStructure(structureName3);

        if (s == null || s2 == null || s3 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        UnityMolAtom a3 = s3.currentModel.getAtomWithID(atomId3);
        if (a != null && a2 != null && a3 != null) {
            anM.AnnotateAngle(a, a2, a3);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("annotateAngle(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                         atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ")");
        UnityMolMain.recordUndoPythonCommand("removeAnnotationAngle(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                             atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ")");
    }

    public static void removeAnnotationAngle(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        UnityMolStructure s3 = sm.GetStructure(structureName3);

        if (s == null || s2 == null || s3 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        UnityMolAtom a3 = s3.currentModel.getAtomWithID(atomId3);
        if (a != null && a2 != null && a3 != null) {

            AngleAnnotation aa = new AngleAnnotation();
            aa.atoms.Add(a);
            aa.atoms.Add(a2);
            aa.atoms.Add(a3);
            anM.RemoveAnnotation(aa);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }

        UnityMolMain.recordPythonCommand("removeAnnotationAngle(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                         atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ")");
        UnityMolMain.recordUndoPythonCommand("annotateAngle(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                             atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ")");
    }

    public static void annotateDihedralAngle(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3, string structureName4, int atomId4) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        UnityMolStructure s3 = sm.GetStructure(structureName3);
        UnityMolStructure s4 = sm.GetStructure(structureName4);

        if (s == null || s2 == null || s3 == null || s4 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        UnityMolAtom a3 = s3.currentModel.getAtomWithID(atomId3);
        UnityMolAtom a4 = s4.currentModel.getAtomWithID(atomId4);
        if (a != null && a2 != null && a3 != null && a4 != null) {
            anM.AnnotateDihedralAngle(a, a2, a3, a4);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("annotateDihedralAngle(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                         atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ", \"" + structureName4 + "\", " + atomId4 + ")");
        UnityMolMain.recordUndoPythonCommand("removeAnnotationDihedralAngle(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                             atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ", \"" + structureName4 + "\", " + atomId4 + ")");
    }

    public static void removeAnnotationDihedralAngle(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3, string structureName4, int atomId4) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        UnityMolStructure s3 = sm.GetStructure(structureName3);
        UnityMolStructure s4 = sm.GetStructure(structureName4);

        if (s == null || s2 == null || s3 == null || s4 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        UnityMolAtom a3 = s3.currentModel.getAtomWithID(atomId3);
        UnityMolAtom a4 = s4.currentModel.getAtomWithID(atomId4);
        if (a != null && a2 != null && a3 != null && a4 != null) {

            TorsionAngleAnnotation ta = new TorsionAngleAnnotation();
            ta.atoms.Add(a);
            ta.atoms.Add(a2);
            ta.atoms.Add(a3);
            ta.atoms.Add(a4);
            anM.RemoveAnnotation(ta);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("removeAnnotationDihedralAngle(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                         atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ", \"" + structureName4 + "\", " + atomId4 + ")");
        UnityMolMain.recordUndoPythonCommand("annotateDihedralAngle(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                             atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ", \"" + structureName4 + "\", " + atomId4 + ")");
    }

    public static void annotateRotatingArrow(string structureName, int atomId, string structureName2, int atomId2) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        if (s == null || s2 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        if (a != null && a2 != null) {
            anM.AnnotateDihedralArrow(a, a2);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("annotateRotatingArrow(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
        UnityMolMain.recordUndoPythonCommand("removeAnnotationRotatingArrow(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
    }

    public static void removeAnnotationRotatingArrow(string structureName, int atomId, string structureName2, int atomId2) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        if (s == null || s2 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        if (a != null && a2 != null) {
            ArrowAnnotation aa = new ArrowAnnotation();
            aa.atoms.Add(a);
            aa.atoms.Add(a2);
            anM.RemoveAnnotation(aa);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("removeAnnotationRotatingArrow(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
        UnityMolMain.recordUndoPythonCommand("annotateRotatingArrow(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " + atomId2 + ")");
    }

    public static void annotateArcLine(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        UnityMolStructure s3 = sm.GetStructure(structureName3);
        if (s == null || s2 == null || s3 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        UnityMolAtom a3 = s3.currentModel.getAtomWithID(atomId3);
        if (a != null && a2 != null && a3 != null) {
            anM.AnnotateCurvedLine(a, a2, a3);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("annotateArcLine(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                         atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ")");
        UnityMolMain.recordUndoPythonCommand("removeAnnotationArcLine(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                             atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ")");
    }

    public static void removeAnnotationArcLine(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        UnityMolStructure s2 = sm.GetStructure(structureName2);
        UnityMolStructure s3 = sm.GetStructure(structureName3);
        if (s == null || s2 == null || s3 == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        UnityMolAtom a = s.currentModel.getAtomWithID(atomId);
        UnityMolAtom a2 = s2.currentModel.getAtomWithID(atomId2);
        UnityMolAtom a3 = s3.currentModel.getAtomWithID(atomId3);
        if (a != null && a2 != null && a3 != null) {

            ArcLineAnnotation aa = new ArcLineAnnotation();
            aa.atoms.Add(a);
            aa.atoms.Add(a2);
            aa.atoms.Add(a3);
            anM.RemoveAnnotation(aa);
        } else {
            Debug.LogError("Wrong atom id");
            return;
        }
        UnityMolMain.recordPythonCommand("removeAnnotationArcLine(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                         atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ")");
        UnityMolMain.recordUndoPythonCommand("annotateArcLine(\"" + structureName + "\", " + atomId + ", \"" + structureName2 + "\", " +
                                             atomId2 + ", \"" + structureName3 + "\", " + atomId3 + ")");
    }

    public static void annotateDrawLine(string structureName, List<Vector3> line, Color col) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }

        UnityMolStructure s = sm.GetStructure(structureName);
        if (s == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        int id = -1;
        if (s != null) {
            id = anM.AnnotateDrawing(s, line, col);
        } else {
            Debug.LogError("Wrong structure name");
            return;
        }

        string command = "annotateDrawLine(\"" + structureName + "\",  List[Vector3]([";
        for (int i = 0; i < line.Count; i++) {
            command += cVec3ToPy(line[i]);
            if (i != line.Count - 1) {
                command += ", ";
            }
        }
        command += String.Format(CultureInfo.InvariantCulture, "]), {0}, )", col);
        UnityMolMain.recordPythonCommand(command);
        UnityMolMain.recordUndoPythonCommand("removeLastDrawLine(\"" + structureName + "\", " + id + ")");
    }

    public static void removeLastDrawLine(string structureName, int id) {

        UnityMolAnnotationManager anM = UnityMolMain.getAnnotationManager();
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }
        UnityMolStructure s = sm.GetStructure(structureName);
        if (s == null) {
            Debug.LogError("Wrong structure");
            return;
        }

        if (s != null && id != -1) {
            DrawAnnotation da = new DrawAnnotation();
            da.atoms.Add(s.currentModel.allAtoms[0]);
            da.id = id;
            anM.RemoveAnnotation(da);
        } else {
            Debug.LogError("Wrong structure name");
            return;
        }

        UnityMolMain.recordPythonCommand("removeLastDrawLine(\"" + structureName + "\", " + id + ")");
        UnityMolMain.recordUndoPythonCommand("");

    }

    /// <summary>
    /// Play a sonar sound at a world position
    /// </summary>
    public static void playSoundAtPosition(Vector3 wpos) {
        GameObject go = GameObject.Instantiate(Resources.Load("Prefabs/AudioSourceSonar")) as GameObject;
        go.transform.position = wpos;

        UnityMolMain.recordPythonCommand("playSoundAtPosition(" + cVec3ToPy(wpos) + ")");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Remove all drawing annotations
    /// </summary>
    public static void clearDrawings() {
        UnityMolAnnotationManager am = UnityMolMain.getAnnotationManager();
        am.CleanDrawings();
        UnityMolMain.recordPythonCommand("clearDrawings()");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Remove all annotations + Drawings
    /// </summary>
    public static void clearAnnotations() {
        UnityMolMain.getAnnotationManager().Clean();
        UnityMolMain.recordPythonCommand("clearAnnotations()");
        UnityMolMain.recordUndoPythonCommand("");
    }

    /// <summary>
    /// Export the given structure to an OBJ file containing several meshes
    /// BondOrder/Point/Hbonds are ignored
    /// </summary>
    public static void exportRepsToOBJFile(string structureName, string fullPath, bool withAO = true) {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }
        UnityMolStructure s = sm.GetStructure(structureName);

        if (s != null) {
            List<GameObject> repGameObjects = new List<GameObject>();
            foreach (UnityMolRepresentation r in s.representations) {
                if (r.repType.atomType == AtomType.bondorder ||
                        r.repType.atomType == AtomType.fieldlines ||
                        r.repType.atomType == AtomType.ellipsoid ||
                        r.repType.atomType == AtomType.point ||
                        r.repType.bondType == BondType.hbond) {
                    Debug.LogWarning("Ignoring point/hbond/bondorder/fl/ellipsoid representation");
                    continue;
                }
                if (r.repType.atomType == AtomType.optihb ||
                        r.repType.bondType == BondType.optihs)
                    continue;//Don't export the cubes

                foreach (SubRepresentation sr in r.subReps) {
                    if (sr.atomRepManager != null) {
                        if (sr.atomRep.representationTransform != null) {
                            repGameObjects.Add(sr.atomRep.representationTransform.gameObject);
                            break;
                        }
                    }
                    if (sr.bondRepManager != null) {
                        if (sr.bondRep.representationTransform != null) {
                            repGameObjects.Add(sr.bondRep.representationTransform.gameObject);
                        }
                    }
                }
            }

            //Export all hyperballs to mesh
            List<GameObject> hbMeshesGo = ExtractHyperballMesh.getAllHBForStructure(s);
            repGameObjects.AddRange(hbMeshesGo);

            string objString = ObjExporter.DoExport(repGameObjects, true, withAO);
            try {
                using(StreamWriter sw = new StreamWriter(fullPath, false)) {
                    sw.Write(objString);
                    sw.Close();
                }
                Debug.Log("Exported " + repGameObjects.Count + " representations to " + fullPath);
            } catch (System.Exception e) {
                Debug.LogError("Failed to write to " + fullPath);
#if UNITY_EDITOR
                Debug.LogError(e);
#endif
                for (int i = 0; i < hbMeshesGo.Count; i++) {
                    GameObject.Destroy(hbMeshesGo[i].GetComponent<MeshFilter>().sharedMesh);
                    GameObject.Destroy(hbMeshesGo[i]);
                }
                return;
            }
            for (int i = 0; i < hbMeshesGo.Count; i++) {
                GameObject.Destroy(hbMeshesGo[i].GetComponent<MeshFilter>().sharedMesh);
                GameObject.Destroy(hbMeshesGo[i]);
            }
        } else {
            Debug.LogError("Structure not found");
        }
    }

    /// <summary>
    /// Export the given structure to an FBX file containing several meshes, (Windows or Mac)
    /// BondOrder/Point/Hbonds/Fieldlines are ignored
    /// </summary>
    public static void exportRepsToFBXFile(string structureName, string fullPath, bool withAO = true) {
#if !UNITY_EDITOR_WIN && !UNITY_EDITOR_OSX && !UNITY_STANDALONE_OSX && !UNITY_STANDALONE_WIN
        Debug.LogError("FBX export is only available on Windows/MacOS");
        return;
#else
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();

        if (sm.loadedStructures.Count == 0) {
            Debug.LogWarning("No molecule loaded");
            return;
        }
        UnityMolStructure s = sm.GetStructure(structureName);

        if (s != null) {
            List<GameObject> repGameObjects = new List<GameObject>();
            foreach (UnityMolRepresentation r in s.representations) {
                if (r.repType.atomType == AtomType.bondorder ||
                        r.repType.atomType == AtomType.point ||
                        r.repType.atomType == AtomType.fieldlines ||
                        r.repType.atomType == AtomType.ellipsoid ||
                        r.repType.bondType == BondType.hbond) {
                    Debug.LogWarning("Ignoring point/hbond/bondorder/fl/ellipsoid representation");
                    continue;
                }
                if (r.repType.atomType == AtomType.optihb ||
                        r.repType.bondType == BondType.optihs)
                    continue;//Don't export the cubes

                foreach (SubRepresentation sr in r.subReps) {
                    if (sr.atomRepManager != null) {
                        if (sr.atomRep.representationTransform != null) {
                            repGameObjects.Add(sr.atomRep.representationTransform.gameObject);
                            break;
                        }
                    }
                    if (sr.bondRepManager != null) {
                        if (sr.bondRep.representationTransform != null) {
                            repGameObjects.Add(sr.bondRep.representationTransform.gameObject);
                        }
                    }
                }
            }

            //Export all hyperballs to mesh
            List<GameObject> hbMeshesGo = ExtractHyperballMesh.getAllHBForStructure(s);
            repGameObjects.AddRange(hbMeshesGo);

            try {
                FBXExporter.writeMesh(repGameObjects, fullPath, withAO);
                Debug.Log("Exported " + repGameObjects.Count + " representations to " + fullPath);
            } catch {
                Debug.LogError("Failed to write to " + fullPath);
                for (int i = 0; i < hbMeshesGo.Count; i++) {
                    GameObject.Destroy(hbMeshesGo[i].GetComponent<MeshFilter>().sharedMesh);
                    GameObject.Destroy(hbMeshesGo[i]);
                }
                return;
            }

            for (int i = 0; i < hbMeshesGo.Count; i++) {
                GameObject.Destroy(hbMeshesGo[i].GetComponent<MeshFilter>().sharedMesh);
                GameObject.Destroy(hbMeshesGo[i]);
            }
        } else {
            Debug.LogError("Structure not found");
        }
#endif
    }

//     /// <summary>
//     /// Export the given structure to an GLTF binary file containing several meshes
//     /// Example: exportRepsToGLTFFile(last().name, 'C:/Users/MyName/Desktop/myexportname')
//     /// BondOrder/Point/Hbonds/Fieldlines are ignored
//     /// </summary>
//     public static void exportRepsToGLTFFile(string structureName, string fullPath, bool withAO = true) {

//         UnityMolStructureManager sm = UnityMolMain.getStructureManager();
//         if (sm.loadedStructures.Count == 0) {
//             Debug.LogWarning("No molecule loaded");
//             return;
//         }
//         UnityMolStructure s = sm.GetStructure(structureName);

//         if (s != null) {
//             List<GameObject> repGameObjects = new List<GameObject>();
//             foreach (UnityMolRepresentation r in s.representations) {
//                 if (r.repType.atomType == AtomType.bondorder ||
//                         r.repType.atomType == AtomType.point ||
//                         r.repType.atomType == AtomType.ellipsoid ||
//                         r.repType.atomType == AtomType.fieldlines ||
//                         r.repType.bondType == BondType.hbond) {
//                     Debug.LogWarning("Ignoring point/hbond/bondorder/fl/ellipsoid representation");
//                     continue;
//                 }
//                 if (r.repType.atomType == AtomType.optihb ||
//                         r.repType.bondType == BondType.optihs)
//                     continue;//Don't export the cubes

//                 foreach (SubRepresentation sr in r.subReps) {
//                     if (sr.atomRepManager != null) {
//                         if (sr.atomRep.representationTransform != null) {
//                             repGameObjects.Add(sr.atomRep.representationTransform.gameObject);
//                             break;
//                         }
//                     }
//                     if (sr.bondRepManager != null) {
//                         if (sr.bondRep.representationTransform != null) {
//                             repGameObjects.Add(sr.bondRep.representationTransform.gameObject);
//                         }
//                     }
//                 }
//             }

//             //Export all hyperballs to mesh
//             List<GameObject> hbMeshesGo = ExtractHyperballMesh.getAllHBForStructure(s, true);
//             repGameObjects.AddRange(hbMeshesGo);

//             string path = Path.GetDirectoryName(fullPath);
//             string fileName = Path.GetFileNameWithoutExtension(fullPath);

//             try {
//                 GLTFExporter.DoExport(repGameObjects, path, fileName);

//                 Debug.Log("Exported " + repGameObjects.Count + " representations to " + fullPath);
//             } catch (System.Exception e) {
//                 Debug.LogError("Failed to write to " + path + fileName);
// #if UNITY_EDITOR
//                 Debug.LogError(e);
// #endif
//                 for (int i = 0; i < hbMeshesGo.Count; i++) {
//                     GameObject.Destroy(hbMeshesGo[i].GetComponent<MeshFilter>().sharedMesh);
//                     GameObject.Destroy(hbMeshesGo[i]);
//                 }
//                 if (ExtractHyperballMesh.extHBMat != null)
//                     Destroy(ExtractHyperballMesh.extHBMat);
//                 return;
//             }
//             for (int i = 0; i < hbMeshesGo.Count; i++) {
//                 GameObject.Destroy(hbMeshesGo[i].GetComponent<MeshFilter>().sharedMesh);
//                 GameObject.Destroy(hbMeshesGo[i]);
//             }
//             if (ExtractHyperballMesh.extHBMat != null)
//                 Destroy(ExtractHyperballMesh.extHBMat);
//         } else {
//             Debug.LogError("Structure not found");
//         }
//     }

}
}
}
