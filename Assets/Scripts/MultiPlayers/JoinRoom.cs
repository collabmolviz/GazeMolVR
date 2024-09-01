using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;
using UMol;


public class JoinRoom : MonoBehaviourPunCallbacks
{
    public GameObject rightHandController;
    public GameObject leftHandController;

    ControllerGrabAndScale controllerGrabAndScaleRightHand;
    ControllerGrabAndScale controllerGrabAndScaleLeftHand;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Connect();
        }
    }

    private void Connect()
    {
        PhotonNetwork.GameVersion = "0.1";
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("connecting to server ...");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("connected to server.");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("disconnected from the server for reason: " + cause.ToString());
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("tried to join a room and failed.");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 5 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("joined a room - yay !!");

        controllerGrabAndScaleRightHand = rightHandController.GetComponent<ControllerGrabAndScale>();
        controllerGrabAndScaleLeftHand = leftHandController.GetComponent<ControllerGrabAndScale>();

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("master client");

            controllerGrabAndScaleRightHand.enabled = true;
            controllerGrabAndScaleLeftHand.enabled = true;
        }
        else
        {
            controllerGrabAndScaleRightHand.enabled = false;
            controllerGrabAndScaleLeftHand.enabled = false;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("A new player joined the room");
    }
}
