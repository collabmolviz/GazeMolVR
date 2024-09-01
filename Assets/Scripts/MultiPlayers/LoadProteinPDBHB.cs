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
    public class LoadProteinPDBHB : MonoBehaviourPunCallbacks
    {
        private GameObject loadedMolecue;
        public TextMeshProUGUI proteinViz;
        public TextMeshProUGUI pdbName;


        void Start()
        {
            loadedMolecue = UnityMolMain.getRepresentationParent();
        }

        public void InputFromDropDownHBMenu(int val)
        {
            if (photonView.IsMine)
            {
                if (val == 1)
                {
                    photonView.RPC("HB_6X3Z_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "HyperBall";
                    pdbName.text = "HyperBall_6X3Z";
                }

                if (val == 2)
                {
                    photonView.RPC("HB_6X3S_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "HyperBall";
                    pdbName.text = "HyperBall_6X3S";
                }

                if (val == 3)
                {
                    photonView.RPC("HB_4A98_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "HyperBall";
                    pdbName.text = "HyperBall_4A98";
                }

                if (val == 4)
                {
                    photonView.RPC("HB_6X3U_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "HyperBall";
                    pdbName.text = "HyperBall_6X3U";
                }

                if (val == 5)
                {
                    photonView.RPC("HB_4A97_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "HyperBall";
                    pdbName.text = "HyperBall_4A97";
                }
            }
        }

        [PunRPC]
        void HB_6X3Z()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/HB/6X3Z.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname ABU and chain A", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname ABU and chain C", true, false, false);

            APIPython.deleteSelection("6X3Z_unrecognized_atoms");
            APIPython.deleteSelection("6X3Z_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("6X3Z_protein_or_nucleic", "c");

            APIPython.showSelection("6X3Z_protein_or_nucleic", "hb");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");

            loadedMolecue.transform.position = new Vector3(1.539f, -0.3f, 0.015f);
            loadedMolecue.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        [PunRPC]
        void HB_6X3Z_ColorCodedByChain()
        {
            HB_6X3Z();
            APIPython.colorByChain("6X3Z_protein_or_nucleic", "hb");
        }

        [PunRPC]
        void HB_6X3S()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/HB/6X3S.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname J94 and chain A", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname J94 and chain C", true, false, false);

            APIPython.deleteSelection("6X3S_unrecognized_atoms");
            APIPython.deleteSelection("6X3S_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("6X3S_protein_or_nucleic", "c");

            APIPython.showSelection("6X3S_protein_or_nucleic", "hb");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");

            loadedMolecue.transform.position = new Vector3(1.539f, -0.3f, 0.015f);
            loadedMolecue.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        [PunRPC]
        void HB_6X3S_ColorCodedByChain()
        {
            HB_6X3S();
            APIPython.colorByChain("6X3S_protein_or_nucleic", "hb");
        }

        [PunRPC]
        void HB_4A98()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/HB/4A98.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname BFZ and chain A", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname BFZ and chain B", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_03");
            APIPython.updateSelectionWithMDA("ligand_03", "resname BFZ and chain C", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_04");
            APIPython.updateSelectionWithMDA("ligand_04", "resname BFZ and chain D", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_05");
            APIPython.updateSelectionWithMDA("ligand_05", "resname BFZ and chain E", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_06");
            APIPython.updateSelectionWithMDA("ligand_06", "resname BFZ and chain F", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_07");
            APIPython.updateSelectionWithMDA("ligand_07", "resname BFZ and chain G", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_08");
            APIPython.updateSelectionWithMDA("ligand_08", "resname BFZ and chain H", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_09");
            APIPython.updateSelectionWithMDA("ligand_09", "resname BFZ and chain I", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_10");
            APIPython.updateSelectionWithMDA("ligand_10", "resname BFZ and chain J", true, false, false);

            APIPython.deleteSelection("4A98_unrecognized_atoms");
            APIPython.deleteSelection("4A98_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("4A98_protein_or_nucleic", "c");

            APIPython.showSelection("4A98_protein_or_nucleic", "hb");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");
            APIPython.showSelection("ligand_03", "hb");
            APIPython.showSelection("ligand_04", "hb");
            APIPython.showSelection("ligand_05", "hb");
            APIPython.showSelection("ligand_06", "hb");
            APIPython.showSelection("ligand_07", "hb");
            APIPython.showSelection("ligand_08", "hb");
            APIPython.showSelection("ligand_09", "hb");
            APIPython.showSelection("ligand_10", "hb");

            loadedMolecue.transform.position = new Vector3(-0.54f, 1.71f, 1.68f);
            loadedMolecue.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        [PunRPC]
        void HB_4A98_ColorCodedByChain()
        {
            HB_4A98();
            APIPython.colorByChain("4A98_protein_or_nucleic", "hb");
        }

        [PunRPC]
        void HB_6X3U()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/HB/6X3U.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname FYP and chain D", true, false, false);

            APIPython.deleteSelection("6X3U_unrecognized_atoms");
            APIPython.deleteSelection("6X3U_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("6X3U_protein_or_nucleic", "c");

            APIPython.showSelection("6X3U_protein_or_nucleic", "hb");
            APIPython.showSelection("ligand_01", "hb");

            loadedMolecue.transform.position = new Vector3(1.539f, -0.3f, 0.015f);
            loadedMolecue.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        [PunRPC]
        void HB_6X3U_ColorCodedByChain()
        {
            HB_6X3U();
            APIPython.colorByChain("6X3U_protein_or_nucleic", "hb");
        }

        [PunRPC]
        void HB_4A97()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/HB/4A97.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname ZPC and chain A", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname ZPC and chain B", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_03");
            APIPython.updateSelectionWithMDA("ligand_03", "resname ZPC and chain C", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_04");
            APIPython.updateSelectionWithMDA("ligand_04", "resname ZPC and chain D", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_05");
            APIPython.updateSelectionWithMDA("ligand_05", "resname ZPC and chain E", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_06");
            APIPython.updateSelectionWithMDA("ligand_06", "resname ZPC and chain F", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_07");
            APIPython.updateSelectionWithMDA("ligand_07", "resname ZPC and chain G", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_08");
            APIPython.updateSelectionWithMDA("ligand_08", "resname ZPC and chain H", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_09");
            APIPython.updateSelectionWithMDA("ligand_09", "resname ZPC and chain I", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_10");
            APIPython.updateSelectionWithMDA("ligand_10", "resname ZPC and chain J", true, false, false);

            APIPython.deleteSelection("4A97_unrecognized_atoms");
            APIPython.deleteSelection("4A97_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("4A97_protein_or_nucleic", "c");

            APIPython.showSelection("4A97_protein_or_nucleic", "hb");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");
            APIPython.showSelection("ligand_03", "hb");
            APIPython.showSelection("ligand_04", "hb");
            APIPython.showSelection("ligand_05", "hb");
            APIPython.showSelection("ligand_06", "hb");
            APIPython.showSelection("ligand_07", "hb");
            APIPython.showSelection("ligand_08", "hb");
            APIPython.showSelection("ligand_09", "hb");
            APIPython.showSelection("ligand_10", "hb");

            loadedMolecue.transform.position = new Vector3(-0.54f, 1.71f, 1.68f);
            loadedMolecue.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        [PunRPC]
        void HB_4A97_ColorCodedByChain()
        {
            HB_4A97();
            APIPython.colorByChain("4A97_protein_or_nucleic", "hb");
        }
    }
}