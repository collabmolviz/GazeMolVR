using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;

namespace UMol {

/// <summary>
/// Part of the SMCRA data structure, UnityMolStructure stores the models of the structure
/// </summary>
public class UnityMolStructure {

    /// <summary>
    /// Store all the models of the structure
    /// </summary>
    public List<UnityMolModel> models;

    /// <summary>
    /// Parsed structure name
    /// </summary>
    public string parsedName;

    /// <summary>
    /// Unique structure name
    /// </summary>
    public string name;

    /// <summary>
    /// Path the structure was loaded from (null if fetch)
    /// </summary>
    public string path = null;

    /// <summary>
    /// If fetched, PDBID queried (null if from file)
    /// </summary>
    public string pdbID = null;

    /// <summary>
    /// Class to perform spatial searches
    /// </summary>
    public SpatialSearch spatialSearch;

    /// <summary>
    /// Identified type of molecular structure (all-atoms/Martini/Hire-RNA/OPEP)
    /// </summary>
    public MolecularType structureType = MolecularType.standard;

    /// <summary>
    /// List of representations for this structure
    /// </summary>
    public List<UnityMolRepresentation> representations;

    /// <summary>
    /// Trajectory loaded for this structure
    /// </summary>
    public bool trajectoryLoaded = false;

    /// <summary>
    /// Recompute secondary structure assignation for each step of the trajectory
    /// </summary>
    public bool updateSSWithTraj = false;

    /// <summary>
    /// Atom positions used when reading a trajectory file
    /// </summary>
    public Vector3[] trajAtomPositions;

    /// <summary>
    /// Trajectory reader using xdrfile library
    /// </summary>
    public XDRFileReader xdr;

    /// <summary>
    /// Trajectory reader using xdrfile library
    /// </summary>
    public MDDriverManager mddriverM;

    /// <summary>
    /// Trajectory player calling trajNext from a monobehaviour Update loop
    /// </summary>
    public TrajectoryPlayer trajPlayer;

    /// <summary>
    /// Multiple models should be read as a trajectory
    /// </summary>
    public bool trajectoryMode = false;

    /// <summary>
    /// If trajectoryMode is true, for each frame/model, store the positions of each atom
    /// </summary>
    public List<Vector3[]> modelFrames;

    /// <summary>
    /// Dx file reader
    /// </summary>
    public DXReader dxr;

    /// <summary>
    /// Secondary structures were parsed
    /// </summary>
    public bool ssInfoFromFile = false;

    /// <summary>
    /// Secondary structures parsed from the file
    /// </summary>
    public List<Reader.secStruct> parsedSSInfo;

    /// <summary>
    /// Connectivity parsed from the file
    /// </summary>
    public List<int2> parsedConnectivity;

    /// <summary>
    /// Symmetry matrices parsed from file
    /// </summary>
    public List<Matrix4x4> symMatrices;

    /// <summary>
    /// Ignore this structure in docking mode
    /// </summary>
    public bool ignoreDocking = false;

    /// <summary>
    /// Class holding threads to compute surfaces started when creating the structure
    /// </summary>
    public SurfaceThread surfThread;

    /// <summary>
    /// Current model used
    /// </summary>
    public UnityMolModel currentModel {
        get
        {
            if (models == null || currentModelId < 0 || currentModelId >= models.Count)
                return null;
            return models[currentModelId];
        }
    }

    /// <summary>
    /// Current model id used
    /// </summary>
    public int currentModelId = 0;

    /// <summary>
    /// Current frame id used, when trajectoryMode is true
    /// </summary>
    public int currentFrameId = 0;

    /// <summary>
    /// Monobehaviour script to read models like trajectories
    /// </summary>
    public ModelsPlayer modelsPlayer;

    /// <summary>
    /// Id of the group used to move the molecules
    /// </summary>
    public int groupID = 0;

    // /// <summary>
    // /// Associates a GameObject with an UnityMolAtom
    // /// </summary>
    // public Dictionary<UnityMolAtom, GameObject> atomToGo;

    /// <summary>
    /// Parent of the annotations gameObjects
    /// Filled by Reader.CreateUnityGameObjects
    /// </summary>
    public Transform annotationParent;

    /// <summary>
    /// Periodic information from PDB CRYST1 / mmCIF cell.length
    /// </summary>
    public Vector3 periodic = Vector3.one;

    /// Fetched with bioAssembly on
    public bool bioAssembly;

    /// Fetched with mmCIF on
    public bool fetchedmmCIF;

    /// Fetched with readHET on
    public bool readHET = true;

    /// Fetched with modelsAsTraj on
    public bool modelsAsTraj;

    /// Current dx file loaded path
    public string dxPath;

    private GameObject[] boundBoxLines;

    private Material lineMat;

    public delegate void OnNewFrame();
    public event OnNewFrame NewFrameDeleg;

    public enum MolecularType {
        standard = 0,//All-atoms
        Martini = 1,
        OPEP = 2,
        HIRERNA = 3
    }

    private float bboxLineSize = 0.005f;


    /// <summary>
    /// UnityMolStructure constructor taking a list of models as arg
    /// </summary>
    public UnityMolStructure(List<UnityMolModel> listModels, string nameStructure) {
        models = listModels;
        parsedName = nameStructure;
        name = nameStructure;

        if (UnityMolMain.getStructureManager().isNameUsed(name)) {
            name = UnityMolMain.getStructureManager().findNewStructureName(name);
        }

        representations = new List<UnityMolRepresentation>();
        currentModelId = 0;
        currentFrameId = 0;

        createModelPlayer();
        createBoundingBoxLines(); hideBoundingBox();

        spatialSearch = new SpatialSearch(currentModel.allAtoms);
    }

    /// <summary>
    /// UnityMolStructure constructor taking a model as arg
    /// </summary>
    public UnityMolStructure(UnityMolModel newModel, string nameStructure) {
        models = new List<UnityMolModel>();
        models.Add(newModel);
        parsedName = nameStructure;
        name = nameStructure;

        if (UnityMolMain.getStructureManager().isNameUsed(name)) {
            name = UnityMolMain.getStructureManager().findNewStructureName(name);
        }

        representations = new List<UnityMolRepresentation>();
        currentModelId = 0;
        currentFrameId = 0;
        createModelPlayer();
        createBoundingBoxLines(); hideBoundingBox();

        spatialSearch = new SpatialSearch(currentModel.allAtoms);
    }


    /// <summary>
    /// UnityMolStructure constructor taking a list of models and a list of frames
    /// </summary>
    public UnityMolStructure(List<UnityMolModel> listModels, string nameStructure, List<Vector3[]> frames) {
        models = listModels;
        parsedName = nameStructure;
        name = nameStructure;
        modelFrames = frames;
        trajectoryMode = true;

        if (UnityMolMain.getStructureManager().isNameUsed(name)) {
            name = UnityMolMain.getStructureManager().findNewStructureName(name);
        }

        representations = new List<UnityMolRepresentation>();
        currentModelId = 0;
        currentFrameId = 0;
        createModelPlayer();
        createBoundingBoxLines(); hideBoundingBox();

        spatialSearch = new SpatialSearch(currentModel.allAtoms);
    }

    /// <summary>
    /// Returns the number of atoms in the current model
    /// </summary>
    public int Count {
        get {return currentModel.allAtoms.Count;}
    }
    /// <summary>
    /// Returns the number of atoms in the current model
    /// </summary>
    public int Length {
        get {return Count;}
    }


    /// <summary>
    /// Update atom radii and colors of all atoms based on the detected structure molecular type (Martini/HireRNA...)
    /// </summary>
    public void updateAtomRepValues() {
        string prefix = UnityMolMain.topologies.prefixMolType[structureType];


        foreach (UnityMolModel m in models) {
            foreach (UnityMolAtom a in m.allAtoms) {
                if (structureType == MolecularType.Martini) {
                    a.SetAtomRepresentationModel(prefix + a.residue.name + "_");
                }
                else {
                    a.SetAtomRepresentationModel(prefix);
                }
            }
        }
    }

    public void readTrajectoryXDR(string trajPath) {
        if (xdr == null && !trajectoryLoaded) {
            xdr = new XDRFileReader();
        }
        int result = xdr.open_trajectory(this, trajPath);
        if (result >= 0 ) {
            xdr.load_trajectory();
        }
        else if (result == (int) XDRFileReaderStatus.TRAJECTORYPRESENT) {
            throw new System.Exception("Trajectory already exists");
        }
        else {
            unloadTrajectoryXDR();
            throw new System.Exception("Trajectory reader failure " + result);
        }
    }

    ///Deleting flag is used to avoid updating representation when deleting the structure
    public void unloadTrajectoryXDR(bool deleting = false) {
        if (xdr != null) {
            xdr.Clear();
            xdr = null;
            for (int i = 0; i < currentModel.allAtoms.Count; i++) {
                currentModel.allAtoms[i].position = currentModel.allAtoms[i].oriPosition;
            }
            currentModel.ComputeCentroid();

            spatialSearch.UpdatePositions(currentModel.allAtoms);

            if (!deleting)
                updateRepresentations(trajectory: false);

            trajAtomPositions = null;
        }

        if (trajPlayer) {
            GameObject.DestroyImmediate(trajPlayer);
        }
#if !DISABLE_HIGHLIGHT
        UnityMolHighlightManager hM = UnityMolMain.getHighlightManager();
        hM.Clean();
#endif

    }

    public void readDX(string dxpath) {
        if (dxr != null) {
            unloadDX();
        }
        try {
            dxr = new DXReader(dxpath);
            dxr.readDxFile(this);
            dxPath = dxpath;
        }
        catch {
            throw new System.Exception("DX map reader failure");
        }

    }

    public void unloadDX() {
        dxPath = null;
        if (dxr != null) {
            dxr.destroyLines();
            dxr.densityValues = null;
            dxr = null;
        }
    }

    public void createModelPlayer() {
        if (models.Count > 1 || (trajectoryMode && modelFrames != null && modelFrames.Count > 1) ) {
            UnityMolStructureManager sm = UnityMolMain.getStructureManager();
            GameObject structureParent = null;
            try {
                structureParent = sm.GetStructureGameObject(name);
            }
            catch {
                string sName = ToSelectionName();
                structureParent = UnityMolMain.getRepStructureParent(sName);
            }

            modelsPlayer = structureParent.AddComponent<ModelsPlayer>();
            modelsPlayer.play = false;
            modelsPlayer.s = this;
        }
    }

    public void modelNext(bool forward = true, bool loop = true) {
        if (trajectoryMode) {
            if (modelFrames.Count > 1) {
                if (forward) {
                    if (currentFrameId + 1 >= modelFrames.Count) {
                        if (loop) {
                            currentFrameId = 0;
                        }
                        else {
                            currentFrameId = modelFrames.Count - 1;
                        }
                    }
                    else {
                        currentFrameId++;
                    }
                }
                else {
                    if (currentFrameId - 1 < 0) {
                        if (loop) {
                            currentFrameId = modelFrames.Count - 1;
                        }
                        else {
                            currentFrameId = 0;
                        }
                    }
                    else {
                        currentFrameId--;
                    }
                }
                API.APIPython.setModel(name, currentFrameId);
            }
        }
        else {


            if (models.Count > 1) {
                if (forward) {
                    if (currentModelId + 1 >= models.Count) {
                        if (loop) {
                            currentModelId = 0;
                        }
                        else {
                            currentModelId = models.Count - 1;
                        }
                    }
                    else {
                        currentModelId++;
                    }
                }
                else {
                    if (currentModelId - 1 < 0) {
                        if (loop) {
                            currentModelId = models.Count - 1;
                        }
                        else {
                            currentModelId = 0;
                        }
                    }
                    else {
                        currentModelId--;
                    }
                }
                API.APIPython.setModel(name, currentModelId);
            }
        }
        updateBoundingBox();
    }

    public void setModel(int modelId) {
        if (trajectoryMode && modelFrames != null && modelFrames.Count > 1) {
            if (modelId >= 0 && modelId < modelFrames.Count) {
                trajAtomPositions = modelFrames[modelId];
                trajUpdateAtomPositions();
                currentFrameId = modelId;

                updateRepresentations(trajectory: true);
            }
            else {
                Debug.LogWarning("Wrong model number");
            }
            return;
        }

        if (models.Count > 1) {
            if (modelId >= 0 && modelId < models.Count) {
                currentModelId = modelId;
                updateRepresentations(trajectory: false);
            }
            else {
                Debug.LogWarning("Wrong model number");
                return;
            }
            return;
        }
        Debug.LogError("This structure does not contain several models");
    }

    public void trajNext(bool forward = true, bool loop = true, bool windowMean = false, int windowSize = 5) {
        if (xdr != null && trajectoryLoaded) {
            int newFrameId = 0;
            if (forward) {
                newFrameId = xdr.currentFrame + 1;
                if (newFrameId >= xdr.numberFrames) {
                    if (loop) {
                        xdr.sync_scene_with_frame(0);
                    }
                    else {
                        xdr.sync_scene_with_frame(xdr.numberFrames - 1);
                    }
                }
                else {
                    xdr.sync_scene_with_frame(newFrameId, windowMean, windowSize, forward);
                }
            }
            else {
                newFrameId = xdr.currentFrame - 1;
                if (newFrameId < 0) {
                    if (loop) {
                        xdr.sync_scene_with_frame(xdr.numberFrames - 1);
                    }
                    else {
                        xdr.sync_scene_with_frame(0);
                    }
                }
                else {
                    xdr.sync_scene_with_frame(newFrameId, windowMean, windowSize, forward);
                }
            }

            updateRepresentations();
        }
        else {
            Debug.LogError("No trajectory loaded for this structure");
        }
    }

    public void trajNextSmooth(float t, bool forward = true, bool loop = true, bool newFrame = false) {
        if (xdr != null && trajectoryLoaded) {
            int newFrameId = xdr.currentFrame;
            if (forward) {
                if (newFrame) {
                    newFrameId = xdr.currentFrame + 1;
                }
                if (newFrameId + 1 >= xdr.numberFrames) {
                    if (loop) {
                        xdr.sync_scene_with_frame(0);
                    }
                    else
                        xdr.sync_scene_with_frame(xdr.numberFrames - 1);
                }
                else {
                    xdr.sync_scene_with_frame_smooth(newFrameId, newFrameId + 1, t, newFrame);
                }
            }
            else {
                if (newFrame) {
                    newFrameId = xdr.currentFrame - 1;
                }
                if (newFrameId - 1 < 0) {
                    if (loop) {
                        xdr.sync_scene_with_frame(xdr.numberFrames - 1);
                    }
                    else {
                        xdr.sync_scene_with_frame(0);
                    }
                }
                else {
                    xdr.sync_scene_with_frame_smooth(newFrameId, newFrameId - 1, t, newFrame);
                }
            }

            updateRepresentations();
        }
        else {
            Debug.LogError("No trajectory loaded for this structure");
        }
    }


    public void trajSetFrame(int idF) {
        if (xdr != null && trajectoryLoaded) {
            if (idF >= 0 && idF < xdr.numberFrames) {
                xdr.sync_scene_with_frame(idF);
                updateRepresentations();
            }
            else {
                Debug.LogWarning("Wrong frame number");
            }
        }
        else {
            Debug.LogError("No trajectory loaded for this structure");
        }
    }

    /// <summary>
    /// Update positions of GameObject for atoms with an annotation and update representations with new positions
    /// </summary>
    public void updateRepresentations(bool trajectory = true) {

        UnityMolMain.getPrecompRepManager().Clear(name);
        UnityMolModel curM = currentModel;
        List<UnityMolAtom> atoms = curM.allAtoms;
        int N = curM.Count;

        if (trajectory) {

            //Update annotation positions
            UnityMolMain.getAnnotationManager().UpdateAtomPositions();

            //Not needed anymore (Update GameObject for each atom)
            // for (int idA = 0; idA < N; idA++) {
            //     UnityMolAtom a = atoms[idA];
            //     if (float.IsNaN(trajAtomPositions[idA].x) || float.IsInfinity(trajAtomPositions[idA].x) ||
            //             float.IsNaN(trajAtomPositions[idA].y) || float.IsInfinity(trajAtomPositions[idA].y) ||
            //             float.IsNaN(trajAtomPositions[idA].z) || float.IsInfinity(trajAtomPositions[idA].z)) {
            //         // gos[idA].transform.localPosition = Vector3.zero;
            //         atomGos[idA].transform.localPosition = Vector3.zero;
            //     }
            //     else {
            //         // gos[idA].transform.localPosition = trajAtomPositions[idA];
            //         atomGos[idA].transform.localPosition = trajAtomPositions[idA];
            //     }
            //     //Stop if trajectory contains less atoms than the model
            //     if (idA >= trajAtomPositions.Length) {
            //         break;
            //     }
            // }

            if (updateSSWithTraj) {
                DSSP.assignSS_DSSP(this, isTraj: true);
                ssInfoFromFile = false;
            }

            UnityMolMain.getSelectionManager().updateSelectionContentTrajectory(this);

            for (int i = 0; i < representations.Count; i++) {
                UnityMolRepresentation rep = representations[i];
                rep.updateWithTrajectory();
            }

        }
        else {
            //Not needed anymore (Update GameObject for each atom)
            // foreach (UnityMolAtom a in atoms) {
            //     atomToGo[a].transform.localPosition = a.position;
            // }

            //Update annotation positions
            UnityMolMain.getAnnotationManager().UpdateAtomPositions();

            UnityMolMain.getSelectionManager().updateSelectionsWithNewModel(this);

            foreach (UnityMolSelection sel in UnityMolMain.getSelectionManager().selections.Values) {
                if (sel.structures.Contains(this)) {
                    foreach (List<UnityMolRepresentation> reps in sel.representations.Values) {
                        foreach (UnityMolRepresentation r in reps) {
                            r.updateWithNewSelection(sel);
                        }
                    }
                }
            }
            // for (int i = 0; i < representations.Count; i++) {
            //  UnityMolRepresentation rep = representations[i];
            //  rep.updateWithModel();
            // }
        }

        UnityMolMain.getCustomRaycast().needsFullUpdate = true;



#if !DISABLE_HIGHLIGHT
        UnityMolHighlightManager hM = UnityMolMain.getHighlightManager();
        hM.Clean();
#endif

    }


    /// <summary>
    /// Updates each atom position of the current model of the structure based on the trajectory
    /// Uses center of gravity from the first model of the structure !
    /// </summary>
    public void trajUpdateAtomPositions() {
        int N = Mathf.Min(currentModel.allAtoms.Count, trajAtomPositions.Length);
        UnityMolModel m = currentModel;
        List<UnityMolAtom> atoms = m.allAtoms;
        for (int i = 0; i < N; i++) {
            atoms[i].position.x = trajAtomPositions[i].x;
            atoms[i].position.y = trajAtomPositions[i].y;
            atoms[i].position.z = trajAtomPositions[i].z;
        }
        currentModel.centroid = CenterOfGravBurst.computeCOG(trajAtomPositions,
                                ref currentModel.minimumPositions, ref currentModel.maximumPositions);
        updateBoundingBox();
        UnityMolMain.getCustomRaycast().needsUpdatePos = true;

        spatialSearch.UpdatePositions(atoms);

        if (NewFrameDeleg != null)
            NewFrameDeleg();

    }

    public void createTrajectoryPlayer() {
        if (trajPlayer == null) {
            UnityMolStructureManager sm = UnityMolMain.getStructureManager();
            GameObject structureParent = null;
            try {
                structureParent = sm.GetStructureGameObject(name);
            }
            catch {
                string sName = ToSelectionName();
                structureParent = UnityMolMain.getRepStructureParent(sName);
            }

            trajPlayer = structureParent.AddComponent<TrajectoryPlayer>();
            trajPlayer.play = true;
            trajPlayer.s = this;
        }
        else {
            Debug.LogWarning("Trajectory player already exists for this structure");
        }
    }

    /// <summary>
    /// Split different models read in a file into several UnityMolStructure
    /// </summary>
    public List<UnityMolStructure> splitModelsInStructures() {
        List<UnityMolStructure> result = new List<UnityMolStructure>();

        if (modelsAsTraj) {

            int oriModel = currentModelId;

            for (int i = 0; i < modelFrames.Count; i++) {
                UnityMolModel m = currentModel;

                //Move atoms to the model coordinates
                Vector3[] p = modelFrames[i];
                int ida = 0;
                foreach (UnityMolAtom a in currentModel.allAtoms) {
                    a.position = p[ida];
                    ida++;
                }

                UnityMolModel newModel = m.Clone();

                UnityMolStructure newS = new UnityMolStructure(newModel, name + "_" + currentModelId);
                newS.models[0].structure = newS;

                newS.trajectoryLoaded = false;
                newS.ssInfoFromFile = ssInfoFromFile;

                result.Add(newS);

                UnityMolSelection sel = newS.ToSelection();

                Reader.CreateUnityObjects(newS.ToSelectionName(), sel);
                UnityMolMain.getStructureManager().AddStructure(newS);
                UnityMolMain.getSelectionManager().Add(sel);
            }
            //Restore current atom positions
            Vector3[] pp = modelFrames[oriModel];
            int idaa = 0;
            foreach (UnityMolAtom a in currentModel.allAtoms) {
                a.position = pp[idaa];
                idaa++;
            }
        }
        else {
            for (int i = 0; i < models.Count; i++) {
                UnityMolModel m = models[i];

                UnityMolModel newModel = m.Clone();

                UnityMolStructure newS = new UnityMolStructure(newModel, name + "_" + newModel.name);
                newS.models[0].structure = newS;

                newS.trajectoryLoaded = false;
                newS.ssInfoFromFile = ssInfoFromFile;

                result.Add(newS);

                UnityMolSelection sel = newS.ToSelection();

                Reader.CreateUnityObjects(newS.ToSelectionName(), sel);
                UnityMolMain.getStructureManager().AddStructure(newS);
                UnityMolMain.getSelectionManager().Add(sel);
            }
        }
        return result;
    }

    /// <summary>
    /// Split different chains into several UnityMolStructure
    /// </summary>
    public List<UnityMolStructure> splitChainsInStructures() {
        List<UnityMolStructure> result = new List<UnityMolStructure>();

        foreach (UnityMolChain c in currentModel.chains.Values) {

            UnityMolChain newChain = c.Clone();//Create a clone of the chain by cloning all its residues and atoms
            List<UnityMolAtom> newAllAtoms = newChain.allAtoms;

            //Map all atom ids from the original model to the ids in the new model
            Dictionary<int, int> oldIdToNewId = new Dictionary<int, int>(newAllAtoms.Count);
            int i = 0;
            foreach (UnityMolAtom a in newAllAtoms) {
                oldIdToNewId[a.idInAllAtoms] = i;
                i++;
            }

            UnityMolModel newM = new UnityMolModel(new List<UnityMolChain>() {newChain}, currentModel.name);
            newM.allAtoms = newAllAtoms;
            newM.fillIdAtoms();//Update each atom index in the model array
            newM.bonds = c.ToSelection().bonds;//Copy the part of the UnityMolBonds
            newM.bonds.convertIds(oldIdToNewId, newM);//Update the bonds with new ids


            UnityMolStructure newS = new UnityMolStructure(newM, name + "_" + newChain.name);
            newS.models[0].structure = newS;


            newS.ssInfoFromFile = ssInfoFromFile;
            newS.modelsAsTraj = modelsAsTraj;
            newS.ignoreDocking = ignoreDocking;
            newS.readHET = readHET;

            result.Add(newS);
            UnityMolSelection sel = newS.ToSelection();

            Reader.CreateUnityObjects(newS.ToSelectionName(), sel);
            UnityMolMain.getStructureManager().AddStructure(newS);
            UnityMolMain.getSelectionManager().Add(sel);
        }
        return result;
    }


    /// <summary>
    /// Find the corresponding atom from a model to a different model
    /// Returns null if not found
    /// </summary>
    public UnityMolAtom findAtomInModel(UnityMolModel newModel, UnityMolAtom prevAtom, bool strict = false) {

        if (!strict) {
            if (prevAtom.idInAllAtoms >= 0 && prevAtom.idInAllAtoms < newModel.allAtoms.Count) {
                return newModel.allAtoms[prevAtom.idInAllAtoms];
            }

            //Could not find the atom
            return null;
        }

        UnityMolResidue prevres = prevAtom.residue;
        UnityMolChain prevchain = prevres.chain;

        if (newModel.chains.ContainsKey(prevchain.name)) {
            //Look for the same residue
            int h = prevres.id;
            int prevnum = prevres.resnum;
            if (prevnum >= 0 && prevnum < newModel.chains[prevchain.name].residues.Count &&
                    newModel.chains[prevchain.name].residues[prevnum].id == h) {
                UnityMolResidue r = newModel.chains[prevchain.name].residues[prevnum];
                if (r.atoms.ContainsKey(prevAtom.name)) {
                    return r.atoms[prevAtom.name];
                }
            }

        }

        //Could not find the atom
        return null;
    }

    /// <summary>
    /// Outputs a UnityMolSelection of all the atoms of the structure, including the atoms of the different models
    /// </summary>
    public UnityMolSelection ToSelectionAll() {
        List<UnityMolAtom> selectedAtoms = models[0].allAtoms;
        for (int i = 1; i < models.Count; i++) {
            selectedAtoms.AddRange(models[i].allAtoms);
        }
        return new UnityMolSelection(selectedAtoms, "AllModels_" + name);
    }
    /// <summary>
    /// Outputs a UnityMolSelection of all the atoms of the current model of structure
    /// </summary>
    public UnityMolSelection ToSelection() {//Current model only
        List<UnityMolAtom> selectedAtoms = currentModel.allAtoms;
        string selString = name;
        UnityMolSelection sel = new UnityMolSelection(selectedAtoms, currentModel.bonds, ToSelectionName(), selString);
        sel.structures = new List<UnityMolStructure>();
        sel.structures.Add(this);
        sel.isAlterable = false;
        return sel;
    }

    public string ToSelectionName() {
        return "all_" + name;
    }

    public string formatName(int length) {
        string nameS = name;

        if (nameS.Length > length) {
            nameS = nameS.Substring(0, Mathf.Min(length - 3, nameS.Length));
            nameS += "...";
        }
        return nameS;
    }

    /// <summary>
    /// Add an atom to the structure.
    /// Check if the model and the chain exists in the structure, check if the atom is not already existing
    /// </summary>
    public void AddAtom(UnityMolAtom toAdd, string modelName, string chainName) {
        UnityMolModel m = null;

        foreach (UnityMolModel mdl in models) {
            if (modelName == mdl.name) {
                m = mdl;
                break;
            }
        }
        if (m == null) { //Try to find the model based on its id
            try {
                int modelId = int.Parse(modelName);
                if (modelId >= 0 && modelId < models.Count) {
                    m = models[modelId];
                }
            }
            catch { //Int parse failed
            }
        }

        if (m == null) {
            throw new System.Exception("Adding Atom Error, model not found (" + modelName + ")");
        }

        if (!m.chains.ContainsKey(chainName)) {
            throw new System.Exception("Adding Atom Error, chain not found");
        }
        UnityMolChain c = m.chains[chainName];

        UnityMolResidue r = c.getResidueWithId(toAdd.residue.id);
        if (r == null) {
            throw new System.Exception("Adding Atom Error, residue not found " + toAdd);
        }

        if (r.atoms.ContainsKey(toAdd.name)) {
            throw new System.Exception("Adding Atom Already Existing Error (" + toAdd.name + ")");
        }
        toAdd.SetResidue(r);

        r.atoms[toAdd.name] = toAdd;
        toAdd.idInAllAtoms = m.allAtoms.Count;
        m.allAtoms.Add(toAdd);

        //Call that after adding multiple atoms
        // m.ComputeCentroid();
        // updateBoundingBox();


        UnityMolMain.getCustomRaycast().needsFullUpdate = true;
        UnityMolMain.getPrecompRepManager().Clear(name);

        spatialSearch.Recreate(currentModel.allAtoms);
    }

    /// <summary>
    /// Remove several atoms from the structure, all models are affected
    /// </summary>
    public void RemoveAtoms(List<UnityMolAtom> toRm) {

        foreach (UnityMolModel m in models) {
            HashSet<int> idToRm = new HashSet<int>();
            for (int ida = 0; ida < toRm.Count; ida++) {

                if (toRm[ida].residue.chain.model.structure.name != name) {
                    //Not the same structure
                    continue;
                }
                string chainName = toRm[ida].residue.chain.name;

                if (!m.chains.ContainsKey(chainName)) {
                    throw new System.Exception("Removing Atom Error, chain not found");
                }
                UnityMolChain c = m.chains[chainName];

                UnityMolResidue r = c.getResidueWithId(toRm[ida].residue.id);
                if (r == null) {
                    throw new System.Exception("Removing Atom Error, residue not found");
                }

                if (!r.atoms.ContainsKey(toRm[ida].name)) {
                    throw new System.Exception("Removing Atom Not Existing Error (" + toRm[ida].name + ")");
                }

                r.atoms.Remove(toRm[ida].name);
                toRm[ida].residue = null;

                if (r.Count == 0) //Residue empty => remove it
                    c.residues.Remove(r);
                if (c.Count == 0) //Chain empty => remove it
                    m.chains.Remove(c.name);

                //Remove from allAtoms list
                int id = toRm[ida].idInAllAtoms;
                m.bonds.Remove(id, m);
                idToRm.Add(id);

            }
            List<UnityMolAtom> newallAtoms = new List<UnityMolAtom>(m.Count);
            Dictionary<int, int> oldIdToNew = new Dictionary<int, int>(m.Count);
            for (int i = 0; i < m.allAtoms.Count; i++) {
                if (!idToRm.Contains(i)) {
                    oldIdToNew[m.allAtoms[i].idInAllAtoms] = newallAtoms.Count;
                    newallAtoms.Add(m.allAtoms[i]);
                }
            }

            m.allAtoms = newallAtoms;
            m.fillIdAtoms();
            m.bonds.convertIds(oldIdToNew, m);
            m.ComputeCentroid();
        }
        updateBoundingBox();

        UnityMolMain.getCustomRaycast().needsFullUpdate = true;
        UnityMolMain.getPrecompRepManager().Clear(name);

        spatialSearch.Recreate(currentModel.allAtoms);

        // UnityMolMain.getRepresentationManager().UpdateActiveRepresentations();
        updateRepresentations(trajectory: false);
    }


    /// <summary>
    /// Merge a structure into the current one.
    /// The chain name is used to avoid conflicts between existing residues/atoms of the current structure and the merged one
    /// Delete the structure to be merged and all its representation
    /// </summary>
    public void MergeStructure(UnityMolStructure tobeMerged, string newChainName) {

        UnityMolChain[] chains = tobeMerged.currentModel.chains.Values.ToArray();
        foreach (UnityMolChain c in chains) {
            string chainName = newChainName;
            while (currentModel.chains.ContainsKey(chainName)) { //Chain already exists
                chainName = findNewChainName(chainName);
            }
            //Modify the chain to be integrated in the current UnityMolStructure
            c.name = chainName;
            c.model = currentModel;

            currentModel.chains[chainName] = c;

            foreach (UnityMolResidue r in c.residues) {
                //Modify idInAllAtoms
                foreach (UnityMolAtom a in r.atoms.Values) {
                    a.idInAllAtoms = currentModel.allAtoms.Count;
                    currentModel.allAtoms.Add(a);
                }
            }
        }

        currentModel.ComputeCentroid();
        currentModel.fillIdAtoms();

        //-----Fill bonds-----
        foreach (int idA in tobeMerged.currentModel.bonds.bonds.Keys) {
            UnityMolAtom a = tobeMerged.currentModel.allAtoms[idA];
            int aserial = a.serial;
            int anewId = getIdFromSerial(aserial, currentModel);
            foreach (int idB in tobeMerged.currentModel.bonds.bonds[idA]) {
                if (idB != -1) {
                    UnityMolAtom b = tobeMerged.currentModel.allAtoms[idB];
                    int bserial = b.serial;
                    int bnewId = getIdFromSerial(bserial, currentModel);
                    currentModel.bonds.Add(anewId, bnewId, currentModel);
                }
            }
        }

        //Need to create colliders for newly added atoms
        Reader.CreateUnityObjects(ToSelectionName(), new UnityMolSelection(tobeMerged.currentModel.allAtoms, newBonds: null, ToSelectionName(), name));
        updateBoundingBox();

        UnityMolMain.getStructureManager().Delete(tobeMerged);

        UnityMolMain.getCustomRaycast().needsFullUpdate = true;
        UnityMolMain.getPrecompRepManager().Clear(name);

        spatialSearch.Recreate(currentModel.allAtoms);
    }

    private int getIdFromSerial(int serial, UnityMolModel model) {
        for (int i = 0; i < model.allAtoms.Count; i++) {
            if (model.allAtoms[i].serial == serial)
                return model.allAtoms[i].idInAllAtoms;
        }
        return -1;
    }

    private string findNewChainName(string name) {
        string result = name;
        if (name.Length == 1) {
            char tmp = name[0];
            tmp++;
            result = tmp.ToString();
        }
        else {
            int toAdd = 2;
            result = name + toAdd.ToString();
            while (currentModel.chains.ContainsKey(result)) {
                toAdd++;
                result = name + toAdd.ToString();
            }
        }

        return result;
    }

    public Vector3[] getWorldPositions() {

        Vector3[] wPos = new Vector3[Count];

        GameObject loadedMolGO = UnityMolMain.getRepresentationParent();
        Transform repParent = loadedMolGO.transform;

        int i = 0;
        foreach (UnityMolAtom a in currentModel.allAtoms) {
            Vector3 localPos = repParent.InverseTransformPoint(a.curWorldPosition);
            wPos[i] = localPos;
            i++;
        }

        return wPos;
    }
    public void getWorldPositions(ref Vector3[] toFill, int idStart = 0) {
        if (toFill.Length < Count) {
            Debug.LogWarning("Reallocating memory. This shouldn't happen if toFill has the right size");
            toFill = new Vector3[Count];
        }
        GameObject loadedMolGO = UnityMolMain.getRepresentationParent();
        Transform repParent = loadedMolGO.transform;
        int i = 0;
        foreach (UnityMolAtom a in currentModel.allAtoms) {
            Vector3 localPos = repParent.InverseTransformPoint(a.curWorldPosition);

            toFill[idStart + i] = localPos;
            i++;
        }
    }

    public List<UnityMolResidue.secondaryStructureType> ssInfoToList() {
        List<UnityMolResidue.secondaryStructureType> ssInfo = new List<UnityMolResidue.secondaryStructureType>();
        foreach (UnityMolChain c in currentModel.chains.Values) {
            foreach (UnityMolResidue r in c.residues) {
                ssInfo.Add(r.secondaryStructure);
            }
        }
        return ssInfo;
    }

    public void createBoundingBoxLines() {
        if (string.IsNullOrEmpty(name))
            return;
        if (boundBoxLines != null) {
            destroyBoundingBox();
        }
        GameObject gospar = getStructureParent();

        if (gospar == null) {
            return;
        }
        Transform spar = gospar.transform;
        GameObject bboxPar = new GameObject("BoundingBox");
        bboxPar.transform.SetParent(spar, false);
        bboxPar.transform.localRotation = Quaternion.identity;
        bboxPar.transform.localPosition = Vector3.zero;
        bboxPar.transform.localScale = Vector3.one;

        boundBoxLines = new GameObject[12];
        for (int i = 0; i < 12; i++) {
            boundBoxLines[i] = new GameObject("BBox_" + name);
        }
        Vector3 minP = Vector3.zero;
        Vector3 maxP = Vector3.zero;
        getMinMax(ref minP, ref maxP);


        Vector3 p1 = minP;
        Vector3 p2 = new Vector3(maxP.x, minP.y, minP.z);
        Vector3 p3 = new Vector3(minP.x, maxP.y, minP.z);
        Vector3 p4 = new Vector3(minP.x, minP.y, maxP.z);
        Vector3 p5 = new Vector3(maxP.x, maxP.y, minP.z);
        Vector3 p6 = new Vector3(maxP.x, minP.y, maxP.z);
        Vector3 p7 = new Vector3(minP.x, maxP.y, maxP.z);
        Vector3 p8 = maxP;

        createLine(bboxPar.transform, boundBoxLines[0], p1, p2, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[1], p1, p3, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[2], p1, p4, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[3], p2, p5, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[4], p3, p5, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[5], p4, p6, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[6], p2, p6, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[7], p4, p7, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[8], p3, p7, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[9], p8, p7, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[10], p8, p6, Color.gray);
        createLine(bboxPar.transform, boundBoxLines[11], p8, p5, Color.gray);

    }

    void createLine(Transform spar, GameObject go, Vector3 start, Vector3 end, Color col) {
        go.transform.SetParent(spar);
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localPosition = Vector3.zero;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;

        if (lineMat == null) {
            Shader lineShader = Shader.Find("Particles/Alpha Blended");
            if (lineShader == null)
                lineShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            lineMat = new Material(lineShader);
        }

        lr.sharedMaterial = lineMat;
        lr.startColor = lr.endColor = col;
        lr.startWidth = lr.endWidth = bboxLineSize;
        lr.alignment = LineAlignment.View;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    GameObject getStructureParent() {

        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        GameObject structureParent = null;

        try {
            structureParent = sm.GetStructureGameObject(name);
        }
        catch {
            string sName = ToSelectionName();
            structureParent = UnityMolMain.getRepStructureParent(sName);
        }

        return structureParent;
    }


    void getMinMax(ref Vector3 minP, ref Vector3 maxP) {
        if (Count == 0) {
            minP = Vector3.zero;
            maxP = Vector3.zero;
            return;
        }
        minP = currentModel.allAtoms[0].position - (Vector3.one * currentModel.allAtoms[0].radius);
        maxP = currentModel.allAtoms[0].position + (Vector3.one * currentModel.allAtoms[0].radius);

        for (int i = 1; i < Count; i++) {
            Vector3 pmin = currentModel.allAtoms[i].position - (Vector3.one * currentModel.allAtoms[i].radius);
            Vector3 pmax = currentModel.allAtoms[i].position + (Vector3.one * currentModel.allAtoms[i].radius);
            minP = Vector3.Min(minP, pmin);
            maxP = Vector3.Max(maxP, pmax);
        }
    }
    ///Show bounding box lines only for the current model
    public void showBoundingBox() {
        if (boundBoxLines == null) {
            createBoundingBoxLines();
            return;
        }
        foreach (GameObject go in boundBoxLines) {
            go.GetComponent<LineRenderer>().enabled = true;
        }
    }
    public void hideBoundingBox() {
        if (boundBoxLines == null) {
            createBoundingBoxLines();
        }
        if (boundBoxLines == null)
            return;
        foreach (GameObject go in boundBoxLines) {
            go.GetComponent<LineRenderer>().enabled = false;
        }
    }

    private void setLinePositions(GameObject go, Vector3 start, Vector3 end) {
        LineRenderer lr = go.GetComponent<LineRenderer>();
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    public void updateBoundingBox() {

        if (boundBoxLines == null) {
            createBoundingBoxLines();
            hideBoundingBox();
            return;
        }
        if (boundBoxLines == null)
            return;

        Vector3 minP = currentModel.minimumPositions;
        Vector3 maxP = currentModel.maximumPositions;

        Vector3 p1 = minP;
        Vector3 p2 = new Vector3(maxP.x, minP.y, minP.z);
        Vector3 p3 = new Vector3(minP.x, maxP.y, minP.z);
        Vector3 p4 = new Vector3(minP.x, minP.y, maxP.z);
        Vector3 p5 = new Vector3(maxP.x, maxP.y, minP.z);
        Vector3 p6 = new Vector3(maxP.x, minP.y, maxP.z);
        Vector3 p7 = new Vector3(minP.x, maxP.y, maxP.z);
        Vector3 p8 = maxP;

        setLinePositions(boundBoxLines[0], p1, p2);
        setLinePositions(boundBoxLines[1], p1, p3);
        setLinePositions(boundBoxLines[2], p1, p4);
        setLinePositions(boundBoxLines[3], p2, p5);
        setLinePositions(boundBoxLines[4], p3, p5);
        setLinePositions(boundBoxLines[5], p4, p6);
        setLinePositions(boundBoxLines[6], p2, p6);
        setLinePositions(boundBoxLines[7], p4, p7);
        setLinePositions(boundBoxLines[8], p3, p7);
        setLinePositions(boundBoxLines[9], p8, p7);
        setLinePositions(boundBoxLines[10], p8, p6);
        setLinePositions(boundBoxLines[11], p8, p5);

    }

    public void setBoundingBoxLineSize(float newSize) {
        if (boundBoxLines != null) {
            bboxLineSize = newSize;
            foreach (GameObject go in boundBoxLines) {
                LineRenderer lr = go.GetComponent<LineRenderer>();
                lr.startWidth = lr.endWidth = bboxLineSize;
            }
        }
    }
    public float getBoundingBoxLineSize() {
        return bboxLineSize;
    }

    void destroyBoundingBox() {
        if (boundBoxLines != null) {
#if UNITY_EDITOR
            GameObject.DestroyImmediate(boundBoxLines[0]);
            GameObject.DestroyImmediate(boundBoxLines[1]);
            GameObject.DestroyImmediate(boundBoxLines[2]);
            GameObject.DestroyImmediate(boundBoxLines[3]);
            GameObject.DestroyImmediate(boundBoxLines[4]);
            GameObject.DestroyImmediate(boundBoxLines[5]);
            GameObject.DestroyImmediate(boundBoxLines[6]);
            GameObject.DestroyImmediate(boundBoxLines[7]);
            GameObject.DestroyImmediate(boundBoxLines[8]);
            GameObject.DestroyImmediate(boundBoxLines[9]);
            GameObject.DestroyImmediate(boundBoxLines[10]);
            GameObject.DestroyImmediate(boundBoxLines[11]);
            if (lineMat != null) {
                GameObject.DestroyImmediate(lineMat);
            }
#else
            GameObject.Destroy(boundBoxLines[0]);
            GameObject.Destroy(boundBoxLines[1]);
            GameObject.Destroy(boundBoxLines[2]);
            GameObject.Destroy(boundBoxLines[3]);
            GameObject.Destroy(boundBoxLines[4]);
            GameObject.Destroy(boundBoxLines[5]);
            GameObject.Destroy(boundBoxLines[6]);
            GameObject.Destroy(boundBoxLines[7]);
            GameObject.Destroy(boundBoxLines[8]);
            GameObject.Destroy(boundBoxLines[9]);
            GameObject.Destroy(boundBoxLines[10]);
            GameObject.Destroy(boundBoxLines[11]);
            if (lineMat != null) {
                GameObject.Destroy(lineMat);
            }
#endif
        }

        boundBoxLines = null;
    }

    public bool connectIMD(string adress, int port) {
        GameObject sgo = UnityMolMain.getStructureManager().structureToGameObject[name];
        mddriverM = sgo.AddComponent<MDDriverManager>();
        mddriverM.structure = this;

        bool res = mddriverM.connect(adress, port);
        if (!res) {
            disconnectIMD();
            return false;
        }

        UnityMolStructureManager.callIMDConnectionEvent(this);

        return true;
    }
    public void disconnectIMD() {

        if (mddriverM == null)
            return;
        mddriverM.disconnect();
        GameObject.DestroyImmediate(mddriverM);

        mddriverM = null;

        for (int i = 0; i < currentModel.allAtoms.Count; i++) {
            currentModel.allAtoms[i].position = currentModel.allAtoms[i].oriPosition;
        }
        currentModel.ComputeCentroid();

        spatialSearch.UpdatePositions(currentModel.allAtoms);

        updateRepresentations(trajectory: false);

        if (trajAtomPositions != null) {
            trajAtomPositions = null;
        }
        UnityMolStructureManager.callIMDDisconnectionEvent(this);
    }

    public void OnDestroy() {
        destroyBoundingBox();

        if (modelFrames != null) {
            modelFrames.Clear();
        }
        disconnectIMD();
        unloadTrajectoryXDR(true);
        if (surfThread != null) {
            surfThread.Clear();
        }
        UnityMolMain.getPrecompRepManager().Clear(name);
        spatialSearch.Clean();
    }

    public SerializedStructure Serialize() {
        SerializedStructure sstru = new SerializedStructure();

        sstru.name = name;
        sstru.path = path;
        sstru.pdbID = pdbID;
        sstru.dxPath = dxPath;
        sstru.fromPath = (path != null);
        sstru.fetchmmCif = fetchedmmCIF;
        sstru.bioAssembly = bioAssembly;
        sstru.modelsAsTraj = modelsAsTraj;
        sstru.trajLoaded = (xdr != null);
        sstru.ssFromFile = ssInfoFromFile;
        sstru.ignoreDocking = ignoreDocking;
        sstru.readHET = readHET;
        sstru.currentModel = currentModelId;
        sstru.currentFrameId = currentFrameId;
        if (xdr != null) {
            sstru.trajPath = xdr.path;
            sstru.currentFrameTraj = xdr.currentFrame;
        }
        sstru.groupId = groupID;
        sstru.structureType = (int)structureType;

        return sstru;
    }
}
}
