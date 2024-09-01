using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEditor;
using UMol.API;
using UMol;
using TMPro;

public class NetworkLoadMolecule : MonoBehaviourPunCallbacks
{
    private GameObject loadedMolecue;
    public TextMeshProUGUI proteinViz;

    void Start()
    {
        loadedMolecue = UnityMolMain.getRepresentationParent();
    }

    public void LoadCartoonReprestation()
    {
        if (photonView.IsMine)
        {
            photonView.RPC("Cartoon", RpcTarget.All);
            proteinViz.text = "Cartoon";
        }
    }

    public void LoadHBReprestation()
    {
        if (photonView.IsMine)
        {
            photonView.RPC("HyperBall", RpcTarget.All);
            proteinViz.text = "HyperBall";
        }
    }

    public void LoadSurfaceReprestation()
    {
        if (photonView.IsMine)
        {
            photonView.RPC("Surface", RpcTarget.All);
            proteinViz.text = "Surface";
        }
    }


    [PunRPC]
    void Cartoon()
    {
        APIPython.reset();
        APIPython.fetch("5BKI");

        /*
        string loadedPDBName = loadedMolecue.transform.GetChild(0).gameObject.name;
        Transform atomCartoon = loadedMolecue.transform.Find(
                    loadedPDBName + "/AtomCartoonRepresentation"
                );
        for (int i = 0; i < atomCartoon.childCount; i++)
        {
            Mesh cartoonMesh = atomCartoon
                .GetChild(i)
                .gameObject.GetComponent<MeshFilter>()
                .mesh;
            if (cartoonMesh != null)
            {
                MeshCollider cartoonMeshCollider = atomCartoon
                    .GetChild(i)
                    .transform.gameObject.AddComponent<MeshCollider>();
                cartoonMeshCollider.sharedMesh = cartoonMesh;
            }
            atomCartoon.GetChild(i).gameObject.tag = "protein";
        }

        if (
            loadedMolecue.transform.Find(loadedPDBName + "/AtomRepresentationPoint") != null
        )
        {
            Transform H2O = loadedMolecue.transform.Find(
                loadedPDBName + "/AtomRepresentationPoint"
            );
            H2O.gameObject.SetActive(false);
        }

        Transform atomOptiHB = loadedMolecue.transform.Find(
            loadedPDBName + "/AtomOptiHBRepresentation"
        );
        Mesh atomOptiHBmesh = atomOptiHB
            .GetChild(0)
            .gameObject.GetComponent<MeshFilter>()
            .mesh;
        if (atomOptiHBmesh != null)
        {
            MeshCollider atomOptiHBmeshCollider = atomOptiHB
                .GetChild(0)
                .transform.gameObject.AddComponent<MeshCollider>();
            atomOptiHBmeshCollider.sharedMesh = atomOptiHBmesh;
        }
        atomOptiHB.GetChild(0).gameObject.tag = "protein";

        Transform atomOptiHS = loadedMolecue.transform.Find(
            loadedPDBName + "/BondOptiHSRepresentation"
        );
        Mesh atomOptiHSmesh = atomOptiHS
            .GetChild(0)
            .gameObject.GetComponent<MeshFilter>()
            .mesh;
        if (atomOptiHSmesh != null)
        {
            MeshCollider atomOptiHSmeshCollider = atomOptiHS
                .GetChild(0)
                .transform.gameObject.AddComponent<MeshCollider>();
            atomOptiHSmeshCollider.sharedMesh = atomOptiHSmesh;
        }

        atomOptiHS.GetChild(0).gameObject.tag = "protein";
        */
    }


    [PunRPC]
    void HyperBall()
    {
        APIPython.reset();
        APIPython.fetch("5BKI", true, true, false, false, true, true, -1, false); //the 5th argument is for default represenattion
        APIPython.showAs("hb");
        
        /*
        string loadedPDBName = loadedMolecue.transform.GetChild(0).gameObject.name;
        bool atomOptiHBRepr = loadedMolecue.transform.Find(
                    loadedPDBName + "/AtomOptiHBRepresentation"
                );
        bool bondOptiHSRepr = loadedMolecue.transform.Find(
            loadedPDBName + "/BondOptiHSRepresentation"
        );

        if (atomOptiHBRepr && bondOptiHSRepr)
        {
            Transform atomOptiHB = loadedMolecue.transform.Find(
                loadedPDBName + "/AtomOptiHBRepresentation"
            );
            for (int i = 0; i < atomOptiHB.childCount; i++)
            {
                Mesh atomOptiHBmesh = atomOptiHB
                    .GetChild(i)
                    .gameObject.GetComponent<MeshFilter>()
                    .mesh;
                if (atomOptiHBmesh != null)
                {
                    MeshCollider atomOptiHBmeshCollider = atomOptiHB
                        .GetChild(i)
                        .transform.gameObject.AddComponent<MeshCollider>();
                    atomOptiHBmeshCollider.sharedMesh = atomOptiHBmesh;
                }
                atomOptiHB.GetChild(i).gameObject.tag = "protein";
            }

            Transform atomOptiHS = loadedMolecue.transform.Find(
                loadedPDBName + "/BondOptiHSRepresentation"
            );
            for (int i = 0; i < atomOptiHS.childCount; i++)
            {
                Mesh atomOptiHSmesh = atomOptiHS
                    .GetChild(i)
                    .gameObject.GetComponent<MeshFilter>()
                    .mesh;
                if (atomOptiHSmesh != null)
                {
                    MeshCollider atomOptiHSmeshCollider = atomOptiHS
                        .GetChild(i)
                        .transform.gameObject.AddComponent<MeshCollider>();
                    atomOptiHSmeshCollider.sharedMesh = atomOptiHSmesh;
                }
                atomOptiHS.GetChild(i).gameObject.tag = "protein";
            }
        }
        */        
    }

    [PunRPC]
    void Surface()
    {
        APIPython.reset();
        APIPython.fetch("5BKI", true, true, false, false, true, true, -1, false); //the 5th argument is for default represenattion
        APIPython.showAs("s");
        

        /*
        string loadedPDBName = loadedMolecue.transform.GetChild(0).gameObject.name;

        bool atomSurfaceRepr = loadedMolecue.transform.Find(
            loadedPDBName + "/AtomSurfaceRepresentation"
        );
        if (atomSurfaceRepr)
        {
            Transform atomSurface = loadedMolecue.transform.Find(
                loadedPDBName + "/AtomSurfaceRepresentation"
            );
            for (int i = 0; i < atomSurface.childCount; i++)
            {
                Mesh atomSurfacemesh = atomSurface
                    .GetChild(i)
                    .gameObject.GetComponent<MeshFilter>()
                    .mesh;
                if (atomSurfacemesh != null)
                {
                    MeshCollider atomSurfacemeshMeshCollider = atomSurface
                        .GetChild(i)
                        .transform.gameObject.AddComponent<MeshCollider>();
                    atomSurfacemeshMeshCollider.sharedMesh = atomSurfacemesh;
                }
                atomSurface.GetChild(i).gameObject.tag = "protein";
            }
        }
        */
    }


    /*
    void Update()
    {
        if (photonView.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                photonView.RPC("LoadMolecule", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    void LoadMolecule()
    {
        //cartoon represenattaion
        APIPython.fetch("5BKI");

        //hyperball represenation
        //APIPython.fetch("5BKI", true, true, false, false, true, true, -1, false); //the 5th argument is for default represenattion
        //APIPython.showAs("hb");

        //surface represenation
        //APIPython.fetch("5BKI", true, true, false, false, true, true, -1, false); //the 5th argument is for default represenattion
        //APIPython.showAs("s");
    }
    */

}
