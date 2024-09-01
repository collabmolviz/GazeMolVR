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
//using System.Windows.Forms;

public class LoadPDB : MonoBehaviourPunCallbacks
{
    public UserStudy02Manager userStudy02Manager;
    private PhotonView photonView;

    private GameObject loadedMolecue;
    private string filePath;
    public TextMeshProUGUI proteinViz;
    public TextMeshProUGUI pdbName;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
        loadedMolecue = UnityMolMain.getRepresentationParent();
    }

    public void OpenFileBrowser()
    {
        string bioMolName = userStudy02Manager.currentPDB.ToString();
        string fileName = $"{bioMolName}.pdb";

        if (userStudy02Manager.proteinRepresentaionType == UserStudy02Manager.ProteinRepresentaionType.Cartoon && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("Cartoon_ColorCodedByChain", RpcTarget.All, fileName, bioMolName);
            proteinViz.text = "Cartoon";
            pdbName.text = userStudy02Manager.currentPDB.ToString();
        }

        if (userStudy02Manager.proteinRepresentaionType == UserStudy02Manager.ProteinRepresentaionType.Surface && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("Surface_ColorCodedByChain", RpcTarget.All, fileName, bioMolName);
            proteinViz.text = "Surface";
            pdbName.text = userStudy02Manager.currentPDB.ToString();
        }

        if (userStudy02Manager.proteinRepresentaionType == UserStudy02Manager.ProteinRepresentaionType.HyperBall && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("HyperBall_ColorCodedByChain", RpcTarget.All, fileName, bioMolName);
            proteinViz.text = "HyperBall";
            pdbName.text = userStudy02Manager.currentPDB.ToString();
        }


        /*
        // Note: This code will run in the Editor or a standalone build but is not suitable for a built VR application without modification
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
        openFileDialog.FilterIndex = 2;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            filePath = openFileDialog.FileName;            
        }
        */
    }

    public string GeneratePath(string fileName)
    {
        string basePath = Application.streamingAssetsPath;
        string folderPath = "PDB Files/2ndExp";
        string fullPath = Path.Combine(basePath, folderPath, fileName);

        return fullPath;
    }

    [PunRPC]
    void Cartoon_ColorCodedByChain(string fileName, string bioMolName)
    {
        APIPython.reset();
        filePath = GeneratePath(fileName);
        Debug.Log(filePath);
        APIPython.load(filePath);

        string water = userStudy02Manager.currentPDB.ToString() + "_water";
        APIPython.deleteRepresentationInSelection(water, "p");

        string unrecognized_atoms = userStudy02Manager.currentPDB.ToString() + "_unrecognized_atoms";
        APIPython.deleteSelection(unrecognized_atoms);

        if (bioMolName == "Caroline_PDB")
        {
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resid 154 and resname ARG", true, false, false);
            APIPython.showSelection("ligand_01", "hb");

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resid 178", true, false, false);
            APIPython.showSelection("ligand_02", "hb");
        }

        if (bioMolName == "Etienne_PAR1")
        {
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_par_1");
            APIPython.updateSelectionWithMDA("ligand_par_1", "chain A", true, false, false, false);
            APIPython.showSelection("ligand_par_1", "hb");
        }

        if (bioMolName == "Maya_1KZ0")
        {
            APIPython.hideSelection("Maya_1KZ0_water", "l");
            APIPython.colorByChain("1kz0_DMPG_sim2_3000_protein_or_nucleic", "c");
        }

        string protein = userStudy02Manager.currentPDB.ToString() + "_protein_or_nucleic";
        APIPython.colorByChain(protein, "c");
        APIPython.setShadows(protein, "c", true);

        loadedMolecue.transform.position = new Vector3(0.69f, 1.83f, 1.1f);
        loadedMolecue.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    [PunRPC]
    void Surface_ColorCodedByChain(string fileName, string bioMolName)
    {
        APIPython.reset();
        filePath = GeneratePath(fileName);
        Debug.Log(filePath);
        APIPython.load(filePath);

        string water = userStudy02Manager.currentPDB.ToString() + "_water";
        APIPython.deleteRepresentationInSelection(water, "p");

        string unrecognized_atoms = userStudy02Manager.currentPDB.ToString() + "_unrecognized_atoms";
        APIPython.deleteSelection(unrecognized_atoms);

        string protein = userStudy02Manager.currentPDB.ToString() + "_protein_or_nucleic";
        APIPython.deleteRepresentationInSelection(protein, "c");
        APIPython.showSelection(protein, "s");
        APIPython.colorByChain(protein, "s");
        APIPython.setShadows(protein, "s", true);
        APIPython.setSmoothness(protein, "s", 0.15f);
        APIPython.setMetal(protein, "s", 0.23f);

        if (bioMolName == "Maya_1KZ0_2")
        {
            APIPython.hideSelection("Maya_1KZ0_2_water", "l");            
        }

        loadedMolecue.transform.position = new Vector3(0.69f, 1.83f, 1.1f);
        loadedMolecue.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    [PunRPC]
    void HyperBall_ColorCodedByChain(string fileName, string bioMolName)
    {
        APIPython.reset();
        filePath = GeneratePath(fileName);
        Debug.Log(filePath);
        APIPython.load(filePath);

        if (bioMolName == "HB_4A97")
        {
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

            APIPython.deleteSelection("HB_4A97_unrecognized_atoms");
            APIPython.deleteSelection("HB_4A97_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("HB_4A97_protein_or_nucleic", "c");

            APIPython.showSelection("HB_4A97_protein_or_nucleic", "hb");
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
            APIPython.colorByChain("HB_4A97_protein_or_nucleic", "hb");
        }

        if (bioMolName == "HB_4A98")
        {
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

            APIPython.deleteSelection("HB_4A98_unrecognized_atoms");
            APIPython.deleteSelection("HB_4A98_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("HB_4A98_protein_or_nucleic", "c");

            APIPython.showSelection("HB_4A98_protein_or_nucleic", "hb");
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
            APIPython.colorByChain("HB_4A98_protein_or_nucleic", "hb");
        }

        if (bioMolName == "HB_6X3S")
        {
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname J94 and chain A", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname J94 and chain C", true, false, false);

            APIPython.deleteSelection("HB_6X3S_unrecognized_atoms");
            APIPython.deleteSelection("HB_6X3S_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("HB_6X3S_protein_or_nucleic", "c");

            APIPython.showSelection("HB_6X3S_protein_or_nucleic", "hb");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");

            loadedMolecue.transform.position = new Vector3(1.539f, -0.3f, 0.015f);
            loadedMolecue.transform.rotation = Quaternion.Euler(0, 0, 0);
            APIPython.colorByChain("HB_6X3S_protein_or_nucleic", "hb");
        }

        if (bioMolName == "HB_6X3U")
        {
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname FYP and chain D", true, false, false);

            APIPython.deleteSelection("HB_6X3U_unrecognized_atoms");
            APIPython.deleteSelection("HB_6X3U_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("HB_6X3U_protein_or_nucleic", "c");

            APIPython.showSelection("HB_6X3U_protein_or_nucleic", "hb");
            APIPython.showSelection("ligand_01", "hb");

            loadedMolecue.transform.position = new Vector3(1.539f, -0.3f, 0.015f);
            loadedMolecue.transform.rotation = Quaternion.Euler(0, 0, 0);
            APIPython.colorByChain("HB_6X3U_protein_or_nucleic", "hb");
        }

        if (bioMolName == "HB_6X3Z")
        {
            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_01");
            APIPython.updateSelectionWithMDA("ligand_01", "resname ABU and chain A", true, false, false);

            APIPython.select("nothing", "newSelection", true, false, false, true, true, false, true);
            APIPython.renameSelection("newSelection", "ligand_02");
            APIPython.updateSelectionWithMDA("ligand_02", "resname ABU and chain C", true, false, false);

            APIPython.deleteSelection("HB_6X3Z_unrecognized_atoms");
            APIPython.deleteSelection("HB_6X3Z_not_protein_nucleic");
            APIPython.deleteRepresentationInSelection("HB_6X3Z_protein_or_nucleic", "c");

            APIPython.showSelection("HB_6X3Z_protein_or_nucleic", "hb");
            APIPython.showSelection("ligand_01", "hb");
            APIPython.showSelection("ligand_02", "hb");

            loadedMolecue.transform.position = new Vector3(1.539f, -0.3f, 0.015f);
            loadedMolecue.transform.rotation = Quaternion.Euler(0, 0, 0);
            APIPython.colorByChain("HB_6X3Z_protein_or_nucleic", "hb");
        }
    }
}
