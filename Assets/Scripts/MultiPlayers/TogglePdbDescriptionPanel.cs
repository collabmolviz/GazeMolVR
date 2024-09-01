using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class TogglePdbDescriptionPanel : MonoBehaviour
{
    public GameObject proteinDescriptionTextPanel01;
    public GameObject proteinDescriptionTextPanel02;
    private bool toggleFlag = false;

    void Start()
    {
        proteinDescriptionTextPanel01.SetActive(false);
        proteinDescriptionTextPanel02.SetActive(false);
    }

    public void ChangeTextPanelStatus()
    {
        bool previousFlag = toggleFlag;
        toggleFlag = !toggleFlag;

        if (previousFlag != toggleFlag)
        {
            UpdatePanelStatus();
        }
    }

    private void UpdatePanelStatus()
    {
        if (toggleFlag)
        {
            proteinDescriptionTextPanel01.SetActive(PhotonNetwork.IsMasterClient);
            proteinDescriptionTextPanel02.SetActive(!PhotonNetwork.IsMasterClient);
        }
        else
        {
            proteinDescriptionTextPanel01.SetActive(false);
            proteinDescriptionTextPanel02.SetActive(false);
        }
    }
}
