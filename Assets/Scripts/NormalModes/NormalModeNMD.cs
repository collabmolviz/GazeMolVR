using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UMol.API;
using System.IO;
using System.Text;
using System.Linq;

namespace UMol {
public class NormalModeNMD : MonoBehaviour
{
    NormalModes normalModes;
    float factor = 1.0f;

    void Start()
    {
        UnityMolMain.disableSurfaceThread = true;
        APIPython.load("E:/PDBs/1p38.pdb");
        APIPython.showAs("c");
        APIPython.colorSelection(APIPython.last().ToSelectionName(), "c", Color.red);


        var s = APIPython.load("E:/PDBs/1p38.pdb");
        normalModes = parseNMD(s, "E:/PDBs/1p38_anm_modes.nmd");

    }
    void Update() {
        Transform loadedMol = UnityMolMain.getRepresentationParent().transform;

        if (normalModes != null) {

            factor = 10.0f * (Mathf.PingPong(Time.time, 10.0f) - 10.0f / 2);
            moveAtomsWithNormalMode(normalModes, 0, factor);
        }
    }

    void moveAtomsWithNormalMode(NormalModes nm, int idNM, float factor) {
        UnityMolStructure s = nm.residues[0].chain.model.structure;
        if (s.trajAtomPositions == null || s.trajAtomPositions.Length != s.Count) {
            s.trajAtomPositions = new Vector3[s.Count];

            //Copy original position
            for (int i = 0; i < s.Count; i++) {
                s.trajAtomPositions[i] = s.currentModel.allAtoms[i].oriPosition;
            }
        }

        Vector3[] modePerRes = nm.modeVectors[idNM];
        for (int i = 0; i < modePerRes.Length; i++) {
            UnityMolResidue r = nm.residues[i];
            foreach (UnityMolAtom a in r.atoms.Values) {
                s.trajAtomPositions[a.idInAllAtoms] = a.oriPosition + modePerRes[i] * factor;
            }
        }

        //Update real atom positions and representations
        s.trajUpdateAtomPositions();
        s.updateRepresentations(trajectory: true);
    }

    NormalModes parseNMD(UnityMolStructure s, string path) {
        Vector3[][] normalModesVec;
        StreamReader sr;

        if (Application.platform == RuntimePlatform.Android)
        {
            Stream textStream;
            textStream = new StringReaderStream(AndroidUtils.GetFileText(path));
            sr = new StreamReader(textStream);
        }
        else
        {
            FileInfo LocalFile = new FileInfo(path);
            if (!LocalFile.Exists)
            {
                throw new FileNotFoundException("File not found: " + path);
            }
            sr = new StreamReader(path);
        }
        string[] resnames = null;
        string[] chainIds = null;
        string[] resIds = null;
        List<string[]> modes = new List<string[]>();

        using(sr) {
            string line;

            while ((line = sr.ReadLine()) != null) {

                if (line.StartsWith("resnames")) {
                    resnames = line.Split(new [] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                }
                if (line.StartsWith("chids")) {
                    chainIds = line.Split(new [] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                }
                if (line.StartsWith("resids")) {
                    resIds = line.Split(new [] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                }
                if (line.StartsWith("mode")) {
                    modes.Add(line.Split(new [] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries));
                }
            }
        }
        if (resnames == null || chainIds == null || resIds == null || modes.Count == 0) {
            Debug.LogError("Failed to load NMD file");
            return null;
        }
        resnames = resnames.Skip(1).ToArray();
        chainIds = chainIds.Skip(1).ToArray();
        resIds = resIds.Skip(1).ToArray();

        Debug.Log("Loaded " + modes.Count + " modes");
        int N = resnames.Length;
        normalModesVec = new Vector3[modes.Count][];
        for (int i = 0; i < modes.Count; i++) {
            normalModesVec[i] = new Vector3[N];
            int lenmode = modes[i].Length;
            int start = lenmode - N * 3;
            for (int j = 0; j < N; j++) {
                normalModesVec[i][j] = new Vector3(-float.Parse(modes[i][start + (j * 3)]),
                                                   float.Parse(modes[i][start + (j * 3 + 1)]),
                                                   float.Parse(modes[i][start + (j * 3 + 2)]));
            }
        }
        NormalModes nm = new NormalModes();
        nm.modeVectors = normalModesVec;
        nm.getAssociatedResidues(s, chainIds, resIds, resnames);
        if (nm.residues == null)
            return null;
        return nm;
    }
}

/// Utility class to store normal mode vectors associated with the corresponding residues
public class NormalModes {
    public List<UnityMolResidue> residues;
    public Vector3[][] modeVectors;

    public void getAssociatedResidues(UnityMolStructure s, string[] chainIds, string[] resIds, string[] resNames) {
        if (chainIds.Length != resIds.Length || resIds.Length != resNames.Length) {
            Debug.LogError("Arrays have different sizes, something went wrong when parsing NMD file");
            return;
        }
        int N = modeVectors[0].Length;
        residues = new List<UnityMolResidue>(N);

        UnityMolChain chain = null;
        string prevCId = "";
        for (int i = 0; i < N; i++) {
            if (chainIds[i] != prevCId) {
                chain = s.currentModel.chains[chainIds[i]];
                prevCId = chainIds[i];
            }
            UnityMolResidue r = chain.getResidueWithId(int.Parse(resIds[i]));
            if (r.name == resNames[i])
                residues.Add(r);
        }

        if (residues.Count != N) {
            Debug.LogError("Could not associate all residues from NMD file to the loaded molecule");
            residues = null;
        }
    }

}
}
