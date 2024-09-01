using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Text;
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using Autodesk.Fbx;
#endif

namespace UMol {
public class FBXExporter {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

    public static void writeMesh(List<GameObject> gos, string filePath, bool withAO = true) {

        List<MeshFilter> allMf = new List<MeshFilter>();
        foreach (GameObject go in gos) {
            MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in mfs) {
                // MeshRenderer mr = mf.gameObject.GetComponent<MeshRenderer>();
                // if(mr != null && mr.enabled){
                allMf.Add(mf);
                // }
            }
        }

        using(FbxManager fbxManager = FbxManager.Create ()) {
            // configure IO settings.
            fbxManager.SetIOSettings (FbxIOSettings.Create (fbxManager, Globals.IOSROOT));
            // Export the scene
            using (FbxExporter exporter = FbxExporter.Create (fbxManager, "myExporter")) {
                // Initialize the exporter.
                bool status = exporter.Initialize (filePath, -1, fbxManager.GetIOSettings ());
                // Create a new scene to export
                FbxScene scene = FbxScene.Create (fbxManager, "UMolScene");

                // Get scene root node
                FbxNode rootNode = scene.GetRootNode();


                foreach (MeshFilter mf in allMf) {


                    // Create base node
                    FbxNode meshNode = FbxNode.Create(scene, "curMesh" + mf.gameObject.name);

                    Mesh unitymesh = mf.sharedMesh;
                    int[] triangles = unitymesh.triangles;
                    Vector3[] vertices = unitymesh.vertices;
                    Vector3[] normals = unitymesh.normals;
                    Color[] colors = unitymesh.colors;
                    int triangleCount = triangles.Length;
                    int normalsCount = (normals == null ? 0 : normals.Length);
                    int colorCount = (colors == null ? 0 : colors.Length);


                    FbxMesh mesh = FbxMesh.Create(scene, "Mesh_" + mf.gameObject.name);
                    meshNode.SetNodeAttribute(mesh);
                    rootNode.AddChild(meshNode);

                    FbxLayer fbxLayer = mesh.GetLayer (0 /* default layer */);
                    if (fbxLayer == null)
                    {
                        mesh.CreateLayer ();
                        fbxLayer = mesh.GetLayer (0 /* default layer */);
                    }

                    // Add vertices to the mesh list
                    mesh.InitControlPoints(unitymesh.vertexCount);
                    for (int i = 0; i < unitymesh.vertexCount; i++)
                    {
                        mesh.SetControlPointAt(new FbxVector4(vertices[i].x * -1, vertices[i].y, vertices[i].z), i);
                    }

                    // Add triangles
                    for (int i = 0; i + 2 < triangleCount; i = i + 3)
                    {
                        mesh.BeginPolygon();
                        mesh.AddPolygon(triangles[i + 0]);
                        mesh.AddPolygon(triangles[i + 2]);
                        mesh.AddPolygon(triangles[i + 1]);
                        mesh.EndPolygon();
                    }
                    // mesh.BuildMeshEdgeArray();

                    // Add normals
                    if (normalsCount != 0) {

                        var meshNormals = FbxLayerElementNormal.Create (mesh, "Normals");

                        meshNormals.SetMappingMode (FbxLayerElement.EMappingMode.eByControlPoint);
                        meshNormals.SetReferenceMode (FbxLayerElement.EReferenceMode.eDirect);

                        FbxLayerElementArray fbxElementArray = meshNormals.GetDirectArray ();
                        for (int i = 0; i < normalsCount; i++)
                        {
                            fbxElementArray.Add(new FbxVector4(normals[i].x * -1, normals[i].y, normals[i].z, 0.0f));
                        }
                        fbxLayer.SetNormals(meshNormals);
                    }


                    // Add vertex colors
                    if (colorCount != 0)
                    {
                        FbxLayerElementVertexColor meshColors = FbxLayerElementVertexColor.Create(mesh, "VertexColors");

                        meshColors.SetMappingMode (FbxLayerElement.EMappingMode.eByControlPoint);
                        meshColors.SetReferenceMode (FbxLayerElement.EReferenceMode.eDirect);

                        FbxLayerElementArray fbxElementArraycol = meshColors.GetDirectArray ();

                        // for (int i = 0; i < triangleCount; i = i + 3)
                        // {
                        //     fbxElementArraycol.Add(new FbxColor(colors[triangles[i + 0]].r, colors[triangles[i + 0]].g, colors[triangles[i + 0]].b));
                        //     fbxElementArraycol.Add(new FbxColor(colors[triangles[i + 2]].r, colors[triangles[i + 2]].g, colors[triangles[i + 2]].b));
                        //     fbxElementArraycol.Add(new FbxColor(colors[triangles[i + 1]].r, colors[triangles[i + 1]].g, colors[triangles[i + 1]].b));
                        // }
                        bool addAO = false;
                        Renderer rd = mf.GetComponent<Renderer>();
                        if (withAO && rd != null) {
                            Material mat = rd.sharedMaterial;
                            if (mat.HasProperty("_AOIntensity") && mat.GetFloat("_AOIntensity") != 0.0f) {
                                addAO = true;
                                float inten = mat.GetFloat("_AOIntensity");

                                Color colme = Color.yellow;
                                for (int i = 0; i < vertices.Length; i++) {
                                    Color c = colors[i];
                                    c *= Mathf.Clamp( c.a * inten, 0.0f, 1.0f);
                                    fbxElementArraycol.Add(new FbxColor(c.r, c.g, c.b));
                                    if (i == 10)
                                        colme = c;
                                }
                                Debug.Log("Adding AO to colors ! " + colme);
                            }
                        }
                        if (!addAO) {
                            for (int i = 0; i < vertices.Length; i++) {
                                fbxElementArraycol.Add(new FbxColor(colors[i].r, colors[i].g, colors[i].b));
                            }
                        }
                        fbxLayer.SetVertexColors(meshColors);

                    }
                }


                exporter.Export(scene);
                // exporter.Destroy();


            }
        }
    }

#endif

}
}
