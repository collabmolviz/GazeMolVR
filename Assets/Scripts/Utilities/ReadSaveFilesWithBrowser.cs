using SimpleFileBrowser;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.XR;
using SFB;

namespace UMol {

public class ReadSaveFilesWithBrowser : MonoBehaviour
{
    public string lastOpenedFolder = "";
    public string initPath = "";
    public string extension = "";

    void loadFileFromPath(string path, bool readHetm) {

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        path = path.Replace("file:/", "").Replace("%20", " ");
#endif
        if (!string.IsNullOrEmpty(path))
        {

            if (path.EndsWith(".xtc")) {
                string lastStructureName = API.APIPython.last().name;
                if (lastStructureName != null) {
                    API.APIPython.loadTraj(lastStructureName, path);
                }
            }
            else if (path.EndsWith(".dx")) {
                string lastStructureName = API.APIPython.last().name;
                if (lastStructureName != null) {
                    API.APIPython.loadDXmap(lastStructureName, path);
                }
            }
            else if (path.EndsWith(".py")) {
                API.APIPython.loadHistoryScript(path);
            }
            else if (path.EndsWith(".itp")) {
                string lastStructureName = API.APIPython.last().name;
                if (lastStructureName != null)
                    API.APIPython.loadMartiniITP(lastStructureName, path);
            }
            else if (path.EndsWith(".psf")) {
                string lastStructureName = API.APIPython.last().name;
                if (lastStructureName != null)
                    API.APIPython.loadPSFTopology(lastStructureName, path);
            }
            else if (path.EndsWith(".top")) {
                string lastStructureName = API.APIPython.last().name;
                if (lastStructureName != null)
                    API.APIPython.loadTOPTopology(lastStructureName, path);
            }
            else {
                API.APIPython.load(path, readHetm);
            }
        }
    }


    public void readFiles(bool readHetm = true, bool forceDesktop = false)
    {

        string[] paths = filesToRead(initPath, extension, readHetm, forceDesktop);
        if (paths != null && paths.Length != 0) {
            if (paths[0] != "")
                initPath = Path.GetDirectoryName(paths[0]);

            for (int i = 0; i < paths.Length; i++)
            {
                loadFileFromPath(paths[i], readHetm);
            }
        }
    }

    public void saveState() {
        string path = stateToSave(initPath);
        if (path != null) {
            API.APIPython.saveHistoryScript(path);
        }
    }

    public void saveJSON(string content){
        string path = jsonToSave(initPath);
        if (path != null) {
            System.IO.File.WriteAllText(path, content);
            Debug.Log("Wrote to : "+path);
        }
    }

    public void readState(bool forceDesktop = false) {
        string[] paths = filesToRead(initPath, "py", true, forceDesktop);
        if (paths != null && paths.Length > 0)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            paths[0] = paths[0].Replace("file:/", "");
#endif
            API.APIPython.loadHistoryScript(paths[0]);
        }
    }

    public string stateToSave(string initPath = "") {
#if (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WEBGL )
        var extensions = new []
        {
            new ExtensionFilter("UnityMol State Files", "py" )
        };

        return StandaloneFileBrowser.SaveFilePanel("Save UnityMol State", initPath, "UMolState.py", extensions);

#else
        StartDialogSave("Save UnityMol State", ".py");
        return null;
#endif
    }

    public string jsonToSave(string initPath = "") {
#if (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WEBGL )
        var extensions = new []
        {
            new ExtensionFilter("Json file", "json" )
        };

        return StandaloneFileBrowser.SaveFilePanel("Save Json file", initPath, "UMol.json", extensions);

#else
        StartDialogSave("Save Json file", ".json");
        return null;  
#endif
    }

    public string[] filesToRead(string initPath = "", string extension = "", bool readHetm = true, bool forceDesktop = false)
    {


        var extensions = new []
        {
            new ExtensionFilter("Molecule Files", "pdb", "ent", "mmcif", "cif", "gro", "mol2", "sdf", "mol", "xyz"),
            new ExtensionFilter("Trajectory Files", "xtc"),
            new ExtensionFilter("Density map Files", "dx"),
            new ExtensionFilter("State/Script Files", "py"),
            new ExtensionFilter("Martini itp Files", "itp"),
            new ExtensionFilter("PSF Files", "psf"),
            new ExtensionFilter("TOP Files", "top"),
            new ExtensionFilter("All Files", "*"),
        };

        string[] paths = null;
        //Use native file browser for Windows and Mac and WebGL (https://github.com/gkngkc/UnityStandaloneFileBrowser)
#if (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WEBGL )
        if (!UnityMolMain.inVR() || forceDesktop) {
            if (extension == "")
            {
                paths = StandaloneFileBrowser.OpenFilePanel("Open File", initPath, extensions, true);
            }
            else if (extension == "*")
            {
                paths = StandaloneFileBrowser.OpenFilePanel("Open File", initPath, "", true);
            }
            else
            {
                paths = StandaloneFileBrowser.OpenFilePanel("Open File", initPath, extension, true);
            }
        }
        else {
            StartDialog(readHetm);
        }
#else //Use asset based file browser (https://github.com/yasirkula/UnitySimpleFileBrowser)
        //Uses a coroutine
        StartDialog(readHetm);
#endif
        return paths;
    }

    void StartDialog(bool readHetm)
    {
        // Set filters (optional)
        // It is sufficient to set the filters just once (instead of each time before showing the file browser dialog),
        // if all the dialogs will be using the same filters
        FileBrowser.SetFilters( true, new FileBrowser.Filter( "Supported", ".pdb", ".cif", ".mmcif", ".gro",
                                ".mol2", ".xyz", ".sdf", ".mol", ".py", ".dx", ".xtc", ".itp", ".psf", ".top"));

        // Set default filter that is selected when the dialog is shown (optional)
        // Returns true if the default filter is set successfully
        // In this case, set Images filter as the default filter
        FileBrowser.SetDefaultFilter( ".pdb" );

        // Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
        // Note that when you use this function, .lnk and .tmp extensions will no longer be
        // excluded unless you explicitly add them as parameters to the function
        // FileBrowser.SetExcludedExtensions( ".lnk", ".tmp", ".zip", ".rar", ".exe" );

        // // Add a new quick link to the browser (optional) (returns true if quick link is added successfully)
        // // It is sufficient to add a quick link just once
        // // Name: Users
        // // Path: C:\Users
        // // Icon: default (folder icon)
        // FileBrowser.AddQuickLink( "Users", "C:\\Users", null );

        // Show a save file dialog
        // onSuccess event: not registered (which means this dialog is pretty useless)
        // onCancel event: not registered
        // Save file/folder: file, Initial path: "C:\", Title: "Save As", submit button text: "Save"
        // FileBrowser.ShowSaveDialog( null, null, false, "C:\\", "Save As", "Save" );

        // Show a select folder dialog
        // onSuccess event: print the selected folder's path
        // onCancel event: print "Canceled"
        // Load file/folder: folder, Initial path: default (Documents), Title: "Select Folder", submit button text: "Select"
        // FileBrowser.ShowLoadDialog( (path) => { Debug.Log( "Selected: " + path ); },
        //                                () => { Debug.Log( "Canceled" ); },
        //                                true, null, "Select Folder", "Select" );

        // Coroutine example
        StartCoroutine( ShowLoadDialogCoroutine(readHetm) );
    }

    void StartDialogSave(string extName, string exten) {
        FileBrowser.SetFilters( true, new FileBrowser.Filter( extName, exten) );

        FileBrowser.SetDefaultFilter( exten );

        StartCoroutine( ShowSaveDialogCoroutine() );
    }

    IEnumerator ShowLoadDialogCoroutine(bool readHetm)
    {
        if (lastOpenedFolder == "") {
            string savedFolder = PlayerPrefs.GetString("lastOpenedFolderVR");
            if (!string.IsNullOrEmpty(savedFolder)) {
                lastOpenedFolder = savedFolder;
            }
        }
        // Show a load file dialog and wait for a response from user
        // Load file/folder: file, Initial path: default (Documents), Title: "Load File", submit button text: "Load"
        yield return FileBrowser.WaitForLoadDialog( false, lastOpenedFolder, "Load File", "Load" );

        // Dialog is closed
        // Print whether a file is chosen (FileBrowser.Success)
        // and the path to the selected file (FileBrowser.Result) (null, if FileBrowser.Success is false)

        if (FileBrowser.Success)
        {
            lastOpenedFolder = Path.GetDirectoryName(FileBrowser.Result);
            PlayerPrefs.SetString("lastOpenedFolderVR", lastOpenedFolder);
            foreach (string p in FileBrowser.Results) {
                loadFileFromPath(p, readHetm);
            }
        }
        else
        {
            Debug.LogError("Could not load selected file");
        }
    }
    IEnumerator ShowSaveDialogCoroutine() {
        yield return FileBrowser.WaitForSaveDialog( false, lastOpenedFolder, "Save File", "Save" );

        if (FileBrowser.Success)
        {
            lastOpenedFolder = Path.GetDirectoryName(FileBrowser.Result);
            API.APIPython.saveHistoryScript(FileBrowser.Result);
        }
        else
        {
            Debug.LogError("Could not save to selected file");
        }
    }
}
}