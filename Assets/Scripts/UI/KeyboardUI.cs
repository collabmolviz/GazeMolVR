using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardUI : MonoBehaviour {

	public InputField inpF;

	public void sendKey(string keyval){
		if(inpF != null){
			if(keyval == "Back"){
				if(inpF.text.Length > 0)
					inpF.text = inpF.text.Remove(inpF.text.Length-1);
			}
			else{
				inpF.text += keyval;
			}
		}
	}
	public void deactivateIF(){
		if(inpF != null){
			inpF.DeactivateInputField();
		}
	}
}
