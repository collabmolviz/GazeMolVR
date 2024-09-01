```python
# Show/Hide UnityMol console

showHideConsole(bool show) 
```
```python
# Load a local molecular file (pdb/mmcif/gro/mol2/sdf/xyz formats)
# forceStructureType (-1 = auto-detect / 0 = standard / 1 = CG / 2 = OPEP / 3 = HIRERNA)

UnityMolStructure load(string filePath, bool readHetm = True, bool forceDSSP = False, bool showDefaultRep = True, bool center = True, bool modelsAsTraj = True, int forceStructureType = -1) 
```
```python
# Load a molecular file (pdb/mmcif/gro/mol2/sdf/xyz formats) from a string
# forceStructureType (-1 = auto-detect / 0 = standard / 1 = CG / 2 = OPEP / 3 = HIRERNA)

UnityMolStructure loadFromString(string fileName, string fileContent, bool readHetm = True, bool forceDSSP = False, bool showDefaultRep = True, bool center = True, bool modelsAsTraj = True, int forceStructureType = -1) 
```
```python
# Fetch a remote molecular file (pdb or mmcif zipped)
# forceStructureType (-1 = auto-detect / 0 = standard / 1 = CG / 2 = OPEP / 3 = HIRERNA)

UnityMolStructure fetch(string PDBId, bool usemmCIF = True, bool readHetm = True, bool forceDSSP = False, bool showDefaultRep = True, bool center = True, bool modelsAsTraj = True, int forceStructureType = -1, bool bioAssembly = False) 
```
```python
# WIP Load martini itp file to prase elastic network and secondary structure

loadMartiniITP(string structureName, string filePath) 
```
```python
# Show bounding box lines around the structure

showBoundingBox(string structureName) 
```
```python
# Hide bounding box lines around the structure

hideBoundingBox(string structureName) 
```
```python
# Set bounding box lines size

setBoundingBoxLineSize(string structureName, float size = 0.005f) 
```
```python
# Load a XML file containing covalent and noncovalent bonds
# modelId = -1 means currentModel
# Possible bond types are: 'covalent' or 'db_geom', 'hbond' or 'h-bond' or 'hbond_weak', 'halogen', 'ionic', 'aromatic', 'hydrophobic', 'carbonyl'

loadBondsXML(string structureName, string filePath, int modelId = -1) 
```
```python
# Override the current bonds of the model modelId and saves the previous one in model.savedBonds
# modelId = -1 means currentModel

overrideBondsWithXML(string structureName, int modelId = -1) 
```
```python
# Load bounding information from a PSF file
# modelId = -1 means currentModel

loadPSFTopology(string structureName, string psfPath, int modelId = -1) 
```
```python
# Load bounding information from a TOP file
# modelId = -1 means currentModel
# specialBondString when not empty is used to create a selection containing only these special bonds, shown as hbondtube

loadTOPTopology(string structureName, string topPath, int modelId = -1, string specialBondString = "restrain") 
```
```python
# Restore bonds using the model.savedBonds
# modelId = -1 means currentModel

restoreBonds(string structureName, int modelId = -1) 
```
```python
# Removes the covBondOrders bonds loaded by loadBondsXML from the model

unloadCustomBonds(string structureName, int modelId) 
```
```python
# Delete all the loaded molecules

reset() 
```
```python
# Switch between parsed secondary structure information and DSSP computation

switchSSAssignmentMethod(string structureName, bool forceDSSP = False) 
```
```python
# Show/Hide hydrogens in representations of the provided selection
# This only works for lines, hyperball and sphere representations

showHideHydrogensInSelection(string selName, bool? shouldShow = null) 
```
```python
# Show/Hide side chains in representations of the current selection
# This only works for lines, hyperball and sphere representations only

showHideSideChainsInSelection(string selName) 
```
```python
# Show/Hide backbone in representations of the current selection
# This only works for lines, hyperball and sphere representations only

showHideBackboneInSelection(string selName) 
```
```python
# Set the current model of the structure
# This function is used by ModelPlayers.cs to read the models of a structure like a trajectory

setModel(string structureName, int modelId) 
```
```python
# Load a trajectory for a loaded structure
# It creates a XDRFileReader in the corresponding UnityMolStructure and a TrajectoryPlayer

loadTraj(string structureName, string filePath) 
```
```python
# Unload a trajectory for a specific structure

unloadTraj(string structureName) 
```
```python
# Create a special selection containing frames from the trajectory

string pickTrajectoryFrames(string structureName, string selectionQuery = "all", int frameStart = 0, int frameEnd = 1, int step = 1) 
```
```python
# Set the current trajectory frame of the structure named structureName to frame
# frame has to be between 0 and numberFrames

setTrajFrame(string structureName, int frame) 
```
```python
# Load a density map for a specific structure
# This function creates a DXReader instance in the UnityMolStructure

loadDXmap(string structureName, string filePath) 
```
```python
# Show lines around dx map

showDXLines(string structureName) 
```
```python
# Hide lines around dx map

hideDXLines(string structureName) 
```
```python
# Unload the density map for the structure

unloadDXmap(string structureName) 
```
```python
# Read a json file and display fieldLines for the specified structure

readJSONFieldlines(string structureName, string filePath) 
```
```python
# Remove the json file for fieldlines stored in the currentModel of the specified structure

unloadJSONFieldlines(string structureName) 
```
```python
# Change fieldline computation gradient threshold

setFieldlineGradientThreshold(string selName, float val) 
```
```python
# Utility function to be able to get the group of the structure
# This group is used to be able to move all the loaded molecules in the same group
# Groups can be between 0 and 9 included

int getStructureGroup(string structureName) 
```
```python
# Utility function to be able to get the structures of the group
# This group is used to be able to move all the loaded molecules in the same group
# Groups can be between 0 and 9 included

HashSet<UnityMolStructure> getStructuresOfGroup(int group) 
```
```python
# Utility function to be set the group of a structure
# This group is used to be able to move all the loaded molecules in the same group
# Groups can be between 0 and 9 included

setStructureGroup(string structureName, int newGroup) 
```
```python
# Delete a molecule and all its UnityMolSelection and UnityMolRepresentation

delete(string structureName) 
```
```python
# Show as 'type' all loaded molecules
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

show(string type) 
```
```python
# Show all loaded molecules only as the 'type' representation
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

showAs(string type) 
```
```python
# Restore all representations of a structure to the default representations

resetRep(string structureName) 
```
```python
# Create selections and default representations: all in cartoon, not protein in hyperballs
# Also create a selection containing "not protein and not water and not ligand and not ions"

bool defaultRep(string selName) 
```
```python
# Create default representations (cartoon for protein + HB for not protein atoms)

showDefault(string selName) 
```
```python
# Unhide all representations already created for a specified structure

showStructureAllRepresentations(string structureName) 
```
```python
# Show the selection as 'type'
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"
# If the representation is already there, update it if the selection content changed and show it
# Surface example: showSelection("all_1kx2", "s", True, True, True, SurfMethod.MSMS) # arguments are cutByChain, AO, cutSurface, computeSurfaceMethod
# Iso-surface example: showSelection("all_1kx2", "dxiso", last().dxr, 0.0f)

showSelection(string selName, string type, params object[] args) 
```
```python
# Show all representations of the selection named 'selName'

showSelection(string selName) 
```
```python
# Hide every representations of the specified selection

hideSelection(string selName) 
```
```python
# Hide every representation of type 'type' of the specified selection
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

hideSelection(string selName, string type) 
```
```python
# Delete every representations of type 'type' of the specified selection
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

deleteRepresentationInSelection(string selName, string type) 
```
```python
# Delete every representations of the specified selection

deleteRepresentationsInSelection(string selName) 
```
```python
# Hide every representations of the specified structure

hideStructureAllRepresentations(string structureName) 
```
```python
# Delete all the selection of the given structure

deleteAllSelectionsStructure(string structureName) 
```
```python
# Utility function to test if a representation is shown for a specified structure

bool areRepresentationsOn(string structureName) 
```
```python
# Utility function to test if a representation of type 'type' is shown for a specified selection
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

bool areRepresentationsOn(string selName, string type) 
```
```python
# Hide all representations of type 'type'
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

hide(string type) 
```
```python
# Switch between the 2 types of surface computation methods: EDTSurf and MSMS

switchSurfaceComputeMethod(string selName) 
```
```python
# Switch between cut surface mode and no-cut surface mode

switchCutSurface(string selName, bool isCut) 
```
```python
# Switch all surface representation in selection to a solid surface material

setSolidSurface(string selName) 
```
```python
# Switch all surface representation in selection to a wireframe surface material when available

setWireframeSurface(string selName) 
```
```python
# Switch all surface representation in selection to a transparent surface material

setTransparentSurface(string selName, float? alpha = null) 
```
```python
# Switch cartoon material from transparent to normal/solid

setSolidCartoon(string selName) 
```
```python
# Switch cartoon material from normal/solid to transparent

setTransparentCartoon(string selName, float alpha = 0.3f) 
```
```python
# Recompute cartoon representation with new tube size

setTubeSizeCartoon(string selName, float newVal) 
```
```python
# Draw cartoon representation as tube

drawCartoonAsTube(string selName, bool drawAsTube = True) 
```
```python
# Draw cartoon representation as tube with Bfactor as tube size

drawCartoonAsBfactorTube(string selName, bool drawAsBTube = True) 
```
```python
# Recompute the DX surface with a new iso value

updateDXIso(string selName, float newVal) 
```
```python
# Change hyperball representation parameters in the specified selection to a preset
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

setSmoothness(string selName, string type, float val) 
```
```python
# Change hyperball representation parameters in the specified selection to a preset
# Metaphores can be "Smooth", "Balls&Sticks", "VdW", "Licorice"
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

setMetal(string selName, string type, float val) 
```
```python
# Change surface wireframe size

setSurfaceWireframe(string selName, string type, float val) 
```
```python
# Only show a part of the representation inside a sphere, only works with surface types for now

enableLimitedView(string selName, string type) 
```
```python
# Disable the limited view

disableLimitedView(string selName, string type) 
```
```python
# Get if the limited view is activated

bool getLimitedView(string selName, string type) 
```
```python
# Set the center of the limited view in local space

setLimitedViewCenter(string selName, string type, Vector3 center) 
```
```python
# Retrieve the current center of the limited view

Vector3 getLimitedViewCenter(string selName, string type) 
```
```python
# Set the radius (in Angstrom) of the limited view

setLimitedViewRadius(string selName, string type, float radius) 
```
```python
# Change hyperball representation parameters in all selections that contains a hb representation
# Metaphores can be "Smooth", "Balls&Sticks", "VdW", "Licorice"

setHyperBallMetaphore(string metaphore, bool forceAOOff = True, bool lerp = False, float duration = 0.5f) 
```
```python
# Change hyperball representation parameters in the specified selection to a preset
# Metaphores can be "Smooth", "Balls&Sticks", "VdW", "Licorice"

setHyperBallMetaphore(string selName, string metaphore, bool forceAOOff = True, bool lerp = False, float duration = 0.5f) 
```
```python
# Set shininess for the hyperball representations of the specified selection

setHyperBallShininess(string selName, float shin) 
```
```python
# Set the shrink factor for the hyperball representations of the specified selection

setHyperballShrink(string selName, float shrink) 
```
```python
# Change all hyperball representation in the selection with a new texture mapped
# idTex of the texture is the index in UnityMolMain.atomColors.textures

setHyperballTexture(string selName, int idTex) 
```
```python
# Change all bond order representation in the selection with a new texture mapped
# idTex of the texture is the index in UnityMolMain.atomColors.textures

setBondOrderTexture(string selName, int idTex) 
```
```python
# Remove AO from hyperballs

clearHyperballAO(string selName) 
```
```python
# Compute object space AO for surface

computeSurfaceAO(string selName, string type) 
```
```python
# Remove AO for surface

clearSurfaceAO(string selName, string type) 
```
```python
# Get statut of AO surface

bool isSurfaceAOOn(string selName, string type) 
```
```python
# Set ambient light intensity

setAmbientLightIntensity(float i) 
```
```python
# Set light intensity of all directional lights found in the scene

setDirLightIntensity(float v) 
```
```python
# Set light shadow strength of all directional lights found in the scene
# 0 is no shadow at all, 1 is full black shadow

setDirLightShadow(float v) 
```
```python
# Set light direction in X of all directional lights found in the scene

setDirLightDirection(Vector3 eulers) 
```
```python
# Set light color of all directional lights found in the scene

setDirLightColor(Color c) 
```
```python
# Set the color of the cartoon representation of the specified selection based on the nature of secondary structure assigned
# ssType can be "helix", "sheet" or "coil"

setCartoonColorSS(string selName, string ssType, Color col) 
```
```python
# Change the size of the representation of type 'type' in the selection
# Mainly used for hyperball representation
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

setRepSize(string selName, string type, float size) 
```
```python
# Change the color of all representation of type 'type' in selection
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

colorSelection(string selName, string type, Color col) 
```
```python
# Change the color of all representation of type 'type' in selection
# colorS can be "black", "white", "yellow", "green", "red", "blue", "pink", "gray"
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

colorSelection(string selName, string type, string colorS) 
```
```python
# Change the color of all representation of type 'type' in selection
# colors is a list of colors the length of the selection named selName
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

colorSelection(string selName, string type, List<Color> colors) 
```
```python
# Reset the color of all representation of type 'type' in selection to the default value
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

resetColorSelection(string selName, string type) 
```
```python
# In the representation of type repType, color all atoms of type atomType in the selection selName with

colorAtomType(string selName, string repType, string atomType, Color col) 
```
```python
# Use the color palette to color representations of type 'type' in the selection 'selName' by chain
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

colorByChain(string selName, string type) 
```
```python
# Use the color palette to color representations of type 'type' in the selection 'selName' by residue
# type can be "cartoon", "c", "surface", "s", "hb", "line", "l", "hbond"

colorByResidue(string selName, string type) 
```
```python
# Color representations of type 'type' in the selection 'selName' by atom

colorByAtom(string selName, string type) 
```
```python
# Color representations of type 'type' in the selection 'selName' by hydrophobicity

colorByHydrophobicity(string selName, string type) 
```
```python
# Color representations of type 'type' in the selection 'selName' by sequence (rainbow effect)

colorBySequence(string selName, string type) 
```
```python
# Use the dx map to color by charge around atoms
# Only works for surface for now
# If normalizeDensity is set to true, the density values will be normalized
# if it is set to true, the default -10|10 range is used

colorByCharge(string selName, bool normalizeDensity = False, float minDens = -10.0f, float maxDens = 10.0f) 
```
```python
# Color residues by "restype": negatively charge = red, positively charged = blue, nonpolar = light yellow,
# polar = green, cys = orange

colorByResidueType(string selName, string type) 
```
```python
# Color residues by "restype": negatively charge = red, positively charged = blue, neutral = white

colorByResidueCharge(string selName, string type) 
```
```python
# Color residues by Bfactor

colorByBfactor(string selName, string type, Color startColor, Color midColor, Color endColor) 
```
```python
# Color residues by Bfactor: low to high = blue to red

colorByBfactor(string selName, string type) 
```
```python
# Set size of the line representation

setLineSize(string selName, float val) 
```
```python
# Set size of the trace representation

setTraceSize(string selName, float val) 
```
```python
# Change sheherasade computation method

switchSheherasadeMethod(string selName) 
```
```python
# Set sheherasade texture

setSheherasadeTexture(string selName, int idTex) 
```
```python
# Offsets all representations to center the structure 'structureName'
# Instead of moving the camera, move the loaded molecules to center them on the center of the camera

centerOnStructure(string structureName, bool lerp = False, bool recordCommand = True) 
```
```python
# Get the current ManipulationManager, creates one if there is none

ManipulationManager getManipulationManager() 
```
```python
# Offsets all representations to center the selection 'selName'

centerOnSelection(string selName, bool lerp = False, float distance = -1.0f) 
```
```python
# CEAlign algorithm to align two proteins with "little to no sequence similarity", only uses Calpha atoms
# For more details: https://pymolwiki.org/index.php/Cealign

cealign(string selNameTarget, string selNameMobile) 
```
```python
# Create a UnityMolSelection based on MDAnalysis selection language (https://www.mdanalysis.org/docs/documentation_pages/selections.html)
# Returns a UnityMolSelection object, adding it to the selection manager if createSelection is true
# If a selection with the same name already exists and addToExisting is true, add atoms to the already existing selection
# Set forceCreate to true if the selection is empty but still need to generate the selection

UnityMolSelection select(string selMDA, string name = "selection", bool createSelection = True, bool addToExisting = False, bool silent = False, bool setAsCurrentSelection = True, bool forceCreate = False, bool allModels = False, bool addToSelectionKeyword = True) 
```
```python
# Add a keyword to the selection language

addSelectionKeyword(string keyword, string selName) 
```
```python
# Remove a keyword from the selection language

removeSelectionKeyword(string keyword, string selName) 
```
```python
# Set the selection as currentSelection in the UnityMolSelectionManager

setCurrentSelection(string selName) 
```
```python
# Look for an existing selection named 'name' and add atoms to it based on MDAnalysis selection language

addToSelection(string selMDA, string name = "selection", bool silent = False, bool allModels = False) 
```
```python
# Look for an existing selection named 'name' and remove atoms from it based on MDAnalysis selection language

removeFromSelection(string selMDA, string name = "selection", bool silent = False, bool allModels = False) 
```
```python
# Delete selection 'selName' and all its representations

deleteSelection(string selName) 
```
```python
# Duplicate selection 'selName' and without the representations

string duplicateSelection(string selName) 
```
```python
# Change the 'oldSelName' selection name into 'newSelName'

bool renameSelection(string oldSelName, string newSelName) 
```
```python
# Update the atoms of the selection based on a new MDAnalysis language selection
# The selection only applies to the structures of the selection

bool updateSelectionWithMDA(string selName, string selectionString, bool forceAlteration, bool silent = False, bool recordCommand = True, bool allModels = False) 
```
```python
# Directly clear the highlight manager, this does not unselect the current selection

cleanHighlight() 
```
```python
# Select atoms of all loaded molecules inside a sphere defined by a molecular space position and a radius in Anstrom

UnityMolSelection selectInSphere(Vector3 position, float radius) 
```
```python
# Select atoms of all loaded molecules inside a rectangle defined by a molecular space position and 3 axis

UnityMolSelection selectInRectangle(Vector3 lowerLeft, Vector3 xaxis, Vector3 yaxis, Vector3 zaxis) 
```
```python
# Update representations of the specified selection, called automatically after a selection content change

updateRepresentations(string selName) 
```
```python
# Clear the currentSelection in UnityMolSelectionManager

clearSelections() 
```
```python
# Utility function to test if a trajectory is playing for any loaded molecule

bool isATrajectoryPlaying() 
```
```python
# Set the updateContentWithTraj of the selection to enable/disable selection content update

setUpdateSelectionTraj(string selName, bool v) 
```
```python
# Show or hide representation shadows

setShadows(string selName, string type, bool enable) 
```
```python
# Utility function to change the material of highlighted selection

changeHighlightMaterial(Material newMat) 
```
```python
# Take a screenshot of the current viewpoint with a specific resolution

screenshot(string path, int resolutionWidth = 1280, int resolutionHeight = 720, bool transparentBG = False) 
```
```python
# Start to record a video with FFMPEG at a specific resolution and framerate

startVideo(string filePath, int resolutionWidth = 1280, int resolutionHeight = 720, int frameRate = 30) 
```
```python
# Stop recording

stopVideo() 
```
```python
# Pause recording

pauseVideo() 
```
```python
# Unpause recording

unpauseVideo() 
```
```python
# Play the opposite function of the lastly called APIPython function recorded in UnityMolMain.pythonUndoCommands

undo() 
```
```python
# Set the local position and rotation (euler angles) of the given structure

setStructurePositionRotation(string structureName, Vector3 pos, Vector3 rot) 
```
```python
# Get the current position and rotation of the given structure

getStructurePositionRotation(string structureName, ref Vector3 pos, ref Vector3 rot) 
```
```python
# Get the current position and rotation of the given structure

string getStructurePositionRotation(string structureName) 
```
```python
# Save the history of commands executed in a file

saveHistoryScript(string fullpath) 
```
```python
# Save the current positions of the loaded structures in a single PDB file

saveDockingState(string fullpath = null) 
```
```python

string loadedMolParentToString(bool addToHistory = False) 
```
```python
# Load a python script of commands (possibly the output of the saveHistoryScript function)

loadHistoryScript(string filePath) 
```
```python
# Load a python script of commands (possibly the output of the saveHistoryScript function)

loadScript(string filePath) 
```
```python
# Set the position, scale and rotation of the parent of all loaded molecules
# Linear interpolation between the current state of the camera to the specified values

setMolParentTransform(Vector3 pos, Vector3 scale, Vector3 rot, Vector3 centerOfRotation, bool lerp = True, float duration = 1.0f) 
```
```python

string getMolParentTransform() 
```
```python
# Change the scale of the parent of the representations of each molecules
# Try to not move the center of mass

changeGeneralScale_cog(float newVal) 
```
```python
# Change the scale of the parent of the representations of each molecules
# Keep relative positions of molecules, use the first loaded molecule center of gravity to compensate the translation due to scaling

changeGeneralScale(float newVal) 
```
```python
# Use Reduce method to add hydrogens

addHydrogensReduce(string structureName) 
```
```python
# Use HAAD method to add hydrogens

addHydrogensHaad(string structureName) 
```
```python
# Set the atoms of the selection named 'selName' to ligand

setAsLigand(string selName, bool isLig = True, bool updateAllSelections = True) 
```
```python
# Merge UnityMolStructure structureName2 in structureName using a different chain name to avoid conflict

mergeStructure(string structureName, string structureName2, string chainName = "Z") 
```
```python
# Save current atom positions of the selection to a PDB file
# World atom positions are transformed to be relative to the first structure in the selection

saveToPDB(string selName, string fullPath, bool writeSSinfo = False) 
```
```python
# Connect to a running simulation using the IMD protocol implemented in MDDriver
# The running simulation is binded to a UnityMolStructure

bool connectIMD(string structureName, string adress, int port) 
```
```python
# Disconnect from the IMD simulation for the specified structure

disconnectIMD(string structureName) 
```
```python
# Get current surface material

string getSurfaceType(string selName) 
```
```python
# Get current hyperball metaphore

string getHyperBallMetaphore(string selName) 
```
```python
# Set camera near plane, note this has an impact on shadow map quality

setCameraNearPlane(float newV) 
```
```python
# Set camera far plane, note this has an impact on shadow map quality

setCameraFarPlane(float newV) 
```
```python
# Enable depth cueing effect

enableDepthCueing() 
```
```python
# Disable depth cueing effect

disableDepthCueing() 
```
```python
# Set depth cueing starting position in world space

setDepthCueingStart(float v) 
```
```python
# Set depth cueing density

setDepthCueingDensity(float v) 
```
```python
# Set depth cueing color

setDepthCueingColor(Color col) 
```
```python
# Enable DOF effect, only available in desktop mode

enableDOF() 
```
```python
# Disable DOF effect, only available in desktop mode

disableDOF() 
```
```python
# Set DOF focus distance, this is used by the MouseAutoFocus script

setDOFFocusDistance(float v) 
```
```python
# Set DOF aperture

setDOFAperture(float a) 
```
```python
# Set DOF focal length

setDOFFocalLength(float f) 
```
```python
# Enable outline post-process effect

enableOutline() 
```
```python
# Disable outline effect

disableOutline() 
```
```python
# Set outline effect thickness

setOutlineThickness(float v) 
```
```python
# Print the content of the current directory, outputs only the files

List<string> ls() 
```
```python
# Change the current directory

cd(string newPath) 
```
```python
# Print the current directory

pwd() 
```
```python
# Return the lastly loaded UnityMolStructure

UnityMolStructure last() 
```
```python
# Change the background color of the camera based on a color name, also changes the fog color

bg_color(string colorS) 
```
```python
# Change the background color of the camera, also changes the fog color

bg_color(Color col) 
```
```python
# Convert a color string to a standard Unity Color
# Values can be "black", "white", "yellow" ,"green", "red", "blue", "pink", "gray"
# Switch on or off the rotation around the X axis of all loaded molecules

switchRotateAxisX() 
```
```python
# Switch on or off the rotation around the Y axis of all loaded molecules

switchRotateAxisY() 
```
```python
# Switch on or off the rotation around the Z axis of all loaded molecules

switchRotateAxisZ() 
```
```python
# Change the rotation speed around the X axis

changeRotationSpeedX(float val) 
```
```python
# Change the rotation speed around the Y axis

changeRotationSpeedY(float val) 
```
```python
# Change the rotation speed around the Z axis

changeRotationSpeedZ(float val) 
```
```python
# Change the mouse scroll speed

setMouseScrollSpeed(float val) 
```
```python
# Change the speed of mouse rotations and translations

setMouseMoveSpeed(float val) 
```
```python
# Stop rotation around all axis

stopRotations() 
```
```python
# Turn docking mode on and off

switchDockingMode() 
```
```python
# Transform a string of representation type to a RepType object

RepType getRepType(string type) 
```
```python
# Transform a representation type into a string

string getTypeFromRepType(RepType rept) 
```
```python
# Measure modes : 0 = distance, 1 = angle, 2 = torsion angle

setMeasureMode(int newMode) 
```
```python

annotateAtom(string structureName, int atomId) 
```
```python

removeAnnotationAtom(string structureName, int atomId) 
```
```python

annotateSphere(Vector3 worldP, float scale = 1.0f) 
```
```python

removeAnnotationSphere(Vector3 worldP, float scale = 1.0f) 
```
```python

annotateAtomText(string structureName, int atomId, string text, bool showLine = False) 
```
```python

removeAnnotationAtomText(string structureName, int atomId, string text) 
```
```python

annotateWorldText(Vector3 worldP, float scale, string text, Color textCol) 
```
```python

removeAnnotationWorldText(Vector3 worldP, float scale, string text, Color textCol) 
```
```python
#Add a 2D text over everything
#The position is set as 0/0 = bottom/left and 1/1 is top/right of the screen

annotate2DText(Vector2 screenP, float scale, string text, Color textCol) 
```
```python

removeAnnotation2DText(Vector2 screenP, float scale, string text, Color textCol) 
```
```python

annotateLine(string structureName, int atomId, string structureName2, int atomId2) 
```
```python

removeAnnotationLine(string structureName, int atomId, string structureName2, int atomId2) 
```
```python

annotateWorldLine(Vector3 p1, Vector3 p2, float sizeLine, Color lineCol) 
```
```python

removeWorldAnnotationLine(Vector3 p1, Vector3 p2, float sizeLine, Color lineCol) 
```
```python

annotateDistance(string structureName, int atomId, string structureName2, int atomId2) 
```
```python

removeAnnotationDistance(string structureName, int atomId, string structureName2, int atomId2) 
```
```python

annotateAngle(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3) 
```
```python

removeAnnotationAngle(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3) 
```
```python

annotateDihedralAngle(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3, string structureName4, int atomId4) 
```
```python

removeAnnotationDihedralAngle(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3, string structureName4, int atomId4) 
```
```python

annotateRotatingArrow(string structureName, int atomId, string structureName2, int atomId2) 
```
```python

removeAnnotationRotatingArrow(string structureName, int atomId, string structureName2, int atomId2) 
```
```python

annotateArcLine(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3) 
```
```python

removeAnnotationArcLine(string structureName, int atomId, string structureName2, int atomId2, string structureName3, int atomId3) 
```
```python

annotateDrawLine(string structureName, List<Vector3> line, Color col) 
```
```python

removeLastDrawLine(string structureName, int id) 
```
```python
# Play a sonar sound at a world position

playSoundAtPosition(Vector3 wpos) 
```
```python
# Remove all drawing annotations

clearDrawings() 
```
```python
# Remove all annotations + Drawings

clearAnnotations() 
```
```python
# Export the given structure to an OBJ file containing several models
# BondOrder/Point/Hbonds are ignored

exportRepsToOBJFile(string structureName, string fullPath) 
```
```python
# Export the given structure to an FBX file containing several models, (Windows or Mac)
# BondOrder/Point/Hbonds are ignored

exportRepsToFBXFile(string structureName, string fullPath) 
```
