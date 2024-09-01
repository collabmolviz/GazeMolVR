using UnityEngine;
using UnityEditor;
using UnityEngine.XR;
using UnityEditor.Callbacks;
using System;
using System.IO;

namespace UMol {
public class UMolPostBuild {
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {
        string mainFolder = Path.GetDirectoryName(pathToBuiltProject);
        string folderToDel1 = null;
        string folderToDel2 = null;


        if (target == BuildTarget.StandaloneOSX) {
            folderToDel1 = "Linux";
            folderToDel2 = "Windows";
            mainFolder = pathToBuiltProject;
            mainFolder = Path.Combine(Path.Combine(mainFolder, "Contents"), "Resources");
        }
        else if (target == BuildTarget.StandaloneLinux64) {
            folderToDel1 = "Windows";
            folderToDel2 = "OSX";
        }
        else if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64) {
            folderToDel1 = "Linux";
            folderToDel2 = "OSX";
        }
        else {
            Debug.LogError("Post build not supported for this platform");
            return;
        }
        string saFolder = null;
        string pluginsFolder = null;

        foreach (string f in Directory.GetDirectories(mainFolder)) {
            if (f.EndsWith("Data") || f == "Contents") {
                saFolder = Path.Combine(f, "StreamingAssets");
                pluginsFolder = Path.Combine(f, "Plugins");
                break;
            }
        }
        if (saFolder == null) {
            Debug.LogError("Couldn't find Streaming asset folder");
            return;
        }

        foreach (string f1 in Directory.GetDirectories(saFolder)) {
            foreach (string f2 in Directory.GetDirectories(f1)) {
                if (f2.EndsWith(folderToDel1) || f2.EndsWith(folderToDel2)) {
                    Directory.Delete(f2, true);
                }
            }
        }

        if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64) {
            if (!PlayerSettings.virtualRealitySupported) {
                pluginsFolder = Path.Combine(pluginsFolder, "x86_64");
                string vrlip = Path.Combine(pluginsFolder, "OVRLipSync.dll");
                string vrspace = Path.Combine(pluginsFolder, "AudioPluginOculusSpatializer.dll");
                string vrplug = Path.Combine(pluginsFolder, "OVRPlugin.dll");
                string vrleap = Path.Combine(pluginsFolder, "LeapC.dll");
                if (File.Exists(vrlip))
                    File.Delete(vrlip);
                if (File.Exists(vrspace))
                    File.Delete(vrspace);
                if (File.Exists(vrplug))
                    File.Delete(vrplug);
                if (File.Exists(vrleap))
                    File.Delete(vrleap);
            }
        }

    }
}
}