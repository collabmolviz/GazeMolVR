using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using UnityEngine;
//using UnityGLTF;

namespace UMol {
public class GLTFExporter {

    public static void DoExport(List<GameObject> gos, string folderPath, string fileName) {

        Transform[] arrT = new Transform[gos.Count];
        int i = 0;
        foreach (GameObject g in gos){
            arrT[i] = g.transform;
            i++;
        }
        // var exporter = new GLTFSceneExporter(arrT, null);
        // exporter.SaveGLTFandBin(folderPath, fileName);
    }
}
}