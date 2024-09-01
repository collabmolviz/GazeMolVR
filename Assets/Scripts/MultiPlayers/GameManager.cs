using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    //public GameObject playerPrefab;

    public bool loadSpectatorPlayer = false;
    public List<GameObject> avatarPrefabs;
    public int selectedAvatarIndex;
    private GameObject player;

    public override void OnJoinedRoom()
    {
        //var position = new Vector3(UnityEngine.Random.Range(-2.0f, 1.0f), 0, UnityEngine.Random.Range(-1.0f, 1.4f));
        //PhotonNetwork.Instantiate(playerPrefab.name, position, Quaternion.identity);

        /*
        if (PhotonNetwork.IsMasterClient)
        {
            player = PhotonNetwork.Instantiate(avatarPrefabs[selectedAvatarIndex].name, new Vector3(0, 0, 0), Quaternion.identity);
        }
        else
        {
            player = PhotonNetwork.Instantiate(avatarPrefabs[selectedAvatarIndex].name, new Vector3(1, 0, 0), Quaternion.identity);
        }
        */
    }

    public override void OnLeftRoom()
    {
        if (player != null)
        {
            PhotonNetwork.Destroy(player); // PhotonNetwork.Destroy(playerPrefab);
        }
    }

    public void OnAvatarButton1Clicked()
    {
        selectedAvatarIndex = 0;
        //Debug.Log("Avatar 1 selected, index: " + selectedAvatarIndex);
        Debug.Log("Player 01 joined");
        player = PhotonNetwork.Instantiate(avatarPrefabs[selectedAvatarIndex].name, new Vector3(0, 0, 0), Quaternion.identity);
    }

    public void OnAvatarButton2Clicked()
    {
        selectedAvatarIndex = 1;
        //Debug.Log("Avatar 2 selected, index: " + selectedAvatarIndex);
        Debug.Log("Player 02 joined");
        player = PhotonNetwork.Instantiate(avatarPrefabs[selectedAvatarIndex].name, new Vector3(1, 0, 0), Quaternion.identity);
    }

    public void Update()
    {
        if (loadSpectatorPlayer)
        {
            selectedAvatarIndex = 2;
            player = PhotonNetwork.Instantiate(avatarPrefabs[selectedAvatarIndex].name, new Vector3(0, 0, 0), Quaternion.identity);
            Debug.Log("Spectator Player joined");
            loadSpectatorPlayer = false;
        }
    }
}
