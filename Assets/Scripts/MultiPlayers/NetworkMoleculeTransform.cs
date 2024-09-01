using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UMol;
using TMPro;

public class NetworkMoleculeTransform : MonoBehaviourPunCallbacks, IPunObservable
{
    public Transform molTransform;

    private Vector3 latestPos;
    private Quaternion latestRot;
    private Vector3 latestScale;

    private Vector3 latestChildPos;
    private Quaternion latestChildRot;
    private Vector3 latestChildScale;

    private bool waitingToLoadPDB = true;
    private GameObject loadedMolecule;
    private string molName;


    void Start()
    {
        loadedMolecule = UnityMolMain.getRepresentationParent();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (photonView.IsMine && waitingToLoadPDB == false)
            {
                stream.SendNext(molTransform.localPosition);
                stream.SendNext(molTransform.localRotation);
                stream.SendNext(molTransform.localScale);

                if (GetFirstChild(molTransform) != null)
                {
                    stream.SendNext(GetFirstChild(molTransform).localPosition);
                    stream.SendNext(GetFirstChild(molTransform).localRotation);
                    stream.SendNext(GetFirstChild(molTransform).localScale);
                }
            }
        }
        else
        {
            latestPos = (Vector3)stream.ReceiveNext();
            latestRot = (Quaternion)stream.ReceiveNext();
            latestScale = (Vector3)stream.ReceiveNext();

            latestChildPos = (Vector3)stream.ReceiveNext();
            latestChildRot = (Quaternion)stream.ReceiveNext();
            latestChildScale = (Vector3)stream.ReceiveNext();
        }
    }


    public void Update()
    {
        if (photonView.IsMine)
        {
            UpdateOwner();
        }
        else
        {
            UpdateRemote();
        }

        Transform firstChild = GetFirstChild(loadedMolecule.transform);
        if (firstChild != null)
        {
            molName = firstChild.gameObject.name;
        }
    }

    private void UpdateOwner()
    {
        Transform firstChild = GetFirstChild(loadedMolecule.transform);

        if (waitingToLoadPDB && firstChild != null)
        {
            molTransform.localPosition = loadedMolecule.transform.localPosition;
            molTransform.localRotation = loadedMolecule.transform.localRotation;
            molTransform.localScale = loadedMolecule.transform.localScale;

            if (GetFirstChild(molTransform) != null)
            {
                GetFirstChild(molTransform).localPosition = firstChild.localPosition;
                GetFirstChild(molTransform).localRotation = firstChild.localRotation;
                GetFirstChild(molTransform).localScale = firstChild.localScale;
            }

            waitingToLoadPDB = false;
        }

        if (!waitingToLoadPDB)
        {
            GameObject molecule = GameObject.Find(molName);

            if (molecule != null && molecule.transform.parent != null)
            {
                molTransform.localPosition = molecule.transform.parent.localPosition;
                molTransform.localRotation = molecule.transform.parent.localRotation;
                molTransform.localScale = molecule.transform.parent.localScale;

                if (GetFirstChild(molTransform) != null)
                {
                    GetFirstChild(molTransform).localPosition = molecule.transform.localPosition;
                    GetFirstChild(molTransform).localRotation = molecule.transform.localRotation;
                    GetFirstChild(molTransform).localScale = molecule.transform.localScale;
                }
            }
        }
    }

    private void UpdateRemote()
    {
        molTransform.localPosition = latestPos;
        molTransform.localRotation = latestRot;
        molTransform.localScale = latestScale;

        if (GetFirstChild(molTransform) != null)
        {
            GetFirstChild(molTransform).localPosition = latestChildPos;
            GetFirstChild(molTransform).localRotation = latestChildRot;
            GetFirstChild(molTransform).localScale = latestChildScale;
        }

        transform.localPosition = molTransform.localPosition;
        transform.localRotation = molTransform.localRotation;
        transform.localScale = molTransform.localScale;

        if (GetFirstChild(transform) != null)
        {
            GetFirstChild(transform).localPosition = GetFirstChild(molTransform).localPosition;
            GetFirstChild(transform).localRotation = GetFirstChild(molTransform).localRotation;
            GetFirstChild(transform).localScale = GetFirstChild(molTransform).localScale;
        }
    }

    private Transform GetFirstChild(Transform parent)
    {
        if (parent != null && parent.childCount > 0)
        {
            return parent.GetChild(0);
        }
        return null;
    }
}
