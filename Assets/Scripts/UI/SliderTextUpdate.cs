using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderTextUpdate : MonoBehaviour {

    public string prefix = "Value: ";
    public Text textComponent;


    void Start(){
    	Slider sli = GetComponent<Slider>();
    	sli.onValueChanged.AddListener(delegate {SetSliderTextValue(sli.value); });
    }
    public void SetSliderTextValue(float sliderValue) {
        if(textComponent != null){
            textComponent.text = prefix + sliderValue.ToString("f2");
        }
    }
}