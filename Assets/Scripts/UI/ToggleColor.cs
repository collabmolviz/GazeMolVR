using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleColor : MonoBehaviour {

    private Toggle toggle;
    private float alpha = 1.0f;
    public Color onColor = Color.gray;
    public Color offColor = Color.white;
    public bool isOn = false;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        //Add listener for when the state of the Toggle changes, to take action
        // toggle.onValueChanged.AddListener(delegate {
        //         ToggleValueChanged(toggle);
        //     });
        onColor.a = alpha;
        offColor.a = alpha;

        ColorBlock cb = toggle.colors;
        cb.normalColor = offColor;
        cb.highlightedColor = offColor;
        UpdateToggleColor();
    }
    void Update(){
        if(isOn != toggle.isOn){
            // ToggleValueChanged(toggle);
            UpdateToggleColor();
            isOn = toggle.isOn;
        }
    }
    public void UpdateToggleColor(){
        ColorBlock cb = toggle.colors;
        if (!toggle.isOn)//Turn it off
        {
            cb.normalColor = offColor;
            // cb.highlightedColor = offColor;
        }
        else
        {
            cb.normalColor = onColor;
            // cb.highlightedColor = onColor;
        }
        toggle.colors = cb;
    }
    // public void ToggleValueChanged(Toggle change)
    // {
    //     if(toggle == null){
    //         toggle = change;
    //     }

    //     ColorBlock cb = toggle.colors;
    //     if (!toggle.isOn)//Turn it off
    //     {
    //         cb.normalColor = offColor;
    //         cb.highlightedColor = offColor;
    //     }
    //     else
    //     {
    //         cb.normalColor = onColor;
    //         cb.highlightedColor = onColor;
    //     }
    //     toggle.colors = cb;
    //     isOn = toggle.isOn;
    // }
}
