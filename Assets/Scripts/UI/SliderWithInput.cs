
using UnityEngine;
using UnityEngine.UI;


namespace UMol{

[RequireComponent(typeof(Slider))]
public class SliderWithInput : MonoBehaviour {

    public void updateSliderValue(InputField ipf){
        float newV = 0.0f;
        if(float.TryParse(ipf.text, out newV)){
            if(GetComponent<Slider>().value != newV)
                GetComponent<Slider>().value = newV;
        }
    }

    public void updateInputFieldValue(InputField ipf){
        ipf.SetValue(GetComponent<Slider>().value.ToString("F"+(ipf.characterLimit-2)));
    }
}
}