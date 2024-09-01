using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System;

using UMol.API;

namespace UMol {
public class meshlabAO {

    public bool _isMeshlabRunning;
    public GameObject attachedGo;
    private Thread _Meshlabthread;

    private Mesh curMesh;
    private Vector3[] positions;
    private Vector3[] normals;
    private int[] triangles;

    private string tempPath;
    private string tempOutPath;
    private static int idMeshLab = 0;

    public bool success = false;
    public List<float> aoResult = null;

    public static string BinaryPath
    {
        get {
            var basePath = Application.streamingAssetsPath + "/MeshLab";

            if (Application.platform == RuntimePlatform.OSXPlayer ||
                    Application.platform == RuntimePlatform.OSXEditor)
                return basePath + "/OSX/meshlabserver";

            if (Application.platform == RuntimePlatform.LinuxPlayer ||
                    Application.platform == RuntimePlatform.LinuxEditor)
                return basePath + "/Linux/meshlabserver";

            return basePath + "/Windows/meshlabserver.exe";
        }
    }

    public void StartThread(Mesh m) {
        success = false;
        if ( System.IO.File.Exists(BinaryPath))
            curMesh = m;
        tempPath = Path.Combine(Application.temporaryCachePath, "tmpMeshlab_" + idMeshLab + ".obj").Replace("\\", "/");;
        tempOutPath = Path.Combine(Application.temporaryCachePath, "meshlabout_" + idMeshLab + ".obj").Replace("\\", "/");;
        idMeshLab++;
        UnityEngine.Debug.Log("Starting thread " + tempPath + " / " + tempOutPath);
        positions = curMesh.vertices;
        triangles = curMesh.triangles;
        normals = curMesh.normals;

        writeToOBJ(tempPath);

        _Meshlabthread = new Thread(CompteAOMeshlab);
        _Meshlabthread.Start();
    }

    public void StartThread(List<GameObject> gos) {
        success = false;

        tempPath = Path.Combine(Application.temporaryCachePath, "tmpMeshlab_" + idMeshLab + ".obj").Replace("\\", "/");;
        tempOutPath = Path.Combine(Application.temporaryCachePath, "meshlabout_" + idMeshLab + ".obj").Replace("\\", "/");;
        idMeshLab++;

        CombineInstance[] combine = new CombineInstance[gos.Count];
        int id = 0;
        foreach (GameObject go in gos) {
            Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
            combine[id].mesh = m;
            combine[id].transform = go.transform.localToWorldMatrix;
            id++;
        }
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);


        // List<Vector3> tmpPos = new List<Vector3>();
        // List<Vector3> tmpNorm = new List<Vector3>();
        // List<int> tmpTri = new List<int>();
        // int countTri = 0;
        // int idM = 0;
        // foreach (GameObject go in gos) {
        //     Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
        //     Vector3[] v = m.vertices;
        //     Vector3[] n = m.normals;
        //     int[] t = m.triangles;
        //     for (int i = 0; i < v.Length; i++) {
        //         tmpPos.Add(v[i]);
        //         tmpNorm.Add(n[i]);
        //     }
        //     for (int i = 0; i < t.Length; i++) {
        //         tmpTri.Add(t[i] + countTri);
        //     }
        //     countTri += t.Length;
        //     idM++;
        //     if (idM == 2) {break;}
        // }

        positions = combinedMesh.vertices;//tmpPos.ToArray();
        normals = combinedMesh.normals;//tmpNorm.ToArray();
        triangles = combinedMesh.triangles;//tmpTri.ToArray();

        writeToOBJ(tempPath);

        _Meshlabthread = new Thread(CompteAOMeshlab);
        _Meshlabthread.Start();
    }

    void CompteAOMeshlab() {
        _isMeshlabRunning = true;
        //Write mesh to obj file


        //Run meshlab to compute AO
        string scriptPath = Path.Combine(Application.streamingAssetsPath, "aoMesh.mlx").Replace("\\", "/");
        string opt = "-i " + tempPath + " -o " + tempOutPath + " -m vc -s " + scriptPath;

        UnityEngine.Debug.Log("Starting => " + BinaryPath + " \n avec : " + opt);
        ProcessStartInfo info = new ProcessStartInfo(BinaryPath, opt);
        info.UseShellExecute = true;
        info.CreateNoWindow = true;
        info.WorkingDirectory = Path.GetDirectoryName(BinaryPath).Replace("\\", "/");
        // info.RedirectStandardInput = true;
        // info.RedirectStandardOutput = true;
        // info.RedirectStandardError = true;

        Process _subprocess = Process.Start(info);

        // string Error = _subprocess.StandardError.ReadToEnd();
        // string log = _subprocess.StandardOutput.ReadToEnd();
        // UnityEngine.Debug.Log(log);
        // UnityEngine.Debug.LogError(Error);
        // int ExitCode = _subprocess.ExitCode;

        // _subprocess.StandardInput.Close();
        _subprocess.WaitForExit();

        try {
            aoResult = getAOFromFile(tempOutPath);
            success = true;
        }
        catch {
            aoResult = null;
            success = false;
        }
        _isMeshlabRunning = false;

    }
    public void Clear() {
        if (_isMeshlabRunning) {

            // Force thread to quit
            _isMeshlabRunning = false;

            // wait for thread to finish and clean up
            _Meshlabthread.Abort();
        }
    }

    void writeToOBJ(string p) {
        using (StreamWriter writer = new StreamWriter(p, false)) {
            int N = positions.Length;
            for (int i = 0; i < N; i++) {
                writer.Write("v " + (-positions[i].x).ToString("f6", System.Globalization.CultureInfo.InvariantCulture)
                             + " " + positions[i].y.ToString("f6", System.Globalization.CultureInfo.InvariantCulture)
                             + " " + positions[i].z.ToString("f6", System.Globalization.CultureInfo.InvariantCulture) + "\n");
            }
            for (int i = 0; i < N; i++) {
                writer.Write("vn " + (-normals[i].x).ToString("f4", System.Globalization.CultureInfo.InvariantCulture)
                             + " " + normals[i].y.ToString("f4", System.Globalization.CultureInfo.InvariantCulture)
                             + " " + normals[i].z.ToString("f4", System.Globalization.CultureInfo.InvariantCulture) + "\n");
            }
            for (int i = 0; i < triangles.Length - 3; i += 3) {
                writer.Write("f " + (triangles[i + 1] + 1) + " " + (triangles[i] + 1) + " " + (triangles[i + 2] + 1) + "\n");
            }
            writer.Close();
        }
    }

    List<float> getAOFromFile(string p) {
        StreamReader sr = new StreamReader(p);
        List<float> aos = new List<float>();
        using (sr) {

            string line = "";
            while ((line = sr.ReadLine()) != null) {
                if (line.StartsWith("v ")) {
                    string[] spl = line.Split(new [] { ' '}, System.StringSplitOptions.RemoveEmptyEntries);
                    int len = spl.Length;
                    float ao = float.Parse(spl[len - 1], System.Globalization.CultureInfo.InvariantCulture);
                    aos.Add(ao);
                }
            }
        }
        UnityEngine.Debug.Log("Read => " + aos.Count + " ao values");
        return aos;
    }
}


}
