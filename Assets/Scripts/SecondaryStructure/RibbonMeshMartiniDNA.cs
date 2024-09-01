
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UMol {
public class RibbonMeshMartiniDNA {



    public static int splineSteps =  4;
    public static int profileDetail = 4;

    public static int trajSplineSteps = 4;
    public static int trajProfileDetail = 4;

    public static float ribbonWidth = 2.0f;
    public static float ribbonHeight = 0.125f;
    public static float ribbonOffset = 0.5f;
    public static float arrowHeadWidth = 3.0f;
    public static float arrowWidth = 2.0f;
    public static float arrowHeight = 0.5f;
    public static float tubeSize = 0.25f;

    public static float cylinderSize = 0.35f;

    public static bool testDistanceBB_BB = true;


    public static string baseACol = "#D81B60";//Colorblind ready
    public static string baseCCol = "#1E88E5";
    public static string baseGCol = "#FFC107";
    public static string baseTCol = "#004D40";

    public static string strandCol = "#1a237e";

    public static string Cname = "BB1";
    public static string Cname2 = "BB2";
    public static string Oname = "SC1";

    private static float[] powersOfTen = {1e0f, 1e1f, 1e2f, 1e3f, 1e4f, 1e5f, 1e6f,
                                          1e7f, 1e8f, 1e9f, 1e10f, 1e11f, 1e12f, 1e13f, 1e14f, 1e15f, 1e16f
                                         };

    static Vector3[] ellipseProfile(int n, float w, float h) {
        Vector3[] result = new Vector3[n];
        for (int i = 0; i < n; i++) {
            float t = i / (float)n;
            float a = t * 2.0f * Mathf.PI + Mathf.PI / 4.0f;
            float x = Mathf.Cos(a) * w / 2.0f;
            float y = Mathf.Sin(a) * h / 2.0f;
            result[i] = new Vector3(x, y, 0.0f);
        }
        return result;
    }


    static Vector3[] rectangleProfile(int n, float w, float h) {

        Vector3[] result = new Vector3[n];
        float hw = w / 2.0f;
        float hh = h / 2.0f;
        Vector3[,] segments = new Vector3[4, 2];
        segments[0, 0] = new Vector3(hw,   hh, 0.0f);
        segments[0, 1] = new Vector3(-hw,  hh, 0.0f);

        segments[1, 0] = new Vector3(-hw,  hh, 0.0f);
        segments[1, 1] = new Vector3(-hw, -hh, 0.0f);

        segments[2, 0] = new Vector3(-hw, -hh, 0.0f);
        segments[2, 1] = new Vector3(hw,  -hh, 0.0f);

        segments[3, 0] = new Vector3(hw,  -hh, 0.0f);
        segments[3, 1] = new Vector3(hw,   hh, 0.0f);

        int m = n / 4;
        int cpt = 0;
        for (int a = 0; a < 4; a++) {
            for (int i = 0; i < m; i++) {
                float t = (float)i / (float)m;
                Vector3 p = Vector3.Lerp(segments[a, 0], segments[a, 1], t);
                result[cpt++] = p;

            }
        }
        return result;
    }


    static void segmentProfiles(MartiniDNAPlane pp1, MartiniDNAPlane pp2, int n,
                                ref Vector3[] p1, ref Vector3[] p2) {

        float offset1 = ribbonOffset;
        float offset2 = ribbonOffset;

        if (pp1.flipped) {
            offset1 = -offset1;
        }
        if (pp2.flipped) {
            offset2 = -offset2;
        }


        p1 = rectangleProfile(n, arrowWidth, arrowHeight);
        p2 = rectangleProfile(n, arrowWidth, arrowHeight);

    }

    static void segmentColors(MartiniDNAPlane pp, ref Color32 c1, ref Color32 c2) {

        Color col1;
        Color col2;

        ColorUtility.TryParseHtmlString(strandCol, out col1);
        ColorUtility.TryParseHtmlString(strandCol, out col2);

        c1 = col1;
        c2 = col2;
    }



    static void createSegmentMesh(int i, int n, MartiniDNAPlane pp1, MartiniDNAPlane pp2, MartiniDNAPlane pp3, MartiniDNAPlane pp4,
                                  ref List<Vector3> verticesList, ref List<int> trianglesList, ref List<Color32> colorsList,
                                  ref Dictionary<UnityMolResidue, List<int>> residueToVert, ref Dictionary<Vector3, int> verticesDict,
                                  ref HashSet<UnityMolResidue> doneRes, bool isTraj, bool drawBases,
                                  int idF, UnityMolSelection sel) {

        UnityMolResidue.secondaryStructureType type1 = UnityMolResidue.secondaryStructureType.Strand;
        UnityMolResidue.secondaryStructureType type2 = UnityMolResidue.secondaryStructureType.Strand;

        Color32 c1 = Color.black;
        Color32 c2 = Color.black;
        segmentColors(pp2, ref c1, ref c2);

        Vector3[] profile1 = null;
        Vector3[] profile2 = null;

        int pdetail = profileDetail;
        int ssteps = splineSteps;

        segmentProfiles(pp2, pp3, pdetail, ref profile1, ref profile2);

        int linearQuadOutcircOrIncirc = 0;//0 linear / 1 Quad / 2 Out Circ / 3 In Circ

        // if ( !(type1 == UnityMolResidue.secondaryStructureType.Strand && type2 != UnityMolResidue.secondaryStructureType.Strand)) {
        //     linearQuadOutcircOrIncirc = 1;
        // }
        // if (type0 == UnityMolResidue.secondaryStructureType.Strand && type1 != UnityMolResidue.secondaryStructureType.Strand) {
        //     linearQuadOutcircOrIncirc = 2;
        // }
        // if type1 != pdb.ResidueTypeStrand && type2 == pdb.ResidueTypeStrand {
        //  easeFunc = ease.InOutSquare
        // }

        // if (i == 0) {
        //     profile1 = ellipseProfile(pdetail, arrowWidth, arrowWidth);
        //     linearQuadOutcircOrIncirc = 2;
        // }
        // else if (i == n - 1) {
        //     profile2 = ellipseProfile(pdetail, arrowWidth, arrowWidth);
        //     linearQuadOutcircOrIncirc = 2;
        // }
        List<Vector3[]> splines1 = new List<Vector3[]>(profile1.Length);
        List<Vector3[]> splines2 = new List<Vector3[]>(profile2.Length);

        for (int a = 0; a < profile1.Length; a++) {
            Vector3 p1 = profile1[a];
            Vector3 p2 = profile2[a];

            Vector3[] sp1 = splineForPlanes(pp1, pp2, pp3, pp4, ssteps, p1.x, p1.y);
            Vector3[] sp2 = splineForPlanes(pp1, pp2, pp3, pp4, ssteps, p2.x, p2.y);

            splines1.Add(sp1);
            splines2.Add(sp2);

        }

        int startV = Mathf.Max(verticesList.Count - 1, 0);


        for (int a = 0; a < ssteps; a++) {

            float t0 = easeFunc( ((float)a) / ssteps, linearQuadOutcircOrIncirc);
            float t1 = easeFunc( ((float)(a + 1)) / ssteps, linearQuadOutcircOrIncirc);

            if (a == 0 && type1 == UnityMolResidue.secondaryStructureType.Strand
                    && type2 != UnityMolResidue.secondaryStructureType.Strand ) {

                Vector3 p00 = splines1[0][a];
                Vector3 p10 = splines1[pdetail / 4][a];
                Vector3 p11 = splines1[2 * pdetail / 4][a];
                Vector3 p01 = splines1[3 * pdetail / 4][a];
                triangulateQuad(p00, p01, p11, p10,
                                c1, c1, c1, c1,
                                ref verticesList, ref colorsList, ref trianglesList, ref verticesDict);
            }
            for (int j = 0; j < pdetail; j++) {
                Vector3 p100 = splines1[j][a];
                Vector3 p101 = splines1[j][a + 1];
                Vector3 p110 = splines1[(j + 1) % pdetail][a];
                Vector3 p111 = splines1[(j + 1) % pdetail][a + 1];
                Vector3 p200 = splines2[j][a];
                Vector3 p201 = splines2[j][a + 1];
                Vector3 p210 = splines2[(j + 1) % pdetail][a];
                Vector3 p211 = splines2[(j + 1) % pdetail][a + 1];

                Vector3 p00 = Vector3.Lerp(p100, p200, t0);
                Vector3 p01 = Vector3.Lerp(p101, p201, t1);
                Vector3 p10 = Vector3.Lerp(p110, p210, t0);
                Vector3 p11 = Vector3.Lerp(p111, p211, t1);

                Color32 c00 = Color32.Lerp(c1, c2, t0);
                Color32 c01 = Color32.Lerp(c1, c2, t1);
                Color32 c10 = Color32.Lerp(c1, c2, t0);

                Color32 c11 = Color32.Lerp(c1, c2, t1);
                triangulateQuad(p10, p11, p01, p00,
                                c10, c11, c01, c00,
                                ref verticesList, ref colorsList, ref trianglesList, ref verticesDict);
            }
        }


        if (drawBases) {
            UnityMolResidue r = pp1.r3;

            if (!doneRes.Contains(r)) {
                Color colCyl = Color.white;


                //Create a cylinder for each base
                Vector3 ori = Vector3.zero;
                Vector3 end = Vector3.zero;
                bool knownDNA = false;
                if (r.name.Contains("C") || r.name.Contains("T")) {
                    if (r.atoms.ContainsKey(Cname) || r.atoms.ContainsKey(Cname2) && r.atoms.ContainsKey("SC3")) {
                        knownDNA = true;
                        if (r.atoms.ContainsKey(Cname)) {
                            ori = r.atoms[Cname].position;
                            if (idF != -1) {
                                int ida = sel.atomToIdInSel[r.atoms[Cname]];
                                ori = sel.extractTrajFramePositions[idF][ida];
                            }
                        }
                        else {
                            ori = r.atoms[Cname2].position;
                            if (idF != -1) {
                                int ida = sel.atomToIdInSel[r.atoms[Cname2]];
                                ori = sel.extractTrajFramePositions[idF][ida];
                            }
                        }

                        end = r.atoms["SC3"].position;
                        if (idF != -1) {
                            int ida = sel.atomToIdInSel[r.atoms["SC3"]];
                            end = sel.extractTrajFramePositions[idF][ida];
                        }

                        if (r.name.Contains("C"))
                            ColorUtility.TryParseHtmlString(baseCCol, out colCyl);
                        else
                            ColorUtility.TryParseHtmlString(baseTCol, out colCyl);
                    }
                }
                else if (r.name.Contains("A") || r.name.Contains("G")) {
                    if (r.atoms.ContainsKey(Cname) || r.atoms.ContainsKey(Cname2) && r.atoms.ContainsKey("SC3")) {
                        knownDNA = true;
                        if (r.atoms.ContainsKey(Cname)) {
                            ori = r.atoms[Cname].position;
                            if (idF != -1) {
                                int ida = sel.atomToIdInSel[r.atoms[Cname]];
                                ori = sel.extractTrajFramePositions[idF][ida];
                            }
                        }
                        else {
                            ori = r.atoms[Cname2].position;
                            if (idF != -1) {
                                int ida = sel.atomToIdInSel[r.atoms[Cname2]];
                                ori = sel.extractTrajFramePositions[idF][ida];
                            }
                        }

                        end = r.atoms["SC3"].position;
                        if (idF != -1) {
                            int ida = sel.atomToIdInSel[r.atoms["SC3"]];
                            end = sel.extractTrajFramePositions[idF][ida];
                        }
                        if (r.name.Contains("A"))
                            ColorUtility.TryParseHtmlString(baseACol, out colCyl);
                        else
                            ColorUtility.TryParseHtmlString(baseGCol, out colCyl);
                    }
                }

                if (knownDNA) {
                    Vector3 dir = (end - ori).normalized;
                    int startT = verticesList.Count;
                    float dist = Vector3.Distance(ori, end);

                    MeshData tmpCyl = RibbonMeshDNA.createCapsule(dist, cylinderSize);

                    Quaternion rot = Quaternion.LookRotation(dir);
                    Vector3[] verts = tmpCyl.vertices;
                    foreach (Vector3 v in verts) {
                        //Rotate the cylinder toward the vector ori->end
                        Vector3 nv = v + ori;

                        Vector3 pivotAxis = Vector3.Cross(Vector3.up, dir);
                        float totalAngle = Vector3.SignedAngle(Vector3.up, dir, pivotAxis);
                        nv = ArcLine.RotatePointAroundPivot(ori, ori + pivotAxis, nv, totalAngle);

                        nv = nv + 0.5f * (end - ori);
                        verticesList.Add(nv);

                        colorsList.Add(colCyl);
                    }
                    foreach (int t in tmpCyl.triangles) {
                        trianglesList.Add(startT + t);
                    }
                }
                else {
                    Debug.LogWarning("Couldn't draw the cylinder for the DNA base : " + r.name);
                }

                doneRes.Add(r);
            }
        }

        List<int> listVertId = new List<int>();

        for (int sV = startV; sV < verticesList.Count; sV++) {
            listVertId.Add(sV);
        }

        RibbonMesh.AddVertToResidueDict(ref residueToVert, pp1.r3, listVertId);

    }
  

    public static MeshData createChainMesh(int idF, UnityMolSelection sel,
                                           List<UnityMolResidue> residues,
                                           ref Dictionary<UnityMolResidue, List<int>> residueToVert,
                                           bool isTraj = false, bool drawBases = true)  {



        MartiniDNAPlane[] planes = new MartiniDNAPlane[residues.Count + 3];
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color32> colors = new List<Color32>();
        HashSet<UnityMolResidue> doneRes = new HashSet<UnityMolResidue>();


        int nbPlane = 0;
        for (int i = -2; i <= residues.Count; i++) {
            int id = Mathf.Clamp(i, 0, residues.Count - 1);
            int id1 = Mathf.Clamp(i + 1, 0, residues.Count - 1);
            int id2 = Mathf.Clamp(i + 2, 0, residues.Count - 1);


            UnityMolResidue r1 = residues[id];
            UnityMolResidue r2 = residues[id1];
            UnityMolResidue r3 = residues[id2];
            MartiniDNAPlane plane = new MartiniDNAPlane(r1, r2, r3, idF, sel);

            if (plane == null || plane.r1 == null) {
                continue;
            }
            //Make sure to start at the first position
            if (i <= 0) {
                if (r1.atoms.ContainsKey(Cname)) {
                    plane.position = r1.atoms[Cname].position;
                    if (idF != -1) {
                        int ida = sel.atomToIdInSel[r1.atoms[Cname]];
                        plane.position = sel.extractTrajFramePositions[idF][ida];
                    }
                }
                else {
                    plane.position = r1.atoms[Cname2].position;
                    if (idF != -1) {
                        int ida = sel.atomToIdInSel[r1.atoms[Cname2]];
                        plane.position = sel.extractTrajFramePositions[idF][ida];
                    }
                }

                plane.position.x = -plane.position.x;
            }
            //Make sure to end at the last position
            if (i >= residues.Count - 2) {
                if (r3.atoms.ContainsKey(Cname)) {
                    plane.position = r3.atoms[Cname].position;
                    if (idF != -1) {
                        int ida = sel.atomToIdInSel[r3.atoms[Cname]];
                        plane.position = sel.extractTrajFramePositions[idF][ida];
                    }
                }
                else {
                    plane.position = r3.atoms[Cname2].position;
                    if (idF != -1) {
                        int ida = sel.atomToIdInSel[r3.atoms[Cname2]];
                        plane.position = sel.extractTrajFramePositions[idF][ida];
                    }
                }

                plane.position.x = -plane.position.x;
            }

            if (plane != null && plane.r1 != null) {
                // TODO: better handling missing required atoms
                planes[nbPlane++] = plane;
            }
        }
        Vector3 previous = Vector3.zero;

        for (int i = 0; i < nbPlane; i++) {
            MartiniDNAPlane p = planes[i];
            if (i > 0 && Vector3.Dot(p.side, previous) < 0.0f) {
                p.Flip();
            }
            previous = p.side;
        }

        Dictionary<Vector3, int> verticesDict = new Dictionary<Vector3, int>();
        int n = nbPlane - 3;

        MartiniDNAPlane pp1 = null;
        MartiniDNAPlane pp2 = null;
        MartiniDNAPlane pp3 = null;
        MartiniDNAPlane pp4 = null;

        for (int i = 0; i < n; i++) {
            pp1 = planes[i];
            pp2 = planes[i + 1];
            pp3 = planes[i + 2];
            pp4 = planes[i + 3];

            if (discontinuity(pp1, pp2, pp3, pp4)) {
                //Discontinuity
                continue;
            }
            if (testDistanceBB_BB) {
                if (!checkDistanceBB_BB(pp1, idF, sel)) {
                    continue;
                }
                if (!checkDistanceBB_BB(pp2, idF, sel)) {
                    continue;
                }
                if (!checkDistanceBB_BB(pp3, idF, sel)) {
                    continue;
                }
                if (!checkDistanceBB_BB(pp4, idF, sel)) {
                    continue;
                }
            }
            createSegmentMesh(i, n, pp1, pp2, pp3, pp4, ref vertices, ref triangles,
                              ref colors, ref residueToVert, ref verticesDict, ref doneRes, isTraj, drawBases,
                              idF, sel);

        }


        MeshData mesh = new MeshData();
        mesh.triangles = triangles.ToArray();
        mesh.vertices = vertices.ToArray();
        mesh.colors = colors.ToArray();

        return mesh;
    }


    // static int vertInVertexList(Vector3 v, List<Vector3> verticesList, int lookFor = 50){
    //  for(int i=verticesList.Count-1; i >= Mathf.Max(0,verticesList.Count - lookFor); i--){
    //      if(Mathf.Abs(verticesList[i].x - v.x) < 0.0001f &&
    //          Mathf.Abs(verticesList[i].y - v.y) < 0.0001f &&
    //          Mathf.Abs(verticesList[i].z - v.z) < 0.0001f ){
    //              return i;
    //      }
    //  }
    //  return -1;
    // }

    static void triangulateQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
                                Color32 c1, Color32 c2, Color32 c3, Color32 c4, ref List<Vector3> verticesList,
                                ref List<Color32> colorsList, ref List<int> trianglesList, ref Dictionary<Vector3, int> verticesDict) {

        const float tolerance = 1e-4f;
        p1.x = -p1.x;
        p2.x = -p2.x;
        p3.x = -p3.x;
        p4.x = -p4.x;

        p1.x = Mathf.Floor(p1.x / tolerance) * tolerance;
        p1.y = Mathf.Floor(p1.y / tolerance) * tolerance;
        p1.z = Mathf.Floor(p1.z / tolerance) * tolerance;

        p2.x = Mathf.Floor(p2.x / tolerance) * tolerance;
        p2.y = Mathf.Floor(p2.y / tolerance) * tolerance;
        p2.z = Mathf.Floor(p2.z / tolerance) * tolerance;

        p3.x = Mathf.Floor(p3.x / tolerance) * tolerance;
        p3.y = Mathf.Floor(p3.y / tolerance) * tolerance;
        p3.z = Mathf.Floor(p3.z / tolerance) * tolerance;

        p4.x = Mathf.Floor(p4.x / tolerance) * tolerance;
        p4.y = Mathf.Floor(p4.y / tolerance) * tolerance;
        p4.z = Mathf.Floor(p4.z / tolerance) * tolerance;

        //Version with unique vertices (2)
        int res1 = 0;
        int res2 = 0;
        int res3 = 0;
        int res4 = 0;

        int idp1 = res1;
        int idp2 = res2;
        int idp3 = res3;
        int idp4 = res4;

        if (verticesDict.TryGetValue(p1, out res1)) {
            idp1 = res1;
        }
        else {
            verticesList.Add(p1);
            idp1 = verticesList.Count - 1;
            colorsList.Add(c1);
            verticesDict[p1] = idp1;
        }

        if (verticesDict.TryGetValue(p2, out res2)) {
            idp2 = res2;
        }
        else {
            verticesList.Add(p2);
            idp2 = verticesList.Count - 1;
            colorsList.Add(c2);
            verticesDict[p2] = idp2;
        }

        if (verticesDict.TryGetValue(p3, out res3)) {
            idp3 = res3;
        }
        else {
            verticesList.Add(p3);
            idp3 = verticesList.Count - 1;
            colorsList.Add(c3);
            verticesDict[p3] = idp3;
        }

        if (verticesDict.TryGetValue(p4, out res4)) {
            idp4 = res4;
        }
        else {
            verticesList.Add(p4);
            idp4 = verticesList.Count - 1;
            colorsList.Add(c4);
            verticesDict[p4] = idp4;
        }

        trianglesList.Add(idp2);
        trianglesList.Add(idp1);
        trianglesList.Add(idp3);

        trianglesList.Add(idp3);
        trianglesList.Add(idp1);
        trianglesList.Add(idp4);

        // //Version with duplicate vertices
        // verticesList.Add(p1);
        // int idp1 = verticesList.Count - 1;
        // verticesList.Add(p2);
        // verticesList.Add(p3);
        // verticesList.Add(p4);

        // colorsList.Add(c1);
        // colorsList.Add(c2);
        // colorsList.Add(c3);
        // colorsList.Add(c4);

        // trianglesList.Add(idp1+1);
        // trianglesList.Add(idp1);
        // trianglesList.Add(idp1+2);

        // trianglesList.Add(idp1+2);
        // trianglesList.Add(idp1);
        // trianglesList.Add(idp1+3);
    }


    static Vector3[] splineForPlanes(MartiniDNAPlane p1, MartiniDNAPlane p2, MartiniDNAPlane p3, MartiniDNAPlane p4,
                                     int n, float u, float v) {
        Vector3 g1 = p1.position + ((p1.side * u) + (p1.normal * v));
        Vector3 g2 = p2.position + ((p2.side * u) + (p2.normal * v));
        Vector3 g3 = p3.position + ((p3.side * u) + (p3.normal * v));
        Vector3 g4 = p4.position + ((p4.side * u) + (p4.normal * v));
        return spline(g1, g2, g3, g4, n);
    }

    static float easeFunc(float t, int idEase) {
        float res = t;
        //0 linear / 1 Quad / 2 Out Circ / 3 In Circ
        switch (idEase) {
        case 0:
            return t;
        // break;
        case 1:
            if (t < 0.5f) {
                return 2.0f * t * t;
            }
            t = 2.0f * t - 1;
            return -0.5f * (t * (t - 2) - 1);
        // break;
        case 2:
            t -= 1.0f;
            return Mathf.Sqrt(1 - (t * t));
        // break;
        case 3:
            return -1 * (Mathf.Sqrt(1 - t * t) - 1);
        // break;
        default:
            break;
        }
        return res;
    }

    static Vector3[] spline(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4 , int n ) {

        float n2 = (n * n);
        float n3 = (n * n * n);

        Matrix4x4 s = new Matrix4x4();
        s.SetRow(0, new Vector4(6.0f / n3, 0.0f, 0.0f, 0.0f));
        s.SetRow(1, new Vector4(6.0f / n3, 2.0f / n2, 0.0f, 0.0f));
        s.SetRow(2, new Vector4(1.0f / n3, 1.0f / n2, 1.0f / n, 0.0f));
        s.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        Matrix4x4 b = new Matrix4x4();
        b.SetRow(0, new Vector4(-1.0f, 3.0f, -3.0f , 1.0f ) * 1.0f / 6.0f);
        b.SetRow(1, new Vector4(3.0f, -6.0f, 3.0f , 0.0f )  * 1.0f / 6.0f);
        b.SetRow(2, new Vector4(-3.0f, 0.0f, 3.0f, 0.0f)    * 1.0f / 6.0f);
        b.SetRow(3, new Vector4(1.0f, 4.0f, 1.0f, 0.0f)     * 1.0f / 6.0f);

        Matrix4x4 g = new Matrix4x4();
        g.SetRow(0, new Vector4(v1.x, v1.y, v1.z, 1.0f));
        g.SetRow(1, new Vector4(v2.x, v2.y, v2.z, 1.0f));
        g.SetRow(2, new Vector4(v3.x, v3.y, v3.z, 1.0f));
        g.SetRow(3, new Vector4(v4.x, v4.y, v4.z, 1.0f));

        Matrix4x4 m = s * b * g;
        // Matrix4x4 m = g * b * s ;

        Vector3[] result = new Vector3[n + 1];

        Vector3 v = new Vector3(m.m30 / m.m33, m.m31 / m.m33, m.m32 / m.m33 );

        v = RoundPlaces(v, 10);
        int id = 0;
        result[id] = v;
        id++;
        for (int k = 0; k < n; k++) {
            m.m30 = m.m30 + m.m20;
            m.m31 = m.m31 + m.m21;
            m.m32 = m.m32 + m.m22;
            m.m33 = m.m33 + m.m23;
            m.m20 = m.m20 + m.m10;
            m.m21 = m.m21 + m.m11;
            m.m22 = m.m22 + m.m12;
            m.m23 = m.m23 + m.m13;
            m.m10 = m.m10 + m.m00;
            m.m11 = m.m11 + m.m01;
            m.m12 = m.m12 + m.m02;
            m.m13 = m.m13 + m.m03;

            v.x = m.m30 / m.m33;
            v.y = m.m31 / m.m33;
            v.z = m.m32 / m.m33;
            v = RoundPlaces(v, 10);
            result[id] = v;
            id++;
        }
        return result;
    }

    static float RoundPlaces(float a, int places) {
        float shift = powersOfTen[places];
        return (float)(Mathf.Round(a * shift) / shift);
    }
    static Vector3 RoundPlaces(Vector3 v, int n) {
        v.x = RoundPlaces(v.x, n);
        v.y = RoundPlaces(v.y, n);
        v.z = RoundPlaces(v.z, n);
        return v;
    }

    static bool discontinuity(MartiniDNAPlane pp1, MartiniDNAPlane pp2, MartiniDNAPlane pp3, MartiniDNAPlane pp4) {
        if (diffPP(pp1.r1.id, pp1.r2.id) || diffPP(pp1.r2.id, pp1.r3.id)) {
            return true;
        }
        if (diffPP(pp2.r1.id, pp2.r2.id) || diffPP(pp2.r2.id, pp2.r3.id)) {
            return true;
        }
        if (diffPP(pp3.r1.id, pp3.r2.id) || diffPP(pp3.r2.id, pp3.r3.id)) {
            return true;
        }
        if (diffPP(pp4.r1.id, pp4.r2.id) || diffPP(pp4.r2.id, pp4.r3.id)) {
            return true;
        }

        return false;
    }


    static bool diffPP(int id1, int id2) {
        int diff = 1;
        if (id1 < 0 && id2 > 0) {
            diff = 2;
        }
        if (id2 - id1 > diff) {
            return true;
        }
        return false;
    }

    public static MeshData createCylinder(float height, float radius) {

        float bottomRadius = radius;
        float topRadius = bottomRadius;
        const int nbSides = 18;

        int nbVerticesCap = nbSides + 1;
        #region Vertices

        // bottom + top + sides
        Vector3[] vertices = new Vector3[nbVerticesCap + nbVerticesCap + nbSides * 2 + 2];
        int vert = 0;
        float _2pi = Mathf.PI * 2f;

        // Bottom cap
        vertices[vert++] = new Vector3(0f, 0f, 0f);
        while ( vert <= nbSides )
        {
            float rad = (float)vert / nbSides * _2pi;
            vertices[vert] = new Vector3(Mathf.Cos(rad) * bottomRadius, 0f, Mathf.Sin(rad) * bottomRadius);
            vert++;
        }

        // Top cap
        vertices[vert++] = new Vector3(0f, height, 0f);
        while (vert <= nbSides * 2 + 1)
        {
            float rad = (float)(vert - nbSides - 1)  / nbSides * _2pi;
            vertices[vert] = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
            vert++;
        }

        // Sides
        int v = 0;
        while (vert <= vertices.Length - 4 )
        {
            float rad = (float)v / nbSides * _2pi;
            vertices[vert] = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
            vertices[vert + 1] = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
            vert += 2;
            v++;
        }
        vertices[vert] = vertices[ nbSides * 2 + 2 ];
        vertices[vert + 1] = vertices[nbSides * 2 + 3 ];
        #endregion

        //#region Normales

        // // bottom + top + sides
        //         Vector3[] normales = new Vector3[vertices.Length];
        //         vert = 0;

        // // Bottom cap
        //         while ( vert  <= nbSides )
        //         {
        //             normales[vert++] = Vector3.down;
        //         }

        // // Top cap
        //         while ( vert <= nbSides * 2 + 1 )
        //         {
        //             normales[vert++] = Vector3.up;
        //         }

        // // Sides
        //         v = 0;
        //         while (vert <= vertices.Length - 4 )
        //         {
        //             float rad = (float)v / nbSides * _2pi;
        //             float cos = Mathf.Cos(rad);
        //             float sin = Mathf.Sin(rad);

        //             normales[vert] = new Vector3(cos, 0f, sin);
        //             normales[vert + 1] = normales[vert];

        //             vert += 2;
        //             v++;
        //         }
        //         normales[vert] = normales[ nbSides * 2 + 2 ];
        //         normales[vert + 1] = normales[nbSides * 2 + 3 ];
        //         #endregion

        #region Triangles
        int nbTriangles = nbSides + nbSides + nbSides * 2;
        int[] triangles = new int[nbTriangles * 3 + 3];

        // Bottom cap
        int tri = 0;
        int i = 0;
        while (tri < nbSides - 1)
        {
            triangles[ i ] = 0;
            triangles[ i + 1 ] = tri + 1;
            triangles[ i + 2 ] = tri + 2;
            tri++;
            i += 3;
        }
        triangles[i] = 0;
        triangles[i + 1] = tri + 1;
        triangles[i + 2] = 1;
        tri++;
        i += 3;

        // Top cap
        //tri++;
        while (tri < nbSides * 2)
        {
            triangles[ i ] = tri + 2;
            triangles[i + 1] = tri + 1;
            triangles[i + 2] = nbVerticesCap;
            tri++;
            i += 3;
        }

        triangles[i] = nbVerticesCap + 1;
        triangles[i + 1] = tri + 1;
        triangles[i + 2] = nbVerticesCap;
        tri++;
        i += 3;
        tri++;

        // Sides
        while ( tri <= nbTriangles )
        {
            triangles[ i ] = tri + 2;
            triangles[ i + 1 ] = tri + 1;
            triangles[ i + 2 ] = tri + 0;
            tri++;
            i += 3;

            triangles[ i ] = tri + 1;
            triangles[ i + 1 ] = tri + 2;
            triangles[ i + 2 ] = tri + 0;
            tri++;
            i += 3;
        }
        #endregion

        MeshData mdata = new MeshData();
        mdata.triangles = triangles;
        mdata.vertices = vertices;
        return mdata;
    }

    static bool checkDistanceBB_BB(MartiniDNAPlane pp,
                                   int idF, UnityMolSelection sel) {
        const float limitDistBB_BB = 8.0f;//Max distance between 2 BB in Angstrom

        Vector3 p1 = Vector3.zero;
        Vector3 p2 = Vector3.zero;
        Vector3 p3 = Vector3.zero;
        if (pp.r1.atoms.ContainsKey(Cname)) {
            p1 = pp.r1.atoms[Cname].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[pp.r1.atoms[Cname]];
                p1 = sel.extractTrajFramePositions[idF][ida];
            }
        }
        else {
            p1 = pp.r1.atoms[Cname2].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[pp.r1.atoms[Cname2]];
                p1 = sel.extractTrajFramePositions[idF][ida];
            }
        }

        if (pp.r2.atoms.ContainsKey(Cname)) {
            p2 = pp.r2.atoms[Cname].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[pp.r2.atoms[Cname]];
                p2 = sel.extractTrajFramePositions[idF][ida];
            }
        }
        else {
            p2 = pp.r2.atoms[Cname2].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[pp.r2.atoms[Cname2]];
                p2 = sel.extractTrajFramePositions[idF][ida];
            }
        }

        if (pp.r3.atoms.ContainsKey(Cname)) {
            p3 = pp.r3.atoms[Cname].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[pp.r3.atoms[Cname]];
                p3 = sel.extractTrajFramePositions[idF][ida];
            }
        }
        else {
            p3 = pp.r3.atoms[Cname2].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[pp.r3.atoms[Cname2]];
                p3 = sel.extractTrajFramePositions[idF][ida];
            }
        }

        if (Vector3.Distance(p1, p2) >= limitDistBB_BB) {
            return false;
        }
        if (Vector3.Distance(p2, p3) >= limitDistBB_BB) {
            return false;
        }
        return true;
    }

}

public class MartiniDNAPlane {

    public UnityMolResidue r1;
    public UnityMolResidue r2;
    public UnityMolResidue r3;
    public Vector3 position;
    public Vector3 normal;
    public Vector3 forward;
    public Vector3 side;
    public bool flipped;

    public MartiniDNAPlane(UnityMolResidue res1, UnityMolResidue res2, UnityMolResidue res3,
                           int idF, UnityMolSelection sel) {

        if (!MDAnalysisSelection.isNucleic(res1) || !MDAnalysisSelection.isNucleic(res2) || !MDAnalysisSelection.isNucleic(res3))
            return;
        r1 = res1;
        r2 = res2;
        r3 = res3;

        if (!r1.atoms.ContainsKey(RibbonMeshMartiniDNA.Cname) && !r1.atoms.ContainsKey(RibbonMeshMartiniDNA.Cname2)) {
            r1 = null;
            // Debug.LogError("Cannot allocate a MartiniDNAPlane because residue "+r1+" does not contain C3' atom");
            return;
        }
        if (!r1.atoms.ContainsKey(RibbonMeshMartiniDNA.Oname)) {
            r1 = null;
            // Debug.LogError("Cannot allocate a MartiniDNAPlane because residue "+r1+" does not contain O5' atom");
            return;
        }
        if (!r2.atoms.ContainsKey(RibbonMeshMartiniDNA.Cname) && !r2.atoms.ContainsKey(RibbonMeshMartiniDNA.Cname2)) {
            r1 = null;
            // Debug.LogError("Cannot allocate a MartiniDNAPlane because residue "+r2+" does not contain C3' atom");
            return;
        }
        if (!r3.atoms.ContainsKey(RibbonMeshMartiniDNA.Cname) && !r3.atoms.ContainsKey(RibbonMeshMartiniDNA.Cname2)) {
            r1 = null;
            // Debug.LogError("Cannot allocate a MartiniDNAPlane because residue "+r3+" does not contain O5' atom");
            return;
        }
        Vector3 ca1 = Vector3.zero;
        Vector3 ca2 = Vector3.zero;
        if (r1.atoms.ContainsKey(RibbonMeshMartiniDNA.Cname)) {
            ca1 = r1.atoms[RibbonMeshMartiniDNA.Cname].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[r1.atoms[RibbonMeshMartiniDNA.Cname]];
                ca1 = sel.extractTrajFramePositions[idF][ida];
            }
        }
        else {
            ca1 = r1.atoms[RibbonMeshMartiniDNA.Cname2].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[r1.atoms[RibbonMeshMartiniDNA.Cname2]];
                ca1 = sel.extractTrajFramePositions[idF][ida];
            }
        }
        ca1.x = -ca1.x;
        if (r2.atoms.ContainsKey(RibbonMeshMartiniDNA.Cname)) {
            ca2 = r2.atoms[RibbonMeshMartiniDNA.Cname].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[r2.atoms[RibbonMeshMartiniDNA.Cname]];
                ca2 = sel.extractTrajFramePositions[idF][ida];
            }
        }
        else {
            ca2 = r2.atoms[RibbonMeshMartiniDNA.Cname2].position;
            if (idF != -1) {
                int ida = sel.atomToIdInSel[r2.atoms[RibbonMeshMartiniDNA.Cname2]];
                ca2 = sel.extractTrajFramePositions[idF][ida];
            }
        }
        ca2.x = -ca2.x;
        Vector3 o1  = r1.atoms[RibbonMeshMartiniDNA.Oname].position;
        o1.x = -o1.x;

        Vector3 a = (ca2 - ca1).normalized;
        Vector3 b = (o1 -  ca1).normalized;
        Vector3 c = Vector3.Cross(a, b).normalized;
        Vector3 d = Vector3.Cross(c, a).normalized;
        Vector3 p = (ca1 + ca2) / 2.0f;

        position = p;
        // position = ca1;
        normal = c;
        forward = a;
        side = d;
        flipped = false;

    }
    public void Flip() {
        side = -side;
        normal = -normal;
        flipped = !flipped;
    }

}
}
