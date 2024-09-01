using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEditor;
using UMol.API;
using UMol;
using TMPro;
using System.IO;

namespace UnityEngine.UI.Extensions.ColorPicker
{
    public class LoadProteinPDBCartoon : MonoBehaviourPunCallbacks
    {
        private GameObject loadedMolecue;
        public TextMeshProUGUI proteinViz;
        public TextMeshProUGUI pdbName;        

        void Start()
        {
            loadedMolecue = UnityMolMain.getRepresentationParent();            
        }

        public void InputFromDropDownCartoonMenu(int val)
        {
            if (photonView.IsMine)
            {
                if (val == 1)
                {
                    photonView.RPC("Cartoon_MCTP_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Cartoon";
                    pdbName.text = "Cartoon_MCTP";                    
                }

                if (val == 2)
                {
                    photonView.RPC("Cartoon_Fzo1_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Cartoon";
                    pdbName.text = "Cartoon_Fzo1";                    
                }

                if (val == 3)
                {
                    photonView.RPC("Cartoon_NOX2_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Cartoon";
                    pdbName.text = "Cartoon_NOX2";                    
                }

                if (val == 4)
                {
                    photonView.RPC("Cartoon_PvdRT_OpmQ_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Cartoon";
                    pdbName.text = "Cartoon_PvdRT_OpmQ";                    
                }

                if (val == 5)
                {
                    photonView.RPC("Cartoon_PAR1_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Cartoon";
                    pdbName.text = "Cartoon_PAR1";                    
                }
            }
        }

        [PunRPC]
        void Cartoon_MCTP_ColorCodedByChain()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Cartoon/MCTP.pdb");
            APIPython.load(filePath);
            APIPython.colorByChain("MCTP_protein_or_nucleic", "c");
            loadedMolecue.transform.position = new Vector3(0.132f, 0.94f, 1.8f);
            loadedMolecue.transform.rotation = Quaternion.Euler(-106f, 38f, -6f);
        }

        [PunRPC]
        void Cartoon_Fzo1_ColorCodedByChain()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Cartoon/Fzo1.pdb");
            APIPython.load(filePath);
            APIPython.colorByChain("Fzo1_protein_or_nucleic", "c");
            loadedMolecue.transform.position = new Vector3(0.69f, 1.83f, 1.1f);
            loadedMolecue.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        [PunRPC]
        void Cartoon_NOX2_ColorCodedByChain()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Cartoon/NOX2.pdb");
            APIPython.load(filePath);
            APIPython.colorByChain("NOX2_protein_or_nucleic", "c");
            loadedMolecue.transform.position = new Vector3(0.27f, 1.00f, 1.81f);
            loadedMolecue.transform.rotation = Quaternion.Euler(74f, 90f, 0f);
        }

        [PunRPC]
        void Cartoon_PvdRT_OpmQ_ColorCodedByChain()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Cartoon/PvdRT_OpmQ.pdb");
            APIPython.load(filePath);
            APIPython.colorByChain("PvdRT_OpmQ_protein_or_nucleic", "c");
            loadedMolecue.transform.position = new Vector3(-0.1f, 1.14f, 1.8f);
            loadedMolecue.transform.rotation = Quaternion.Euler(158f, 0f, 2.4f);
        }

        [PunRPC]
        void Cartoon_PAR1_ColorCodedByChain()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Cartoon/PAR1.pdb");
            APIPython.load(filePath, true, false, true, false, true, -1);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand");
            APIPython.updateSelectionWithMDA("ligand", "chain A", true, false, false, false);
            APIPython.showSelection("ligand", "hb");
            APIPython.colorByChain("PAR1_protein_or_nucleic", "c");
            loadedMolecue.transform.position = new Vector3(0.3f, 0.93f, 1.9f);
            loadedMolecue.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}