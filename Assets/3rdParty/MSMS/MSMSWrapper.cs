using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System;

namespace UMol {
public class MSMSWrapper {

    public static int idMSMS = 0;
    public static string BinaryPath
    {
        get {
            var basePath = Application.streamingAssetsPath + "/MSMS";

            if (Application.platform == RuntimePlatform.OSXPlayer ||
                    Application.platform == RuntimePlatform.OSXEditor)
                return basePath + "/OSX/msms";

            if (Application.platform == RuntimePlatform.LinuxPlayer ||
                    Application.platform == RuntimePlatform.LinuxEditor)
                return basePath + "/Linux/msms";

            return basePath + "/Windows/msms.exe";
        }
    }

    static string toXYZR(UnityMolSelection sel, int idF, bool withHET = false) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < sel.atoms.Count; i++) {
            UnityMolAtom a = sel.atoms[i];
            bool isWater = WaterSelection.waterResidues.Contains(a.residue.name, StringComparer.OrdinalIgnoreCase);
            if ((!a.isHET && !isWater) || withHET) {
                if (idF != -1) {
                    Vector3 p = sel.extractTrajFramePositions[idF][i];
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} ", -p.x, p.y, p.z);
                }
                else {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} ", -a.position.x, a.position.y, a.position.z);
                }
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0:F2}\n", a.radius);
            }
        }
        return sb.ToString();
    }

    //Calls MSMS executable to create meshes
    public static void callMSMS(ref MeshData mData, int idF, UnityMolSelection sel, float density, float probeRad, string tempPath) {
        string binPath = BinaryPath;

        long time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        string tmpFileName = "tmpmsms_" + idMSMS.ToString() + "_" + time + "_" + sel.name;
        idMSMS++;
        string path = tempPath + "/" + tmpFileName + ".xyzr";
        string pdbLines = toXYZR(sel, idF);
        if (pdbLines.Length == 0) {
            pdbLines = toXYZR(sel, idF, true);
        }

        if (pdbLines.Length == 0) {
            return;
        }
        StreamWriter writer = new StreamWriter(path, false);
        writer.WriteLine(pdbLines);
        writer.Close();

        string opt = "-probe_radius " + probeRad.ToString("f2", CultureInfo.InvariantCulture)
                     + " -no_area -density " + density.ToString("f2", CultureInfo.InvariantCulture)
                     + " -if \"" + path + "\" -of \"" + tempPath + "/" + tmpFileName + "\"";
        ProcessStartInfo info = new ProcessStartInfo(binPath, opt);
        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.RedirectStandardInput = true;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;

        Process _subprocess = Process.Start(info);

        // string Error = _subprocess.StandardError.ReadToEnd();
        string log = _subprocess.StandardOutput.ReadToEnd();
        // UnityEngine.Debug.Log(log);
        // UnityEngine.Debug.LogError(Error);
        // int ExitCode = _subprocess.ExitCode;

        // _subprocess.StandardInput.Close();
        _subprocess.WaitForExit();


        if (log == null || !log.Contains("MSMS terminated normally")) {
            UnityEngine.Debug.LogError("MSMS failed : " + log);
            return;
        }


        string pathV = tempPath + "/" + tmpFileName + ".vert";
        string pathF = tempPath + "/" + tmpFileName + ".face";

        int Nvert = getCountFromHeader(pathV);
        int Ntri = getCountFromHeader(pathF) * 3;


        if (mData == null) {
            mData = new MeshData();
            mData.vertices = new Vector3[Nvert];
            mData.triangles = new int[Ntri];
            mData.colors = new Color32[Nvert];
            mData.normals = new Vector3[Nvert];
            mData.atomByVert = new int[Nvert];
        }
        else {
            if (Nvert > mData.vertices.Length) {//We need more space
                mData.vertices = new Vector3[Nvert];
                mData.colors = new Color32[Nvert];
                mData.normals = new Vector3[Nvert];
                mData.atomByVert = new int[Nvert];
                mData.triangles = new int[Ntri];
            }
        }
        mData.nVert = Nvert;
        mData.nTri = Ntri / 3;


        parseVerticesNormals(pathV, ref mData);
        parseTriangles(pathF, ref mData.triangles);

        mData.FillWhite();

        // MeshSmoother.smoothMeshLaplacian(m.vertices, m.triangles, 1);

        // UnityEngine.Debug.Log(pathV);
        File.Delete(pathV);
        File.Delete(pathF);
        File.Delete(path);
    }

    static void parseVerticesNormals(string path, ref MeshData mData) {
        if (!File.Exists(path)) {
            UnityEngine.Debug.LogError("MSMS failed");
            return;
        }
        StreamReader sr = new StreamReader(path);

        Vector3 cur = Vector3.zero;
        Vector3 curn = Vector3.zero;
        float[] buff = new float[6];

        using (sr) {

            string line;

            line = sr.ReadLine();
            line = sr.ReadLine();
            line = sr.ReadLine();

            int id = 0;
            while ((line = sr.ReadLine()) != null) {

                int stopped = Reader.ParseFloats(6, line, ref buff);
                int end = 0;
                int dummy = Reader.ParseInt(ref end, line, stopped);
                int idV = Reader.ParseInt(line, end) - 1;

                mData.atomByVert[id] = idV;
                mData.vertices[id] = new Vector3(-buff[0], buff[1], buff[2]);
                mData.normals[id] = new Vector3(-buff[3], buff[4], buff[5]);
                id++;
            }
        }
    }

    static void parseTriangles(string path, ref int[] tris) {
        StreamReader sr = new StreamReader(path);

        using (sr) {

            string line;
            line = sr.ReadLine();
            line = sr.ReadLine();
            line = sr.ReadLine();

            int id = 0;
            while ((line = sr.ReadLine()) != null) {
                int newStart = 0;
                int newStart2 = 0;
                int t1, t2, t3;
                t1 = Reader.ParseInt(ref newStart, line, 0) - 1;
                t3 = Reader.ParseInt(ref newStart2, line, newStart) - 1;
                t2 = Reader.ParseInt(ref newStart, line, newStart2) - 1;
                tris[id++] = t1;
                tris[id++] = t2;
                tris[id++] = t3;
            }
        }
    }


    static int getCountFromHeader(string path) {
        StreamReader sr = new StreamReader(path);
        int N = 0;
        using (sr) { // Don't use garbage collection but free temp memory after reading the pdb file
            string line;
            line = sr.ReadLine();
            line = sr.ReadLine();
            line = sr.ReadLine();

            //Should contain number of verts
            N = Reader.ParseInt(line);
        }
        return N;
    }


    public static void createMSMSSurface(int idF, Transform meshPar, string name, UnityMolSelection select,
                                         ref MeshData meshD, float density = 15.0f, float probeRad = 1.4f) {

        callMSMS(ref meshD, idF, select, density, probeRad, Application.temporaryCachePath);
    }
}
}
