using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UMol;

[RequireComponent(typeof(InputField))]
public class activateKeyboard : MonoBehaviour {

    public GameObject toShow;
    private InputField inField;

    void Update () {
        if (inField == null)
            inField = GetComponent<InputField>();

        if (UnityMolMain.inVR() && EventSystem.current.currentSelectedGameObject == gameObject) {
            if (toShow != null && inField.isFocused)
            {
                if (!toShow.activeInHierarchy) {
                    toShow.SetActive(true);
                    toShow.GetComponent<KeyboardUI>().inpF = inField;
                    // toShow.GetComponent<MainKeyboard>().activeIF = inField;//VR keyboard
                }
            }
        }
    }
}
