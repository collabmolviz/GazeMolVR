using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Globalization;
using MiniJSON;

namespace UMol {
public class FieldLinesReader {

	public string path = "";
	public Dictionary <string, List<Vector3>> linesPositions;

	public FieldLinesReader () {
	}
	public FieldLinesReader (string filePath) {

		path = filePath;

		linesPositions = new Dictionary <string, List<Vector3>>();

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


		foreach (string v in deserializedData.Keys) {
			List<Vector3> listP = new List<Vector3>();
			IList d = (IList)deserializedData[v];

			foreach (IList p in d) {

				float x = -Convert.ToSingle( p[0] );
				float y = Convert.ToSingle( p[1] );
				float z = Convert.ToSingle( p[2] );

				Vector3 pos = new Vector3(x, y, z);
				listP.Add(pos);

			}

			linesPositions[v] = listP;

		}
	}
}
}