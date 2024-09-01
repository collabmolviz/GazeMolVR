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
    public class LoadProteinPDBSurface : MonoBehaviourPunCallbacks
    {
        private GameObject loadedMolecue;
        public TextMeshProUGUI proteinViz;
        public TextMeshProUGUI pdbName;

        void Start()
        {
            loadedMolecue = UnityMolMain.getRepresentationParent();
        }

        public void InputFromDropDownSurfaceMenu(int val)
        {
            if (photonView.IsMine)
            {
                if (val == 1)
                {
                    photonView.RPC("Surface_6X3T_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Surface";
                    pdbName.text = "Surface_6X3T";
                }

                if (val == 2)
                {
                    photonView.RPC("Surface_6X3V_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Surface";
                    pdbName.text = "Surface_6X3V";
                }

                if (val == 3)
                {
                    photonView.RPC("Surface_6X3W_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Surface";
                    pdbName.text = "Surface_6X3W";
                }

                if (val == 4)
                {
                    photonView.RPC("Surface_6X3X_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Surface";
                    pdbName.text = "Surface_6X3X";
                }

                if (val == 5)
                {
                    photonView.RPC("Surface_6X40_ColorCodedByChain", RpcTarget.All);
                    proteinViz.text = "Surface";
                    pdbName.text = "Surface_6X40";
                }
            }
        }

        [PunRPC]
        void Surface6X3T()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Surface/6x3t.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname ABU and chain A", true, false, false);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname ABU and chain C", true, false, false);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_03");
            APIPython.updateSelectionWithMDA("ligand_03", "resname PFL and chain B", true, false, false);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_04");
            APIPython.updateSelectionWithMDA("ligand_04", "resname PFL and chain D", true, false, false);
            APIPython.deleteSelection("6x3t_unrecognized_atoms");
            APIPython.deleteSelection("6x3t_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("6x3t_protein_or_nucleic", "c");
            APIPython.showSelection("6x3t_protein_or_nucleic", "s");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");
            APIPython.showSelection("ligand_03", "hb");
            APIPython.showSelection("ligand_04", "hb");

            loadedMolecue.transform.position = new Vector3(1.5f, -0.13f, 0.44f);
            loadedMolecue.transform.rotation = Quaternion.Euler(-90f, 90f, 0f);
        }

        [PunRPC]
        void Surface_6X3T_ColorCodedByChain()
        {
            Surface6X3T();
            APIPython.colorByChain("6x3t_protein_or_nucleic", "s");
        }

        [PunRPC]
        void Surface6X3V()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Surface/6x3v.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname ABU and chain A", true, false, false);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname ABU and chain C", true, false, false);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_03");
            APIPython.updateSelectionWithMDA("ligand_03", "resname V8D and chain A", true, false, false);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_04");
            APIPython.updateSelectionWithMDA("ligand_04", "resname V8D and chain C", true, false, false);
            APIPython.deleteSelection("6x3v_unrecognized_atoms");
            APIPython.deleteSelection("6x3v_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("6x3v_protein_or_nucleic", "c");
            APIPython.showSelection("6x3v_protein_or_nucleic", "s");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");
            APIPython.showSelection("ligand_03", "hb");
            APIPython.showSelection("ligand_04", "hb");

            loadedMolecue.transform.position = new Vector3(1.5f, -0.13f, 0.44f);
            loadedMolecue.transform.rotation = Quaternion.Euler(-90f, 90f, 0f);
        }

        [PunRPC]
        void Surface_6X3V_ColorCodedByChain()
        {
            Surface6X3V();
            APIPython.colorByChain("6x3v_protein_or_nucleic", "s");
        }

        [PunRPC]
        void Surface6X3W()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Surface/6x3w.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname ABU and chain A", true, false, false);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname ABU and chain C", true, false, false);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_03");
            APIPython.updateSelectionWithMDA("ligand_03", "resname UQA and chain C", true, false, false);
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_04");
            APIPython.updateSelectionWithMDA("ligand_04", "resname UQA and chain E", true, false, false);
            APIPython.deleteSelection("6x3w_unrecognized_atoms");
            APIPython.deleteSelection("6x3w_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("6x3w_protein_or_nucleic", "c");
            APIPython.showSelection("6x3w_protein_or_nucleic", "s");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");
            APIPython.showSelection("ligand_03", "hb");
            APIPython.showSelection("ligand_04", "hb");

            loadedMolecue.transform.position = new Vector3(1.5f, -0.13f, 0.44f);
            loadedMolecue.transform.rotation = Quaternion.Euler(-90f, 90f, 0f);
        }

        [PunRPC]
        void Surface_6X3W_ColorCodedByChain()
        {
            Surface6X3W();
            APIPython.colorByChain("6x3w_protein_or_nucleic", "s");
        }

        [PunRPC]
        void Surface6X3X()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Surface/6x3x.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname ABU and chain A", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname ABU and chain C", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_03");
            APIPython.updateSelectionWithMDA("ligand_03", "resname DZP and chain C", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_04");
            APIPython.updateSelectionWithMDA("ligand_04", "resname DZP and chain E", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_05");
            APIPython.updateSelectionWithMDA("ligand_05", "resname DZP and chain A", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_06");
            APIPython.updateSelectionWithMDA("ligand_06", "resname DZP and chain D", true, false, false);

            APIPython.deleteSelection("6x3x_unrecognized_atoms");
            APIPython.deleteSelection("6x3x_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("6x3x_protein_or_nucleic", "c");
            APIPython.showSelection("6x3x_protein_or_nucleic", "s");

            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");
            APIPython.showSelection("ligand_03", "hb");
            APIPython.showSelection("ligand_04", "hb");
            APIPython.showSelection("ligand_05", "hb");
            APIPython.showSelection("ligand_06", "hb");

            loadedMolecue.transform.position = new Vector3(1.5f, -0.13f, 0.44f);
            loadedMolecue.transform.rotation = Quaternion.Euler(-90f, 90f, 0f);
        }

        [PunRPC]
        void Surface_6X3X_ColorCodedByChain()
        {
            Surface6X3X();
            APIPython.colorByChain("6x3x_protein_or_nucleic", "s");
        }

        [PunRPC]
        void Surface6X40()
        {
            APIPython.reset();
            string filePath = Path.Combine(Application.streamingAssetsPath, "PDB Files/Surface/6x40.pdb");
            APIPython.load(filePath);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname ABU and chain A", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname ABU and chain C", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_03");
            APIPython.updateSelectionWithMDA("ligand_03", "resname RI5 and chain D", true, false, false);

            APIPython.deleteSelection("6x40_unrecognized_atoms");
            APIPython.deleteSelection("6x40_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("6x40_protein_or_nucleic", "c");
            APIPython.showSelection("6x40_protein_or_nucleic", "s");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");
            APIPython.showSelection("ligand_03", "hb");

            loadedMolecue.transform.position = new Vector3(1.5f, -0.13f, 0.44f);
            loadedMolecue.transform.rotation = Quaternion.Euler(-90f, 90f, 0f);
        }

        [PunRPC]
        void Surface_6X40_ColorCodedByChain()
        {
            Surface6X40();
            APIPython.colorByChain("6x40_protein_or_nucleic", "s");
        }
    }
}