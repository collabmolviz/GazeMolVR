using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace UMol
{


public class ReadOSPRayMaterialJson {

    ///Store the materials in the material bank
    public static void readRTMatJson(string path){

		IDictionary deserializedData = null;

        StreamReader sr;

        if (Application.platform == RuntimePlatform.Android) {
            var textStream = new StringReaderStream(AndroidUtils.GetFileText(path));
            sr = new StreamReader(textStream);
        }
        else
            sr = new StreamReader(path);

		using(sr) {
			string jsonString = sr.ReadToEnd();
			deserializedData = (IDictionary) Json.Deserialize(jsonString);
		}

        try{
            IDictionary mats = (IDictionary)deserializedData["materials"];
            foreach(string m in mats.Keys){
                string name = m;
                string type = "principled";
                bool ignore = false;
                Dictionary<string, object> parameters = new Dictionary<string, object>();
                IDictionary matParams = (IDictionary)mats[m];
                foreach(string p in matParams.Keys){
                    if(p == "doubles" || p == "float" || p == "floats"){
                        IDictionary fParams = (IDictionary)matParams[p];
                        foreach(string fp in fParams.Keys){

                            IList tmp = (IList) fParams[fp];
                            List<float> resParams = new List<float>();
                            foreach(var t in tmp){
                                resParams.Add(float.Parse(t.ToString(), System.Globalization.CultureInfo.InvariantCulture));
                            }
                            if(resParams.Count == 1){
                                parameters[fp] = resParams[0];
                            }
                            if(resParams.Count == 3){
                                parameters[fp] = new Vector3(resParams[0], resParams[1], resParams[2]);
                            }
                        }
                    }
                    else if (p != "textures") {//Ignoring textures for now

                        if(p == "type"){
                            type = matParams[p] as string;
                            if(type == "OBJMaterial"){//Ignoring OBJ material for scivis
                                ignore = true;
                                break;
                            }
                        }
                        else{

                            List<float> otherParams = new List<float>();
                            foreach(var t in matParams[p] as IList){
                                otherParams.Add(float.Parse(t.ToString(), System.Globalization.CultureInfo.InvariantCulture));
                            }
                            if(otherParams.Count == 1){
                                parameters[p] = otherParams[0];
                            }
                            if(otherParams.Count == 3){
                                parameters[p] = new Vector3(otherParams[0], otherParams[1], otherParams[2]);
                            }
                        }
                    }
                }
                if(ignore){
                    continue;
                }
                RaytracingMaterial newmat = getRTMat(parameters, type.ToLower());
                RaytracingMaterial.materialsBank[name] = newmat;
                Debug.Log("Adding RT material " + name);
            }
        }
        catch (System.Exception e){
            Debug.LogError("Wrong json format "+e);
            return;
        }
    }

    static RaytracingMaterial getRTMat(Dictionary<string, object> parameters, string lowtype){
        RaytracingMaterial res = null;
        if(lowtype == "principled"){
            res = new RaytracingPrincipledMaterial();
        }
        else if(lowtype == "carpaint"){
            res = new RaytracingCarPaintMaterial();
        }
        else if(lowtype == "metal"){
            res = new RaytracingMetalMaterial();
        }
        else if(lowtype == "alloy"){
            res = new RaytracingAlloyMaterial();
        }
        else if(lowtype == "glass"){
            res = new RaytracingGlassMaterial();
        }
        else if(lowtype == "thinglass"){
            res = new RaytracingThinGlassMaterial();
        }
        else if(lowtype == "metallicpaint"){
            res = new RaytracingMetallicPaintMaterial();
        }
        else if(lowtype == "luminous"){
            res = new RaytracingLuminousMaterial();
        }
        else {
            return res;
        }
        foreach(string k in parameters.Keys){
            res.setRTMatProperty(k, parameters[k]);
        }
        return res;
    }
}

}