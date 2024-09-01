using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace UMol {
public class RotamerLibraryParser {

	public static Dictionary<string, List<float[]>> ParseRotamerLibrary(string filePath) {
		Dictionary<string, List<float[]>> rotlib = new Dictionary<string, List<float[]>>();

		StreamReader sr;

		if (Application.platform == RuntimePlatform.Android) {
			var textStream = new StringReaderStream(AndroidUtils.GetFileText(filePath));
			sr = new StreamReader(textStream);
		}
		else
			sr = new StreamReader(filePath);

		using (sr) {
			string line;
			while ((line = sr.ReadLine()) != null) {
				if (!line.StartsWith("#") && line.Length > 10) {
					string[] fields = line.Split(new [] { '\t', ' ', '\n'}, System.StringSplitOptions.RemoveEmptyEntries);
					string key = fields[0];
					int Nchi = fields.Length - 4;
					float[] chis = new float[Nchi];
					for (int c = 0; c < Nchi; c++) {
						chis[c] = float.Parse(fields[c+1], System.Globalization.CultureInfo.InvariantCulture);
					}
					if(!rotlib.ContainsKey(key)){
						rotlib[key] = new List<float[]>();
					}
					rotlib[key].Add(chis);
				}
			}
		}

		return rotlib;
	}


}
}