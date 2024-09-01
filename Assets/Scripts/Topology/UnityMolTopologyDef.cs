using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Xml;

namespace UMol {

public class UnityMolTopologyDef {
    public Dictionary<string, List<pairString>> bondedAtomsPerResidue;
    public Dictionary<UnityMolStructure.MolecularType, string> prefixMolType =
    new Dictionary<UnityMolStructure.MolecularType, string>() {
        {UnityMolStructure.MolecularType.Martini, "Martini_"},
        {UnityMolStructure.MolecularType.HIRERNA, "HIRERNA_"},
        {UnityMolStructure.MolecularType.OPEP, ""},
        {UnityMolStructure.MolecularType.standard, ""}
    };

    public UnityMolTopologyDef(string path = null) {
        if (path == null) {
            path = Path.Combine(Application.streamingAssetsPath , "residues.xml");
        }
        bondedAtomsPerResidue = parseOpenMMTopologyFile(path);
    }

    public Dictionary<string, List<pairString>> parseOpenMMTopologyFile(string path) {
        Dictionary<string, List<pairString>> result = new Dictionary<string, List<pairString>>();

        // Debug.Log("Reading OpenMM topology file: '"+path+"'");
        StreamReader sr;

        if (Application.platform == RuntimePlatform.Android)
        {
            var textStream = new StringReaderStream(AndroidUtils.GetFileText(path));
            sr = new StreamReader(textStream);
        }
        else
        {
            sr = new StreamReader(path);
        }

        using (sr)
        {

            string curRes = "";
            XmlTextReader xmlR = new XmlTextReader(sr);
            while (xmlR.Read()) {
                if (xmlR.Name == "Residue") {
                    string tmpA = xmlR.GetAttribute("name");
                    if (tmpA != null && tmpA != "") {
                        curRes = tmpA;
                        result[curRes] = new List<pairString>();
                    }

                }
                if (xmlR.Name == "Bond") {

                    pairString pair;
                    pair.s1 = xmlR.GetAttribute("from");
                    // pair.s1 = xmlR.GetAttribute("from").Replace("-","");
                    pair.s2 = xmlR.GetAttribute("to");
                    string o = xmlR.GetAttribute("order");
                    if (o == null) {
                        pair.order = 1;
                    }
                    else {
                        pair.order = float.Parse(o);
                    }
                    result[curRes].Add(pair);
                }
            }
        }

        return result;
    }
    public List<UnityMolAtom> getBondedAtomsInResidue(UnityMolAtom curAtom, ref List<UnityMolAtom> result) {
        string prefix = prefixMolType[curAtom.residue.chain.model.structure.structureType];
        result.Clear();
        
        string resName = null;
        if (!string.IsNullOrEmpty(prefix)) {
            resName = prefix + curAtom.residue.name;
        }
        else {
            resName = curAtom.residue.name;
        }


        List<pairString> bondedS;

        if (bondedAtomsPerResidue.TryGetValue(resName, out bondedS)) {
            foreach (pairString s in bondedS) {

                if (s.s1 == curAtom.name) {
                    if (curAtom.residue.atoms.ContainsKey(s.s1) && curAtom.residue.atoms.ContainsKey(s.s2)) {
                        result.Add(curAtom.residue.atoms[s.s1]);
                        result.Add(curAtom.residue.atoms[s.s2]);
                    }
                }
                if (s.s2 == curAtom.name) {
                    if (curAtom.residue.atoms.ContainsKey(s.s1) && curAtom.residue.atoms.ContainsKey(s.s2)) {
                        result.Add(curAtom.residue.atoms[s.s2]);
                        result.Add(curAtom.residue.atoms[s.s1]);
                    }
                }
            }
        }

        return result;
    }
    public List<UnityMolAtom> getBondedAtomsInResidue(UnityMolAtom curAtom, out List<float> bondOrders) {
        string prefix = prefixMolType[curAtom.residue.chain.model.structure.structureType];

        string resName = prefix + curAtom.residue.name;
        bondOrders = new List<float>();

        List<pairString> bondedS = new List<pairString>();
        List<UnityMolAtom> result = new List<UnityMolAtom>();
        if (bondedAtomsPerResidue.TryGetValue(resName, out bondedS)) {
            foreach (pairString s in bondedS) {

                if (s.s1 == curAtom.name) {
                    if (curAtom.residue.atoms.ContainsKey(s.s1) && curAtom.residue.atoms.ContainsKey(s.s2)) {
                        result.Add(curAtom.residue.atoms[s.s1]);
                        result.Add(curAtom.residue.atoms[s.s2]);
                        bondOrders.Add(s.order);
                        bondOrders.Add(s.order);
                    }
                }
                if (s.s2 == curAtom.name) {
                    if (curAtom.residue.atoms.ContainsKey(s.s1) && curAtom.residue.atoms.ContainsKey(s.s2)) {
                        result.Add(curAtom.residue.atoms[s.s2]);
                        result.Add(curAtom.residue.atoms[s.s1]);
                        bondOrders.Add(s.order);
                        bondOrders.Add(s.order);
                    }
                }
            }
        }

        return result;
    }

    public pairString getPreviousAtomToLink(UnityMolAtom curAtom) {
        string prefix = prefixMolType[curAtom.residue.chain.model.structure.structureType];

        string resName = prefix + curAtom.residue.name;
        List<pairString> bondedS;
        pairString result; result.s1 = null; result.s2 = null; result.order = 0;
        if (bondedAtomsPerResidue.TryGetValue(resName, out bondedS)) {
            foreach (pairString s in bondedS) {
                if (s.s1.StartsWith("-")) {
                    return s;
                }
            }
        }
        return result;
    }

}

public struct pairString {
    public string s1;
    public string s2;
    public float order;
}
}