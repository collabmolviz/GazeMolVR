using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UMol;

public class ChangeOwnership : MonoBehaviourPunCallbacks
{
    public GameObject rightHandController;
    public GameObject leftHandController;

    ControllerGrabAndScale controllerGrabAndScaleRightHand;
    ControllerGrabAndScale controllerGrabAndScaleLeftHand;

    public void Start()
    {
        controllerGrabAndScaleRightHand = rightHandController.GetComponent<ControllerGrabAndScale>();
        controllerGrabAndScaleLeftHand = leftHandController.GetComponent<ControllerGrabAndScale>();
    }


    public void OnOwnershipBtnClicked()
    {
        if (photonView.Owner != PhotonNetwork.LocalPlayer)
        {
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);

            controllerGrabAndScaleRightHand.enabled = true;
            controllerGrabAndScaleLeftHand.enabled = true;

            photonView.RPC("DisableGrabAndScale", RpcTarget.Others);
        }
    }

    [PunRPC]
    void DisableGrabAndScale()
    {
        controllerGrabAndScaleRightHand.enabled = false;
        controllerGrabAndScaleLeftHand.enabled = false;
    }
}
