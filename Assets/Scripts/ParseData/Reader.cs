using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace UMol {

public class ParsingException: Exception {
    public ParsingException() {}
    public ParsingException(string message)
    : base(message) {}
    public ParsingException(string message, Exception inner)
    : base(message, inner) {}
}



public abstract class Reader {


    ///Read PDB/mmCIF/GRO frames as trajectory
    public bool modelsAsTraj = true;

    protected string fileName;
    protected string fileNameWithoutExtension;

    public static int limitBigMolecule = 5000;

    public struct secStruct {
        public string chain;
        public int start;
        public int end;
        public UnityMolResidue.secondaryStructureType type;
    }


    public Reader() {}

    public Reader(string fileName) {

        this.fileName = fileName;
        updateFileNames();

    }

    public void updateFileNames() {
        //Get the filename without extensions and without the path
        if ( fileName != "") {

            FileInfo f = new FileInfo(fileName);

            this.fileNameWithoutExtension = f.Name.Substring(0, f.Name.IndexOf("."));
            if (this.fileNameWithoutExtension == "") {
                this.fileNameWithoutExtension = f.Name;
            }
        }
        else {
            this.fileNameWithoutExtension = "";
        }
    }

    /// <summary>
    /// Reads a file from local HDD and parses the data
    /// </summary>
    public UnityMolStructure Read(bool readHet = true, bool readWater = true, bool justParse = false, int forceType = -1) {
        UnityMolStructure structure = null;

        StreamReader sr;
        //Detect compressed files
        if ( fileName.ToLower().EndsWith("gz") ) {
            GZipStream flatStream = new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress);
            sr = new StreamReader(flatStream);
        }
        else
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                Stream textStream;
                textStream = new StringReaderStream(AndroidUtils.GetFileText(fileName));
                sr = new StreamReader(textStream);
            }
            else
            {
                FileInfo LocalFile = new FileInfo(fileName);
                if (!LocalFile.Exists)
                {
                    throw new FileNotFoundException("File not found: " + fileName);
                }
                sr = new StreamReader(fileName);
            }
        }

        using(sr) {
            try {
                if (forceType == -1) {
                    structure = ReadData(sr, readHet, readWater, justParse);
                }
                else {
                    structure = ReadData(sr, readHet, readWater, justParse, (UnityMolStructure.MolecularType)forceType);
                }
                structure.readHET = readHet;
                structure.modelsAsTraj = modelsAsTraj;
                structure.path = fileName;

            }
            catch (Exception err) {
                Debug.LogError("Something went wrong when parsing your file: " + err);
                throw err;
            }
        }

        return structure;
    }

    /// <summary>
    /// Reads a file from string and parses the data
    /// </summary>
    public UnityMolStructure ReadFromString(string content, bool readHet = true, bool readWater = true) {
        UnityMolStructure structure = null;

        byte[] bytes = Encoding.UTF8.GetBytes(content);
        MemoryStream ms = new MemoryStream(bytes);
        StreamReader sr = new StreamReader(ms, System.Text.Encoding.UTF8, true);

        using(sr) {
            try {
                structure = ReadData(sr, readHet, readWater);
            }
            catch (Exception err) {
                Debug.LogError("Something went wrong when parsing your file: " + err);
                throw err;
            }
        }

        return structure;
    }

    /// <summary>
    /// Fills secondary structure types for each residue of each model
    /// </summary>
    public static void FillSecondaryStructure(UnityMolStructure structure, List<secStruct> secStructsList) {
        StringBuilder sb = new StringBuilder();

        //Set everythig to coil
        foreach (UnityMolModel model in structure.models) {
            foreach (UnityMolChain c in model.chains.Values) {
                foreach (UnityMolResidue r in c.residues) {
                    r.secondaryStructure = UnityMolResidue.secondaryStructureType.Coil;
                }
            }
        }

        //Use the parsed secondary structure list to fill ss types
        foreach (secStruct ss in secStructsList) {
            foreach (UnityMolModel model in structure.models) {
                try {
                    UnityMolChain c = model.chains[ss.chain];
                    foreach (UnityMolResidue r in c.residues) {
                        if (r.id >= ss.start && r.id <= ss.end) {
                            r.secondaryStructure = ss.type;
                        }
                    }
                }
                catch {
                    sb.Append("Secondary Structure parsing : No chain ");
                    sb.Append(ss.chain);
                    sb.Append(" parsed in the PDB\n");
                    break;
                }
            }
        }
        if (sb.Length != 0) {
            Debug.LogWarning(sb.ToString());
        }

        if (secStructsList.Count != 0) {
            structure.ssInfoFromFile = true;
        }
    }

    /// <summary>
    /// Creates a gameobject as a parent for future annotations
    /// </summary>
    public static void CreateUnityObjects(string sName, UnityMolSelection sel) {

        Transform repParent = UnityMolMain.getRepStructureParent(sName).transform;

        Transform annoPar = repParent.Find("AtomParent");
        if (annoPar == null) {
            annoPar = new GameObject("AtomParent").transform;
        }
        annoPar.parent = repParent;

        annoPar.transform.localPosition = Vector3.zero;
        annoPar.transform.localRotation = Quaternion.identity;
        annoPar.transform.localScale = Vector3.one;

        sel.structures[0].annotationParent = annoPar;
    }

    /// <summary>
    /// Start 2 threads (EDTSurf + MSMS) per chain of the structure to pre-compute molecular surfaces
    /// </summary>
    public static SurfaceThread startSurfaceThread(UnityMolSelection sel) {
        if (UnityMolMain.disableSurfaceThread)
            return null;

        if (sel.Count >= UnityMolMain.surfaceThreadLimit)
            return null;

        SurfaceThread sf = new SurfaceThread();
        sf.sel = sel;
        sf.StartThread();
        return sf;
    }



    /// <summary>
    /// Fills the structureType field in the UnityMolStructure class based on atom names, uses the 5000 first atoms
    /// </summary>
    public static void identifyStructureMolecularType(UnityMolStructure s) {

        int count = 0;
        const int limitTest = 5000;
        StringBuilder sb = new StringBuilder();
        foreach (UnityMolAtom a in s.currentModel.allAtoms) {

            if (a.name == "BB" || a.name == "BB1" || a.name == "SC1" || //Martini 2.2P
                    a.name == "BAS" || a.name == "SID" || a.name == "SI1" || //Martini 2.2 & 2.1
                    a.name == "DC" || a.name == "DG" ) { //Martini DNA
                s.structureType = UnityMolStructure.MolecularType.Martini;
                break;
            }

            sb.Clear();
            sb.Append("MARTINI_");
            sb.Append(a.residue.name.ToUpper());
            sb.Append("_");
            sb.Append(a.name.ToUpper());
            string martiniAtomName = sb.ToString();
            if (UnityMolMain.atomColors.isKnownAtom(martiniAtomName)) {
                s.structureType = UnityMolStructure.MolecularType.Martini;
                break;
            }

            // if (UnityMolMain.loadedITP.TryGetValue(a.residue.name, out res) && res.ContainsKey(a.name)) {
            //     s.structureType = UnityMolStructure.MolecularType.Martini;
            //     break;
            // }

            if (a.name == "C5*" || a.name == "O5*" || a.name == "G1" || a.name == "G2" || a.name == "A1" || a.name == "A2") {
                s.structureType = UnityMolStructure.MolecularType.HIRERNA;
                break;
            }

            if (MDAnalysisSelection.isProtein(a) && a.name == a.residue.name) { //OPEP
                s.structureType = UnityMolStructure.MolecularType.OPEP;
                break;
            }

            if (count >= limitTest) {
                break;
            }

            //Check non-protein Martini ?
            // if(!MDAnalysisSelection.isProtein(a) && ()){//Martini 2.2 & 2.1
            //     s.structureType = UnityMolStructure.MolecularType.Martini;
            //     break;
            // }
            count++;
        }
        Debug.Log("Molecule type identified : " + s.structureType);
    }


    /// <summary>
    /// Check if the string full contains at the begining, the string comp
    /// </summary>
    protected static bool QuickStartWith(string full, string comp) {
        // return comp == full.Substring(0, Mathf.Min(comp.Length, full.Length));
        return full.StartsWith(comp, StringComparison.Ordinal);
    }


    //Methods which needs to be implemented in child classes :
    protected abstract UnityMolStructure ReadData(StreamReader sr, bool readHet = true, bool readWater = true,
            bool simplyParse = false, UnityMolStructure.MolecularType? forceStructureType = null);

    // Return a Reader according to either the filename extension or the format argument given.
    public static Reader GuessReaderFrom(string filename, string format = "") {

        string type = "";

        //Parse the filename extension and obtain a type for the switch
        if (format != "") {
            type = format.ToLower();
        }
        else {

            if (PDBReader.PDBextensions.Any( x => filename.ToLower().EndsWith(x))) {
                type = "pdb";
            }
            else if (PDBxReader.PDBxextensions.Any( x => filename.ToLower().EndsWith(x))) {
                type = "cif";
            }
            else if (GROReader.GROextensions.Any( x => filename.ToLower().EndsWith(x))) {
                type = "gro";
            }
            else if (SDFReader.SDFextensions.Any( x => filename.ToLower().EndsWith(x))) {
                type = "sdf";
            }
            else if (MOL2Reader.MOL2extensions.Any( x => filename.ToLower().EndsWith(x))) {
                type = "mol2";
            }
            else if (XYZReader.XYZextensions.Any( x => filename.ToLower().EndsWith(x))) {
                type = "xyz";
            }
            else {
                type = Path.GetExtension(filename).ToLower();
            }
        }


        switch (type) {
        case "pdb":
            return new PDBReader(filename);
        case "cif":
            return new PDBxReader(filename);
        case "gro":
            return new GROReader(filename);
        case "sdf":
            return new SDFReader(filename);
        case "mol2":
            return new MOL2Reader(filename);
        case "xyz":
            return new XYZReader(filename);
        default:
            Debug.LogWarning("The file extension '" + type + "' is not supported");
            break;
        }

        return null;
    }

    protected static string findNewAtomName(HashSet<string> residueAtoms, string name) {
        int toAdd = 2;

        Regex reg = new Regex(@"_[0-9]*$");
        Match match = reg.Match(name);

        if (match.Success) {
            name = name.Substring(0, match.Index);
        }
        string result = name + "_" + toAdd.ToString();
        while (residueAtoms.Contains(result)) {
            toAdd++;
            result = name + "_" + toAdd.ToString();
        }
        return result;
    }

    //From https://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin
    // whitespace detection method: very fast, a lot faster than Char.IsWhiteSpace
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // if it's not inlined then it will be slow!!!
    public static bool isWhiteSpace(char ch) {
        // this is surprisingly faster than the equivalent if statement
        switch (ch) {
        case '\u0009': case '\u000A': case '\u000B': case '\u000C': case '\u000D':
        case '\u0020': case '\u0085': case '\u00A0': case '\u1680': case '\u2000':
        case '\u2001': case '\u2002': case '\u2003': case '\u2004': case '\u2005':
        case '\u2006': case '\u2007': case '\u2008': case '\u2009': case '\u200A':
        case '\u2028': case '\u2029': case '\u202F': case '\u205F': case '\u3000':
            return true;
        default:
            return false;
        }
    }
    //From https://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin
    ///Version of Substring that also combines Trim -> avoid allocations
    ///Expects StringBuilder to be allocated
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SubstringWithTrim(StringBuilder sb, string s, int start, int len) {
        sb.Clear();
        for (int i = start; i < start + len; i++) {
            if (!isWhiteSpace(s[i])) {
                sb.Append(s[i]);
            }
        }
        return sb.ToString();
    }

    static bool isDigit(char c) {
        return c <= '9' && c >= '0';
    }
    public static bool OnlyDigits(string s, int start = 0, int stop = -1) {
        if (stop <= 0)
            stop = s.Length;
        for (int i = start; i < stop; i++) {
            if (s[i] != '-' && !isWhiteSpace(s[i]) && !isDigit(s[i]))
                return false;
        }
        return true;
    }

    public static unsafe int ParseInt(string s, int start = 0, int stop = -1)
    {
        int dummy = 0;
        return ParseInt(ref dummy, s, start, stop);
    }

    ///Faster version of int.Parse that avoids doing Substring and Trim -> avoid allocations
    public static unsafe int ParseInt(ref int endedAt, string s, int start = 0, int stop = -1)
    {
        int pos = start;           // read pointer position
        int part = 0;          // the current part (int, float and sci parts of the number)
        bool neg = false;      // true if part is a negative number
        if (stop <= 0)
            stop = s.Length;
        int* ret = stackalloc int[1];
        *ret = -int.MaxValue;

        fixed (char* input = s) {
            while (pos < stop && (*(input + pos) > '9' || *(input + pos) < '0') && *(input + pos) != '-')
                pos++;

            if (pos == stop) {
                endedAt = pos;
                return *ret;
            }
            // sign
            if (*(input + pos) == '-')
            {
                neg = true;
                pos++;
            }

            // integer part
            while (pos < stop && !(input[pos] > '9' || input[pos] < '0'))
                part = part * 10 + (input[pos++] - '0');

            *ret = neg ? (part * -1) : part;
        }
        endedAt = pos;
        return *ret;
    }


    /// PDB hybrid 36 implementation to support hexadecimal values
    /// Cannot be negative
    public unsafe static int ParseIntH36(string s, int size, int start = 0, int stop = -1)
    {
        int pos = start;           // read pointer position
        int part = 0;          // the current part
        if (stop <= 0)
            stop = s.Length;
        int* ret = stackalloc int[1];
        *ret = -1;

        fixed (char* input = s) {
            while (pos < stop &&
                    ((*(input + pos) > '9' && *(input + pos) < 'A')) || *(input + pos) < '0')
                pos++;

            if (pos == stop)
                return *ret;

            // integer part
            while (pos < stop &&
                    (input[pos] <= '9' && input[pos] >= '0') || (input[pos] <= 'z' && input[pos] >= 'A')) {
                if (isDigit(input[pos])) {
                    part = part * 16 + (input[pos++] - '0');
                }
                else {
                    part = part * 16 + (input[pos++] - 55);
                }
            }

            if (size == 4) {
                *ret = part - 30960;
            }
            else if (size == 5) {
                *ret = part - 555360;
            }
        }
        return *ret;
    }

    ///Expects res array to be allocated with sufficient capacity, read maximum maxN floats, returns where it stopped in string
    public static unsafe int ParseFloats(int maxN, string s, ref float[] res, int start = 0, int stop = -1)
    {

        int pos = start;
        if (stop <= 0)
            stop = s.Length;

        int idF = 0;
        float tmp = 0.0f;
        while (pos < stop) {
            int last = ParseFloatFast(s, pos, stop, out tmp);

            pos = last + 1;
            res[idF] = tmp;
            idF++;
            if (idF == maxN){
                return pos;
            }
        }

        return stop;
    }


    private static bool IgnoreChar(char c) {
        return c < 33;
    }

    ///Faster version of float.Parse that avoids doing Substring and Trim -> avoid allocations
    public static bool TryParseFloatFast(string s, int begin, int end, out float result)
    {
        result = 0.0f;
        char c = s[begin];
        int sign = 0;
        int start = begin;

        if (c == '-')
        {
            sign = -1;
            start = begin + 1;
        }
        else if (c > 57 || c < 48)
        {
            if (IgnoreChar(c))
            {
                do
                {
                    ++start;
                }
                while (start < end && IgnoreChar(c = s[start]));

                if (start >= end)
                {
                    return false;
                }

                if (c == '-')
                {
                    sign = -1;
                    ++start;
                }
                else
                {
                    sign = 1;
                }
            }
            else
            {
                result = 0;
                return false;
            }
        }
        else
        {
            start = begin + 1;
            result = 10 * result + (c - 48);
            sign = 1;
        }

        int i = start;

        for (; i < end; ++i)
        {
            c = s[i];
            if (c > 57 || c < 48)
            {
                if (c == '.')
                {
                    ++i;
                    goto DecimalPoint;
                }
                else
                {
                    result = 0;
                    return false;
                }
            }

            result = 10 * result + (c - 48);
        }

        result *= sign;
        return true;

        DecimalPoint:

        long temp = 0;
        int length = i;
        float exponent = 0;

        for (; i < end; ++i)
        {
            c = s[i];
            if (c > 57 || c < 48)
            {
                if (!IgnoreChar(c))
                {
                    if (c == 'e' || c == 'E')
                    {
                        length = i - length;
                        goto ProcessExponent;
                    }

                    result = 0;
                    return false;
                }
                else
                {
                    length = i - length;
                    goto ProcessFraction;
                }
            }
            temp = 10 * temp + (c - 48);
        }
        length = i - length;

        ProcessFraction:

        float fraction = (float)temp;

        if (length < _powLookup.Length)
        {
            fraction = fraction / _powLookup[length];
        }
        else
        {
            fraction = fraction / _powLookup[_powLookup.Length - 1];
        }

        result += fraction;

        result *= sign;

        if (exponent > 0)
        {
            result *= exponent;
        }
        else if (exponent < 0)
        {
            result /= -exponent;
        }

        return true;

        ProcessExponent:

        int expSign = 1;
        int exp = 0;

        for (++i; i < end; ++i)
        {
            c = s[i];
            if (c > 57 || c < 48)
            {
                if (c == '-')
                {
                    expSign = -1;
                    continue;
                }
            }

            exp = 10 * exp + (c - 48);
        }

        exponent = _floatExpLookup[exp] * expSign;

        goto ProcessFraction;
    }

    ///Returns were it stopped in string s or -1 if found no float to parse
    public static int ParseFloatFast(string s, int begin, int end, out float result)
    {
        result = 0.0f;
        char c = s[begin];
        int sign = 0;
        int start = begin;

        if (c == '-')
        {
            sign = -1;
            start = begin + 1;
        }
        else if (c > 57 || c < 48)
        {
            if (IgnoreChar(c))
            {
                do
                {
                    ++start;
                }
                while (start < end && IgnoreChar(c = s[start]));

                if (start >= end)
                {
                    return -1;
                }

                if (c == '-')
                {
                    sign = -1;
                    ++start;
                }
                else
                {
                    sign = 1;
                }
            }
            else
            {
                result = 0;
                return -1;
            }
        }
        else
        {
            start = begin + 1;
            result = 10 * result + (c - 48);
            sign = 1;
        }

        int i = start;

        for (; i < end; ++i)
        {
            c = s[i];
            if (c > 57 || c < 48)
            {
                if (c == '.')
                {
                    ++i;
                    goto DecimalPoint;
                }
                else
                {
                    result = 0;
                    return -1;
                }
            }

            result = 10 * result + (c - 48);
        }

        result *= sign;
        return i;

        DecimalPoint:

        long temp = 0;
        int length = i;
        float exponent = 0;

        for (; i < end; ++i)
        {
            c = s[i];
            if (c > 57 || c < 48)
            {
                if (!IgnoreChar(c))
                {
                    if (c == 'e' || c == 'E')
                    {
                        length = i - length;
                        goto ProcessExponent;
                    }

                    result = 0;
                    return -1;
                }
                else
                {
                    length = i - length;
                    goto ProcessFraction;
                }
            }
            temp = 10 * temp + (c - 48);
        }
        length = i - length;

        ProcessFraction:

        float fraction = (float)temp;

        if (length < _powLookup.Length)
        {
            fraction = fraction / _powLookup[length];
        }
        else
        {
            fraction = fraction / _powLookup[_powLookup.Length - 1];
        }

        result += fraction;

        result *= sign;

        if (exponent > 0)
        {
            result *= exponent;
        }
        else if (exponent < 0)
        {
            result /= -exponent;
        }

        return i;

        ProcessExponent:

        int expSign = 1;
        int exp = 0;

        for (++i; i < end; ++i)
        {
            c = s[i];
            if (c > 57 || c < 48)
            {
                if (c == '-')
                {
                    expSign = -1;
                    continue;
                }
            }

            exp = 10 * exp + (c - 48);
        }

        exponent = _floatExpLookup[exp] * expSign;

        goto ProcessFraction;
    }
    private static readonly long[] _powLookup = new[]
    {
        1, // 10^0
        10, // 10^1
        100, // 10^2
        1000, // 10^3
        10000, // 10^4
        100000, // 10^5
        1000000, // 10^6
        10000000, // 10^7
        100000000, // 10^8
        1000000000, // 10^9,
        10000000000, // 10^10,
        100000000000, // 10^11,
        1000000000000, // 10^12,
        10000000000000, // 10^13,
        100000000000000, // 10^14,
        1000000000000000, // 10^15,
        10000000000000000, // 10^16,
        100000000000000000, // 10^17,
    };

    private static readonly float[] _floatExpLookup = GetFloatExponents();

    private static float[] GetFloatExponents()
    {
        var max = 309;

        var exps = new float[max];

        for (var i = 0; i < max; i++)
        {
            exps[i] = Mathf.Pow(10, i);
        }

        return exps;
    }


}
}
