using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UntoggleAll : MonoBehaviour
{
    void OnEnable()
    {
        ToggleGroup[] toggleGroups = GetComponentsInChildren<ToggleGroup>();
        foreach (var toggleGroup in toggleGroups)
        {
            IEnumerable<Toggle> toggles = toggleGroup.GetComponentsInChildren<Toggle>();
            foreach (Toggle toggle in toggles)
            {
                toggle.isOn = false;
            }
        }
    }
}
