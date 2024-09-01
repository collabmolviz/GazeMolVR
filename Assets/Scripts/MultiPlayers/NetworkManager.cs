using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;


public class NetworkManager : MonoBehaviourPunCallbacks
{
    public InputField inputField;

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
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log("disconnected from the server for reason: " + cause.ToString());
    }

    public void Play()
    {
        PhotonNetwork.JoinRandomRoom();
        PhotonNetwork.NickName = inputField.text;
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("tried to join a room and failed.");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 5 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("joined a room - yay !!");

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(1);
        }
    }
}
