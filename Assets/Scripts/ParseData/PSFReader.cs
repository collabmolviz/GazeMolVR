using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Xml;
using System.Text;
using Unity.Mathematics;

namespace UMol {

public class PSFReader {

    public static UnityMolBonds readTopologyFromPSF(string path, UnityMolStructure s) {


        StreamReader sr;

        if (Application.platform == RuntimePlatform.Android) {
            var textStream = new StringReaderStream(AndroidUtils.GetFileText(path));
            sr = new StreamReader(textStream);
        }
        else {
            sr = new StreamReader(path);
        }

        using (sr) {
            string line = "";
            string header = sr.ReadLine();
            if (!header.StartsWith("PSF")) {
                Debug.LogError("Not a valid PSF file (header)");
                return null;
            }

            while ((line = sr.ReadLine()) != null) {
                if(line.Contains("!NBOND")){
                    break;
                }
            }

            if (!line.Contains("!NBOND")) {
                Debug.LogError("Not a valid PSF file (NBOND)");
                return null;
            }

            string NbondsS = line.Split(new [] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries)[0];
            int Nbonds = int.Parse(NbondsS);

            List<int2> bondedIds = new List<int2>(Nbonds);
            Dictionary<int, int> bondPerAtom = new Dictionary<int, int>();
            int maxBondPerA = 0;
            for (int i = 0; i < Nbonds / 4; i++) {//4 bonds per line and 1 bond = 2 int
                if ((line = sr.ReadLine()) == null) {
                    Debug.LogError("Not enough bond lines");
                    return null;
                }
                string[] split = line.Split(new [] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                int2 b1; b1.x = int.Parse(split[0]); b1.y = int.Parse(split[1]);
                int2 b2; b2.x = int.Parse(split[2]); b2.y = int.Parse(split[3]);
                int2 b3; b3.x = int.Parse(split[4]); b3.y = int.Parse(split[5]);
                int2 b4; b4.x = int.Parse(split[6]); b4.y = int.Parse(split[7]);
                bondedIds.Add(b1);
                bondedIds.Add(b2);
                bondedIds.Add(b3);
                bondedIds.Add(b4);
                if(!bondPerAtom.ContainsKey(b1.x)) bondPerAtom[b1.x] = 0;
                if(!bondPerAtom.ContainsKey(b1.y)) bondPerAtom[b1.y] = 0;
                if(!bondPerAtom.ContainsKey(b2.x)) bondPerAtom[b2.x] = 0;
                if(!bondPerAtom.ContainsKey(b2.y)) bondPerAtom[b2.y] = 0;
                if(!bondPerAtom.ContainsKey(b3.x)) bondPerAtom[b3.x] = 0;
                if(!bondPerAtom.ContainsKey(b3.y)) bondPerAtom[b3.y] = 0;
                if(!bondPerAtom.ContainsKey(b4.x)) bondPerAtom[b4.x] = 0;
                if(!bondPerAtom.ContainsKey(b4.y)) bondPerAtom[b4.y] = 0;

                bondPerAtom[b1.x]++; bondPerAtom[b1.y]++;
                bondPerAtom[b2.x]++; bondPerAtom[b2.y]++;
                bondPerAtom[b3.x]++; bondPerAtom[b3.y]++;
                bondPerAtom[b4.x]++; bondPerAtom[b4.y]++;

                maxBondPerA = Mathf.Max(bondPerAtom[b1.x], Mathf.Max(bondPerAtom[b1.y], maxBondPerA));
                maxBondPerA = Mathf.Max(bondPerAtom[b2.x], Mathf.Max(bondPerAtom[b2.y], maxBondPerA));
                maxBondPerA = Mathf.Max(bondPerAtom[b3.x], Mathf.Max(bondPerAtom[b3.y], maxBondPerA));
                maxBondPerA = Mathf.Max(bondPerAtom[b4.x], Mathf.Max(bondPerAtom[b4.y], maxBondPerA));

            }

            UnityMolBonds bonds = new UnityMolBonds();
            if(maxBondPerA > bonds.NBBONDS)
                bonds.NBBONDS = maxBondPerA;
            foreach (int2 b in bondedIds) {
                UnityMolAtom a1 = s.currentModel.allAtoms[b.x - 1];
                UnityMolAtom a2 = s.currentModel.allAtoms[b.y - 1];
                if (!bonds.isBondedTo(a1, a2)) {
                    bonds.Add(a1, a2);
                }
            }
            return bonds;
        }
    }

    private static outAtomLine parseAtomLine(string line, int PSFType) {

        string atomId = null;
        string segId = null;
        string resId = null;
        string resName = null;
        string atomName = null;
        // string atomType = null;

        if (PSFType == 0) { //Standard
            atomId = line.Substring(8).Trim();
            segId = line.Substring(9, 4).Trim();
            resId = line.Substring(14, 4).Trim();
            resName = line.Substring(19, 4).Trim();
            atomName = line.Substring(24, 4).Trim();
            // atomType = line.Substring(29, 4).Trim();
            // charge = line.Substring(34, 14).Trim();
            // mass = line.Substring(48, 14).Trim();
        }
        if (PSFType == 1) { //NAMD
            string[] split = line.Split(new [] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            atomId = split[0];
            segId = split[1];
            resId = split[2];
            resName = split[3];
            atomName = split[4];
            // atomType = split[5];
        }
        if (PSFType == 2) { //Extended
            atomId = line.Substring(10).Trim();
            segId = line.Substring(11, 8).Trim();
            resId = line.Substring(20, 8).Trim();
            resName = line.Substring(29, 8).Trim();
            atomName = line.Substring(38, 8).Trim();
            // atomType = line.Substring(47, 4).Trim();
        }

        outAtomLine res;
        res.atomId = int.Parse(atomId);
        res.segId = int.Parse(segId);
        res.resId = int.Parse(resId);
        res.resName = resName;
        res.atomName = atomName;

        return res;
    }


    struct outAtomLine {
        public int atomId;
        public int segId;
        public int resId;
        public string resName;
        public string atomName;
    }
}
}
