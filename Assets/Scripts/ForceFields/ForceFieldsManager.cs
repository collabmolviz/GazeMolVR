//
// Manager to load/select force fields from UI
// Joao Rodrigues (j.p.g.l.m.rodrigues@gmail.com)
//

// Unity imports
using UnityEngine;

// C# imports
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using MiniJSON;

namespace UMol {
namespace ForceFields {
public class ForceFieldsManager  {

    public ForceField activeForceField;
    private static List<ForceField> ffList = new List<ForceField>();
    public static List<string> ffNameList = new List<string>();

    // Constructor
    public ForceFieldsManager() {

        // Scan FF folder for .json files and try parsing them
        string assetsPath = Application.streamingAssetsPath;

        if (Application.platform == RuntimePlatform.Android)
        {
            string fileName = Path.Combine(assetsPath, "ff14SB.json");

            ForceField ff = LoadForceField(fileName);
            ffList.Add(ff);
            ffNameList.Add(ff.name);
        }
        else
        {

            DirectoryInfo dir = new DirectoryInfo(assetsPath);
            FileInfo[] info = dir.GetFiles("*.json");
            foreach (FileInfo f in info) {
                string fileName = f.FullName;
                try {
                    ForceField ff = LoadForceField(fileName);
                    ffList.Add(ff);
                    ffNameList.Add(ff.name);
                }
                catch {
                    Debug.LogFormat("Could not parse force field JSON file: {0}", fileName);
                }
            }
        }
        SetForceField("ff14SB");
    }

    private ForceField LoadForceField(string fileName) {

        IDictionary deserializedData = null;

        StreamReader sr;

        if (Application.platform == RuntimePlatform.Android) {
            var textStream = new StringReaderStream(AndroidUtils.GetFileText(fileName));
            sr = new StreamReader(textStream);
        }
        else
            sr = new StreamReader(fileName);

        using (sr) {
            string jsonString = sr.ReadToEnd();
            deserializedData = (IDictionary) Json.Deserialize(jsonString);
        }

        string name = Path.GetFileNameWithoutExtension(fileName);
        ForceField ff = new ForceField(name);

        // Read atom types first
        IDictionary atomTypeLibraryJSON = (IDictionary)deserializedData["atom_types"];
        foreach (string atomTypeName in atomTypeLibraryJSON.Keys) {
            FFAtomType newAtomType = new FFAtomType(atomTypeName);
            IDictionary typeParams = (IDictionary)atomTypeLibraryJSON[atomTypeName];

            newAtomType.eps = float.Parse(typeParams["epsilon"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
            newAtomType.rmin = float.Parse(typeParams["rmin"].ToString(), System.Globalization.CultureInfo.InvariantCulture);

            // Debug.Log(newAtomType.ToString());
            ff.AddAtomType(newAtomType);
        }

        // Read residue library
        IDictionary residueLibraryJSON = (IDictionary)deserializedData["residues"];

        foreach (string residueName in residueLibraryJSON.Keys) {
            FFResidue newResidue = new FFResidue(residueName);
            IDictionary atoms = (IDictionary)residueLibraryJSON[residueName];

            foreach (string atomName in atoms.Keys) {
                IDictionary thisAtom = (IDictionary)atoms[atomName];

                FFAtom newAtom = new FFAtom(residueName, atomName, (string)thisAtom["type"]);
                newAtom.charge = float.Parse(thisAtom["charge"].ToString(), System.Globalization.CultureInfo.InvariantCulture);

                // Debug.Log(newAtom.ToString());
                newResidue.AddAtom(newAtom);
            }
            // Debug.Log(newResidue.ToString());
            ff.AddResidue(newResidue);
        }

        // Debug.LogFormat("Loaded forcefield: {0}", ff);
        return ff;
    }

    public void SetForceField(string ffName) {

        if (!ffNameList.Contains(ffName)) {
            string message = "Force field '{0}' is not available. Pick from {1}";
            message = String.Format(message, ffName, String.Join(", ", ffNameList.ToArray()));

            Debug.LogFormat(message);
            throw new System.Exception(message);
        }
        else {
            int idx = ffNameList.IndexOf(ffName);
            activeForceField = ffList[idx];

            // Debug.LogFormat("Enabling force field '{0}'", activeForceField.name);

            return;
        }
    }

}
}
}