using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Linq;

namespace UMol {
public class SequenceViewerUI : MonoBehaviour {


	public static string[] allColors = new string[] {"navy", "lime", "blue", "magenta", "brown", "cyan", "green",
	        "lightblue", "black", "maroon", "grey", "darkblue", "olive",
	        "orange", "purple", "red", "silver", "teal", "yellow"};

	public Dictionary<string, Dictionary<UnityMolResidue, GameObject>> residueToUIGo = new Dictionary<string, Dictionary<UnityMolResidue, GameObject>>();
	public List<Toggle> allUIToggle = new List<Toggle>();

	public GameObject CreateSequenceViewer(UnityMolStructure newS) {

		Dictionary<UnityMolResidue, GameObject> resToGo = new Dictionary<UnityMolResidue, GameObject>();

		string seqVName = "SequenceViewer";
		if (XRSettings.enabled) {
			seqVName += "VR";
		}

		
		Transform seqViewerT = transform.Find(seqVName);
		GameObject seqViewer = null;

		if (seqViewerT == null) {
			if (UnityMolMain.inVR()) {
				seqViewer = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/CanvasSequenceViewerVR"));
			}
			else{
				seqViewer = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/CanvasSequenceViewer"));
			}
			seqViewer.name = seqVName;
			seqViewer.transform.SetParent(transform);
		}
		else{
			seqViewer = seqViewerT.gameObject;
		}
		Transform seqList = seqViewer.transform.Find("Scroll View/Content");


		GameObject newSeqView = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/MoleculeSeqView"));
		newSeqView.transform.SetParent(seqList, false);


		// if (XRSettings.enabled) {

		// 	newSeqView.transform.localPosition = Vector3.zero;
		// 	newSeqView.transform.localScale = Vector3.one;
		// 	newSeqView.transform.localRotation = Quaternion.identity;
		// }


		newSeqView.transform.Find("MoleculeLabel/Text").GetComponent<Text>().text = newS.name;
		Transform contentParent = newSeqView.transform.Find("Scroll View/Content");

		int idColorChain = 0;

		foreach (UnityMolChain c in newS.currentModel.chains.Values) {
			string nameChain = "<color=" + allColors[idColorChain] + "><b><size=8>" + c.name + "</size></b>";
			foreach (UnityMolResidue r in c.residues) {
				int id = r.id;
				string name = r.name;
				GameObject newResUI = GameObject.Instantiate((GameObject) Resources.Load("Prefabs/ResidueToggle"));
				newResUI.transform.SetParent(contentParent, false);

				string resLabel = nameChain + "\n" + name + "\n" +id.ToString() + "</color>";
				newResUI.transform.Find("Label").GetComponent<Text>().text = resLabel;

				Toggle resToggle = newResUI.GetComponent<Toggle>();
				resToggle.onValueChanged.AddListener((value) => { ResidueButtonSelectionSwitch(r, value); });

				resToGo[r] = newResUI;
				allUIToggle.Add(resToggle);

			}
			idColorChain++;
			if (idColorChain == allColors.Length) {
				idColorChain = 0;
			}
		}

		residueToUIGo[newS.name] = resToGo;


		return newSeqView;
	}

	public void ResidueButtonSelectionSwitch(UnityMolResidue r, bool val) {
		UnityMolSelectionManager selM = UnityMolMain.getSelectionManager();

		UnityMolSelection sel = r.ToSelection();
		UnityMolSelection curSel = selM.getCurrentSelection();

		if (val) {
			API.APIPython.addToSelection(sel.MDASelString, curSel.name);
		}
		else {
			API.APIPython.removeFromSelection(sel.MDASelString, curSel.name);
		}
	}


	public void DeleteStructure(string s){

		if(residueToUIGo.ContainsKey(s)){
			GameObject toRM = null;
			bool first = true;
			foreach(GameObject go in residueToUIGo[s].Values) {
				if(first){
					toRM = go.transform.parent.parent.parent.gameObject;
				}
				first = false;
				allUIToggle.Remove(go.GetComponent<Toggle>());
			}
			residueToUIGo.Remove(s);
			if(toRM != null){
				GameObject.Destroy(toRM);
			}
		}
		
	}
}
}