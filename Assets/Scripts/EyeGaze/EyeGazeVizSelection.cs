using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class EyeGazeVizSelection : MonoBehaviourPun
{
    public GameObject gazePointer;
    public GameObject gazeArrow;
    public GameObject gazeTrailSphereHead;
    public GameObject gazeSpotlightSphereHead;
    public GameObject gazeTurnOff;

    public GameObject gazeTrailArrowHead;
    public GameObject gazeSpotlightArroweHead;

    public TextMeshProUGUI eyegazeViz;

    void Start()
    {
        DisableGazeViz(); // Initialize everything to off
    }

    [PunRPC]
    public void SelectGazePointer()
    {
        SetAllInactive();
        gazePointer.SetActive(true);
        eyegazeViz.text = "GazePointer";
    }

    [PunRPC]
    public void SelectGazeArrow()
    {
        SetAllInactive();
        gazeArrow.SetActive(true);
        eyegazeViz.text = "GazeArrow";
    }

    [PunRPC]
    public void SelectGazeTrailSphere()
    {
        SetAllInactive();
        gazeTrailSphereHead.SetActive(true);
        eyegazeViz.text = "GazeTrail";
    }

    [PunRPC]
    public void SelectGazeTrailArrow()
    {
        SetAllInactive();
        gazeTrailArrowHead.SetActive(true);
        eyegazeViz.text = "GazeTrail";
    }

    [PunRPC]
    public void SelectGazeSpotlightSphere()
    {
        SetAllInactive();
        gazeSpotlightSphereHead.SetActive(true);
        eyegazeViz.text = "GazeSpotlight";
    }

    [PunRPC]
    public void SelectGazeSpotlightArrow()
    {
        SetAllInactive();
        gazeSpotlightArroweHead.SetActive(true);
        eyegazeViz.text = "GazeSpotlight";
    }

    [PunRPC]
    public void DisableGazeViz()
    {
        SetAllInactive();
        gazeTurnOff.SetActive(true);
        eyegazeViz.text = "NoGaze";
    }

    private void SetAllInactive()
    {
        // Deactivate all gaze visualization GameObjects
        gazePointer.SetActive(false);
        gazeArrow.SetActive(false);
        gazeTrailSphereHead.SetActive(false);
        gazeSpotlightSphereHead.SetActive(false);
        gazeTurnOff.SetActive(false);

        gazeTrailArrowHead.SetActive(false);
        gazeSpotlightArroweHead.SetActive(false);
    }

    public void TriggerGazePointer()
    {
        photonView.RPC("SelectGazePointer", RpcTarget.All);
    }

    public void TriggerGazeArrow()
    {
        photonView.RPC("SelectGazeArrow", RpcTarget.All);
    }

    public void TriggerGazeTrailSphere()
    {
        photonView.RPC("SelectGazeTrailSphere", RpcTarget.All);
    }

    public void TriggerGazeSpotlightSphere()
    {
        photonView.RPC("SelectGazeSpotlightSphere", RpcTarget.All);
    }

    public void TriggerGazeTrailArrow()
    {
        photonView.RPC("SelectGazeTrailArrow", RpcTarget.All);
    }

    public void TriggerGazeSpotlightArrow()
    {
        photonView.RPC("SelectGazeSpotlightArrow", RpcTarget.All);
    }

    public void TriggerDisableGazeViz()
    {
        photonView.RPC("DisableGazeViz", RpcTarget.All);
    }
}
