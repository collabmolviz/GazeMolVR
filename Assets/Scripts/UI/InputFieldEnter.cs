using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace UMol {



[RequireComponent(typeof (InputField))]
public class InputFieldEnter : MonoBehaviour {

    public Button toTrigger;

    private InputField inputField;

    void Awake()
    {
        inputField = GetComponent<InputField>();
        inputField.lineType = InputField.LineType.MultiLineNewline;
    }

    void OnEnable()
    {
        inputField.onValidateInput += CheckForEnter;
    }

    void OnDisable()
    {
        inputField.onValidateInput -= CheckForEnter;
    }

    private char CheckForEnter(string text, int charIndex, char addedChar)
    {
        if (addedChar == '\n' && toTrigger != null)
        {
            toTrigger.onClick.Invoke();
            return '\0';
        }
        else
            return addedChar;
    }
}
}