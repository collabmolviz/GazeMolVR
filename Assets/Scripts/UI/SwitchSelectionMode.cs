
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UMol {

[RequireComponent(typeof(Button))]
public class SwitchSelectionMode : MonoBehaviour {

    public string firstPartText = "Selection mode:\n<color=blue><b>";

    public Text butText;
    UnityMolSelectionManager selM;

    void Start() {
        if (butText == null) {
            butText = GetComponentsInChildren<Text>()[0];
        }
        selM = UnityMolMain.getSelectionManager();
    }

    public void switchSelectionMode() {
        string newText = "";
        switch (selM.selectionMode) {
        case UnityMolSelectionManager.SelectionMode.Atom:
            selM.selectionMode = UnityMolSelectionManager.SelectionMode.Residue;
            newText = "\nResidue</b></color>";
            butText.text = firstPartText + newText;
            break;
        case UnityMolSelectionManager.SelectionMode.Residue:
            selM.selectionMode = UnityMolSelectionManager.SelectionMode.Chain;
            newText = "\nChain</b></color>";
            butText.text = firstPartText + newText;
            break;
        case UnityMolSelectionManager.SelectionMode.Chain:
            selM.selectionMode = UnityMolSelectionManager.SelectionMode.Atom;
            newText = "\nAtom</b></color>";
            butText.text = firstPartText + newText;
            break;
        // case UnityMolSelectionManager.SelectionMode.Molecule:
        //     selM.selectionMode = UnityMolSelectionManager.SelectionMode.Atom;
        //     newText += "Atom"+"</b></color>";
        //     butText.text = firstPartText + newText;
        //     break;
        default:
            selM.selectionMode = UnityMolSelectionManager.SelectionMode.Residue;
            newText = "\nResidue</b></color>";
            butText.text = firstPartText + newText;
            break;
        }
    }
}
}