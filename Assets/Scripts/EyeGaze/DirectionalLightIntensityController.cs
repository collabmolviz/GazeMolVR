using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class DirectionalLightIntensityController : MonoBehaviourPun
{
    public Light directionalLight;
    private Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate
        {
            photonView.RPC("ChangeLightIntensity", RpcTarget.All, slider.value);
        });
    }

    [PunRPC]
    public void ChangeLightIntensity(float newIntensity)
    {
        directionalLight.intensity = newIntensity;
    }
}
