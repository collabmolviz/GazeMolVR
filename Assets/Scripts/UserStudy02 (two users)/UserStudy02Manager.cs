using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class UserStudy02Manager : MonoBehaviourPunCallbacks
{
    private PhotonView photonView;

    public string participantName;

    public enum ExpType
    {
        None,
        ControllerOnly,
        Controller_Trail,
        Controller_Spotlight,
    }

    public enum ProteinRepresentaionType
    {
        None,
        Cartoon,
        Surface,
        HyperBall
    }

    public enum PDBList
    {
        None,
        Andre_METY_dodecamer,
        Benoit_3p50,
        Caroline_PDB,
        Etienne_PAR1,
        Felix_6rdi,
        Florencia_PDB,
        Jonathan_6b3r,
        Jules_Alpha1_Beta1,
        Julie_1t46,
        Laetitia_5eao,
        Lucas_1F4J,
        Mariano_6vxx,
        Raphael_7yk5,
        Sujith_MCTP,
        William_5NIK,
        Yanna_1QJ8,
        Maya_1KZ0,
        Julia_PDB,
        Maya_1KZ0_2,
        Carine_PDB,
        HB_4A97,
        HB_4A98,
        HB_6X3S,
        HB_6X3U,
        HB_6X3Z
    }

    public ExpType _expType = ExpType.None;
    public ExpType expType
    {
        get { return _expType; }
        set
        {
            if (_expType != value)
            {
                _expType = value;
                photonView.RPC("UpdateExpType", RpcTarget.All, value);
            }
        }
    }

    public ProteinRepresentaionType _proteinRepresentaionType = ProteinRepresentaionType.None;
    public ProteinRepresentaionType proteinRepresentaionType
    {
        get { return _proteinRepresentaionType; }
        set
        {
            if (_proteinRepresentaionType != value)
            {
                _proteinRepresentaionType = value;
                photonView.RPC("UpdateProteinRepresentationType", RpcTarget.All, value);
            }
        }
    }

    public PDBList _currentPDB = PDBList.None;
    public PDBList currentPDB
    {
        get { return _currentPDB; }

        set
        {
            if (_currentPDB != value)
            {
                _currentPDB = value;
                photonView.RPC("UpdateCurrentPDB", RpcTarget.All, value);
            }
        }
    }

    [PunRPC]
    public void UpdateExpType(ExpType newType)
    {
        _expType = newType;
    }

    [PunRPC]
    public void UpdateProteinRepresentationType(ProteinRepresentaionType newType)
    {
        _proteinRepresentaionType = newType;
    }

    [PunRPC]
    public void UpdateCurrentPDB(PDBList newPDB)
    {
        _currentPDB = newPDB;
    }

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        expType = ExpType.None;
        proteinRepresentaionType = ProteinRepresentaionType.None;
        currentPDB = PDBList.None;
    }

    void OnValidate()
    {
        if (photonView == null) photonView = GetComponent<PhotonView>();

        // Check if Photon is connected and the application is playing
        if (PhotonNetwork.IsConnected && Application.isPlaying)
        {
            photonView.RPC("UpdateExpType", RpcTarget.All, _expType);
            photonView.RPC("UpdateProteinRepresentationType", RpcTarget.All, _proteinRepresentaionType);
            photonView.RPC("UpdateCurrentPDB", RpcTarget.All, _currentPDB);
        }
    }

}
