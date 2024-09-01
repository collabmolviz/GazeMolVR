using UnityEngine;
using System.Collections.Generic;
using System.IO;


//From VMD
/*
 * corresponding table of VDW radii.
 * van der Waals radii are taken from A. Bondi,
 * J. Phys. Chem., 68, 441 - 452, 1964,
 * except the value for H, which is taken from R.S. Rowland & R. Taylor,
 * J.Phys.Chem., 100, 7384 - 7391, 1996. Radii that are not available in
 * either of these publications have RvdW = 2.00 Å.
 * The radii for Ions (Na, K, Cl, Ca, Mg, and Cs are based on the CHARMM27
 * Rmin/2 parameters for (SOD, POT, CLA, CAL, MG, CES) by default.
 */

namespace UMol {

public class UnityMolDefaultColors {

    public Color oxygenColor     = new Color(0.827f, 0.294f, 0.333f, 1f);
    public Color carbonColor     = new Color(0.282f, 0.6f, 0.498f, 1f);
    public Color nitrogenColor   = new Color(0.443f, 0.662f, 0.882f, 1f);
    public Color hydrogenColor   = Color.white;
    public Color sulphurColor    = new Color(1f, 0.839f, 0.325f, 1f);
    public Color phosphorusColor = new Color(0.960f, 0.521f, 0.313f, 1f);
    public Color unknownColor    = new Color(1f, 0.4f, 1f, 1f);
    public Color ferrousColor    = new Color(0.875f, 0.398f, 0.199f, 1f);

    public static Color32 orange = new Color(1.0f, 0.5f, 0.31f, 1.0f);
    public static Color32 lightyellow = new Color(1.0f, 240 / 255.0f, 140 / 255.0f, 1.0f);

    public Dictionary<string, Color32> colorByAtom;
    public Dictionary<string, float> radiusByAtom;

    private List<Color32> colorPalette = new List<Color32>();
    public Dictionary<string, Color32> colorByResidue = new Dictionary<string, Color32>();

    /// Based on https://pymolwiki.org/index.php/Resicolor
    public Dictionary<string, Color32> colorRestypeByResidue = new Dictionary<string, Color32>  {
        {"ALA", lightyellow},
        {"ARG", Color.blue},
        {"ASN", Color.green},
        {"ASP", Color.red},
        {"CYS", orange},
        {"GLN", Color.green},
        {"GLU", Color.red},
        {"GLY", lightyellow},
        {"HIS", Color.green},
        {"ILE", lightyellow},
        {"LEU", lightyellow},
        {"LYS", Color.blue},
        {"MET", lightyellow},
        {"PHE", lightyellow},
        {"PRO", lightyellow},
        {"SEC", Color.green},
        {"SER", Color.green},
        {"THR", Color.green},
        {"TRP", lightyellow},
        {"TYR", Color.green},
        {"VAL", lightyellow}
    };
    /// Negative residues in red, blue for positively charged residues, others in white
    public Dictionary<string, Color> colorReschargeByResidue = new Dictionary<string, Color>  {
        {"ALA", Color.white},
        {"ARG", Color.blue},
        {"ASN", Color.white},
        {"ASP", Color.red},
        {"CYS", Color.white},
        {"GLN", Color.white},
        {"GLU", Color.red},
        {"GLY", Color.white},
        {"HIS", Color.blue},
        {"ILE", Color.white},
        {"LEU", Color.white},
        {"LYS", Color.blue},
        {"MET", Color.white},
        {"PHE", Color.white},
        {"PRO", Color.white},
        {"SEC", Color.white},
        {"SER", Color.white},
        {"THR", Color.white},
        {"TRP", Color.white},
        {"TYR", Color.white},
        {"VAL", Color.white}
    };


    public Object[] textures;

    public UnityMolDefaultColors(string pathColorR = null, string pathPalette = null, string pathMartiniDefault = null, string pathCustom = null) {
        if (pathColorR == null) {
            pathColorR = Path.Combine(Application.streamingAssetsPath , "defaultUnityMolColors.txt");
        }
        if (pathPalette == null) {
            pathPalette = Path.Combine(Application.streamingAssetsPath , "colorPalette.txt");
        }
        if (pathMartiniDefault == null) {
            pathMartiniDefault = Path.Combine(Application.streamingAssetsPath , "defaultMartiniUnityMolColors.txt");
        }
        if (pathCustom == null) {
            pathCustom = Path.Combine(Application.streamingAssetsPath , "customUnityMolColors.txt");
        }

        parseColorRadiusByAtom(pathColorR);
        parseColorRadiusByAtom(pathCustom);
        parseColorRadiusByAtomMartini(pathMartiniDefault);
        parseColorPaletteAndResidue(pathPalette);

        if (Application.platform != RuntimePlatform.Android) {
            textures = Resources.LoadAll("Images/MatCap", typeof(Texture2D));
        }
    }

    public void parseColorRadiusByAtom(string colorFilePath) {
        if (colorByAtom == null) {
            colorByAtom = new Dictionary<string, Color32>();
        }
        if (radiusByAtom == null) {
            radiusByAtom = new Dictionary<string, float>();
        }
        //Set default color even if the parsing fails
        colorByAtom["C"] = carbonColor;
        colorByAtom["O"] = oxygenColor;
        colorByAtom["N"] = nitrogenColor;
        colorByAtom["H"] = hydrogenColor;
        colorByAtom["S"] = sulphurColor;
        colorByAtom["P"] = phosphorusColor;
        colorByAtom["FE"] = ferrousColor;


        radiusByAtom["C"] =  1.70f;
        radiusByAtom["O"] =  1.52f;
        radiusByAtom["N"] =  1.55f;
        radiusByAtom["H"] =  1.20f;
        radiusByAtom["S"] =  1.80f;
        radiusByAtom["P"] =  1.80f;
        radiusByAtom["FE"] = 1.56f;

        StreamReader sr;


        if (Application.platform == RuntimePlatform.Android)
        {
            var textStream = new StringReaderStream(AndroidUtils.GetFileText(colorFilePath));
            sr = new StreamReader(textStream);
        }
        else
        {
            FileInfo LocalFile = new FileInfo(colorFilePath);
            if (!LocalFile.Exists) {
                // Debug.LogWarning("File not found: " + colorFilePath);
                return;
            }
            sr = new StreamReader(colorFilePath);
        }

        using(sr) {
            string line;
            int cptColorParsed = 0;
            while ((line = sr.ReadLine()) != null) {
                if (line.StartsWith("#") || line.Trim().Length < 3) {
                    continue;
                }
                try {
                    string[] splits = line.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    string atomType = splits[0].ToUpper();
                    Color atomCol = Color.white;
                    ColorUtility.TryParseHtmlString(splits[1], out atomCol);
                    float radius = float.Parse(splits[2], System.Globalization.CultureInfo.InvariantCulture);
                    colorByAtom[atomType] = (Color32) atomCol;
                    radiusByAtom[atomType] = radius;
                    cptColorParsed++;
                }
                catch {
                    Debug.LogWarning("Ignoring color/atom line " + line);
                }
            }
        }
    }

    public void parseColorRadiusByAtomMartini(string colorFilePath) {
        StreamReader sr;

        if (Application.platform == RuntimePlatform.Android)
        {
            var textStream = new StringReaderStream(AndroidUtils.GetFileText(colorFilePath));
            sr = new StreamReader(textStream);
        }
        else
        {
            FileInfo LocalFile = new FileInfo(colorFilePath);
            if (!LocalFile.Exists)
            {
                Debug.LogWarning("File not found: " + colorFilePath);
                return;
            }
            sr = new StreamReader(colorFilePath);
        }
        using (sr) {

            string s;
            int cptline = 0;
            while ((s = sr.ReadLine()) != null) {

                cptline++;
                if (s.Length > 1 && !s.StartsWith("#")) {
                    try {

                        string[] fields = s.Split(new [] {' '});
                        if (fields.Length != 6)
                            continue;

                        Color curCol = new Color(float.Parse(fields[2], System.Globalization.CultureInfo.InvariantCulture),
                                                 float.Parse(fields[3], System.Globalization.CultureInfo.InvariantCulture),
                                                 float.Parse(fields[4], System.Globalization.CultureInfo.InvariantCulture), 1.0f);

                        string resCGname = fields[0].Trim();
                        string CGname = fields[1].Trim();
                        float radius = float.Parse(fields[5], System.Globalization.CultureInfo.InvariantCulture);

                        string atomType = "MARTINI_" + resCGname + "_" + CGname;
                        colorByAtom[atomType.ToUpper()] = curCol;
                        radiusByAtom[atomType.ToUpper()] = radius;

                    }
                    catch  {
                        Debug.LogWarning("Ignoring line " + cptline);
                    }
                }
            }
        }
    }

    //Expects atomType uppercase
    public void getColorAtom(string atomType, out Color32 color, out float radius) {
        color = (Color32) unknownColor;
        radius = 1.0f;
        bool found = false;
        if (colorByAtom != null && colorByAtom.TryGetValue(atomType, out color)) {
            found = true;
        }
        if (radiusByAtom != null && radiusByAtom.TryGetValue(atomType, out radius)) {
            found = true;
        }
        if (!found) {
            color = (Color32) unknownColor;
            radius = 2.0f;
#if UNITY_EDITOR
            // Debug.LogWarning("Unknown atom " + atomType);
#endif
        }

    }
    public bool isKnownAtom(string atomType) {
        return radiusByAtom.ContainsKey(atomType);
    }

    public float getMaxRadius(List<UnityMolAtom> atoms) {
        if (radiusByAtom == null) {
            return -1.0f;
        }
        float maxVDW = 0.0f;

        foreach (UnityMolAtom a in atoms) {
            if (radiusByAtom.ContainsKey(a.type)) {
                maxVDW = Mathf.Max(maxVDW, radiusByAtom[a.type]);
            }
        }
        return maxVDW;
    }

    void parseColorPaletteAndResidue(string path) {
        StreamReader sr;

        if (Application.platform == RuntimePlatform.Android)
        {
            var textStream = new StringReaderStream(AndroidUtils.GetFileText(path));
            sr = new StreamReader(textStream);
        }
        else
        {
            FileInfo LocalFile = new FileInfo(path);
            if (!LocalFile.Exists)
            {
                Debug.LogWarning("File not found: " + path);
                return;
            }
            sr = new StreamReader(path);
        }
        colorPalette.Clear();

        using(sr) {
            string line;
            // int cptColorParsed = 0;
            while ((line = sr.ReadLine()) != null) {
                if (line.StartsWith("#") || line.Trim().Length < 3) {
                    continue;
                }
                if (line.StartsWith("-")) {
                    try {
                        string[] splits = line.Split(new [] { '\t', ' ', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
                        Color newCol = Color.white;
                        ColorUtility.TryParseHtmlString(splits[0], out newCol);
                        colorPalette.Add(newCol);
                    }
                    catch {
                        Debug.LogWarning("Ignoring line " + line);
                    }
                }
                else {

                    try {
                        string[] splits = line.Split(new [] { '\t', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        string resType = splits[0].ToUpper();
                        Color resCol = Color.white;
                        ColorUtility.TryParseHtmlString(splits[1], out resCol);
                        colorByResidue[resType] = resCol;
                    }
                    catch {
                        Debug.LogWarning("Ignoring line " + line);
                    }
                }
            }
        }
        // Random.State oldState = Random.state;

        // //Initialize the seed for random color to have the same color for residues all the time
        // Random.InitState(123);

        // float pos = 0.0f;
        // foreach(string resName in UnityMolMain.topologies.bondedAtomsPerResidue.Keys){
        //     colorByResidue[resName] = getRandomColor();
        // }

        // Random.state = oldState;
    }

    public Color getColorFromPalette(int id) {
        if (colorPalette.Count == 0) {
            return getRandomColor();
        }
        if (id < 0)
            id = 0;

        if (id >= colorPalette.Count) {
            id = id % colorPalette.Count;
        }
        return colorPalette[id];
    }

    public Color getColorForResidue(UnityMolResidue res) {
        if (colorByResidue.ContainsKey(res.name)) {
            return colorByResidue[res.name];
        }
        Color newCol = getRandomColor();
        colorByResidue[res.name] = newCol;
        return newCol;
    }

    public Color getColorRestypeForResidue(UnityMolResidue res) {
        if (colorRestypeByResidue.ContainsKey(res.name)) {
            return colorRestypeByResidue[res.name];
        }
        Color newCol = getRandomColor();
        colorRestypeByResidue[res.name] = newCol;
        return newCol;
    }
    public Color getColorReschargeForResidue(UnityMolResidue res) {
        if (colorReschargeByResidue.ContainsKey(res.name)) {
            return colorReschargeByResidue[res.name];
        }
        Color newCol = Color.black;
        colorReschargeByResidue[res.name] = newCol;
        return newCol;
    }


    private Color getRandomColor() {
        const float goldenRatio = 0.618033988749895f;
        Color rndCol = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

        float H, S, V;
        Color.RGBToHSV(rndCol, out H, out S, out V);
        H = (H + goldenRatio) % 1.0f;
        return Color.HSVToRGB(H, S, V);
    }
}
}