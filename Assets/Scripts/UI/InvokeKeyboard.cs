using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[RequireComponent(typeof(InputField))]
public class InvokeKeyboard : MonoBehaviour
{
    public GameObject toShow;
    private InputField inField;

    void Update()
    {
        if (inField == null)
            inField = GetComponent<InputField>();

        if (EventSystem.current.currentSelectedGameObject != null)
        {
            if (EventSystem.current.currentSelectedGameObject == gameObject)
            {
                if (toShow != null && inField.isFocused)
                {
                    if (!toShow.activeInHierarchy)
                    {
                        toShow.SetActive(true);
                        toShow.GetComponent<KeyboardUI>().inpF = inField;
                    }
                }
            }
        }
    }
}
