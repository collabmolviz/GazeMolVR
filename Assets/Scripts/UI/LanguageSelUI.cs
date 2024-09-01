using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace UMol {
public class LanguageSelUI : MonoBehaviour
{

    public Dropdown keywordsDD;
    public InputField selQueryInputF;
    void Start()
    {
        if (keywordsDD != null) {
            fillKeywords(keywordsDD);
            keywordsDD.onValueChanged.AddListener(delegate {
                DropdownValueChanged(keywordsDD);
            });
        }
    }

    void OnEnable() {
        UnityMolStructureManager.OnMoleculeLoaded += callFillKeywords;
        UnityMolStructureManager.OnMoleculeDeleted += callFillKeywords;
        UnityMolSelectionManager.OnNewSelection += callFillKeywords;
        UnityMolSelectionManager.OnSelectionDeleted += callFillKeywords;
    }

    void OnDisable() {
        UnityMolStructureManager.OnMoleculeLoaded -= callFillKeywords;
        UnityMolStructureManager.OnMoleculeDeleted -= callFillKeywords;
        UnityMolSelectionManager.OnNewSelection -= callFillKeywords;
        UnityMolSelectionManager.OnSelectionDeleted -= callFillKeywords;
    }

    public void doSelection() {
        if (selQueryInputF != null) {
            UMol.API.APIPython.select(selQueryInputF.text, "selLangUI");
        }
    }


    void callFillKeywords(){
        fillKeywords(keywordsDD);
    }

    void fillKeywords(Dropdown dd) {
        dd.ClearOptions();

        List<string> opts = new List<string>();

        foreach (string s in MDAnalysisSelection.predefinedKeywords) {
            opts.Add(s);
        }

        // opts.Add("x");
        // opts.Add("y");
        // opts.Add("z");
        // opts.Add("-");
        // opts.Add(">");
        // opts.Add("<");
        // opts.Add("<=");
        // opts.Add(">=");
        // opts.Add("==");
        // opts.Add("!=");
        // opts.Add("(");
        // opts.Add(")");

        foreach (string keyw in UnityMolMain.getSelectionManager().selectionMDAKeywords.Keys) {
            opts.Add(keyw);
        }

        foreach (UnityMolStructure s in UnityMolMain.getStructureManager().loadedStructures) {
            opts.Add(s.name);
        }


        opts.Sort();

        opts.Insert(0, "Keywords");

        dd.AddOptions(opts);
    }
    void DropdownValueChanged(Dropdown dd) {
        if (dd.value != 0) {
            string keyword = dd.options[dd.value].text;
            if (selQueryInputF != null) {
                selQueryInputF.text += keyword + " ";
            }
            dd.SetValue(0);
        }
    }

    public void addButtonText(Button b) {
        string val = b.GetComponentInChildren<Text>().text;
        if (val.Contains("space")) {
            val = " ";
        }
        if (selQueryInputF != null) {
            if (val == "") {
                int len = selQueryInputF.text.Length;
                if (len > 0) {
                    if (len > 1 && selQueryInputF.text[len - 1] == ' ')
                        selQueryInputF.text = selQueryInputF.text.Substring(0, len - 2);
                    else
                        selQueryInputF.text = selQueryInputF.text.Substring(0, len - 1);
                }
                return;
            }
            selQueryInputF.text += val;
            int d = 0;
            if (!int.TryParse(val, out d) && val != "." && val != " ") {//Digit
                selQueryInputF.text += " ";
            }
        }
    }
}
}