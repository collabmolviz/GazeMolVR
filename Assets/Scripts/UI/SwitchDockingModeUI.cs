
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UMol {

[RequireComponent(typeof(Button))]
public class SwitchDockingModeUI : MonoBehaviour {

    private string firstPartText = "Docking mode:\n<b><color=#007AC1>";
    public List<Button> dockingButtons = new List<Button>();
    public List<GameObject> toShowHide = new List<GameObject>();

    public Text butText;

    void Start() {
        if (butText == null) {
            butText = GetComponentsInChildren<Text>()[0];
        }
    }

    public void switchDockingModeText() {
        string newText = "";
        if(UnityMolMain.getDockingManager().isRunning){
            newText += "On</color></b>";
        }
        else{
            newText += "Off</color></b>";
        }
        butText.text = firstPartText + newText;

        foreach (Button button in dockingButtons){
            button.interactable = UnityMolMain.getDockingManager().isRunning;
        }
        foreach(GameObject go in toShowHide){
            go.SetActive(UnityMolMain.getDockingManager().isRunning);
        }
    }
}
}