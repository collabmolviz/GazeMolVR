using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Runtime.InteropServices;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UMol
{
[CustomEditor(typeof(RaytracedObject))]
public class MaterialEditor : Editor {

    private RaytracedObject rtObj;
    private RaytracingMaterial rtMat;


    void OnEnable() {
        rtObj = (target as RaytracedObject);
        rtMat = rtObj.rtMat;
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        if (rtObj == null)
            return;

        if (rtObj.type != RaytracedObject.RayTObjectType.mesh)
            return;
        // Add additional fields here
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("OSPRay Material editor", MessageType.None, true);
        EditorGUILayout.Space();


        Type t = rtMat.GetType();
        var propertyValues = t.GetProperties ();
        foreach (var p in propertyValues) {
            if (p.PropertyType == typeof(Vector3)) { //Vector3
                p.SetValue(rtMat, EditorGUILayout.Vector3Field(p.Name, (Vector3)p.GetValue(rtMat)));
            }
            else if (p.PropertyType == typeof(float)) { //float
                p.SetValue(rtMat, EditorGUILayout.FloatField(p.Name, (float)p.GetValue(rtMat)));

            }
            else if (p.PropertyType == typeof(bool) && p.Name != "propertyChanged") { //bool
                p.SetValue(rtMat, EditorGUILayout.Toggle(p.Name, (bool)p.GetValue(rtMat)));
            }
        }
    }
}
}