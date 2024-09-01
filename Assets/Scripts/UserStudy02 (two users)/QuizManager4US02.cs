using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class QuizManager4US02 : MonoBehaviour
{
    public UserStudy02Manager userStudy02Manager;
    public GameObject quizPanel;

    private PhotonView photonView;

    [Space(8)]
    public TextMeshProUGUI Question1;
    public ToggleGroup toggleGroup1;
    public Toggle Q1Option1;
    public Toggle Q1Option2;
    public Toggle Q1Option3;
    public Toggle Q1Option4;

    [Space(8)]
    public TextMeshProUGUI Question2;
    public ToggleGroup toggleGroup2;
    public Toggle Q2Option1;
    public Toggle Q2Option2;
    public Toggle Q2Option3;
    public Toggle Q2Option4;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    public void EnableQuizPanel()
    {
        photonView.RPC("loadQuizPanel", RpcTarget.All);
    }

    [PunRPC]
    void loadQuizPanel()
    {
        quizPanel.SetActive(true);

        if (userStudy02Manager.currentPDB == UserStudy02Manager.PDBList.None)
        {
            /*
            Question1.text = "This is sample question about MCTP:";
            Q1Option1.GetComponentInChildren<TextMeshProUGUI>().text = "MCTP 01.1";
            Q1Option2.GetComponentInChildren<TextMeshProUGUI>().text = "MCTP 02.1";
            Q1Option3.GetComponentInChildren<TextMeshProUGUI>().text = "MCTP 03.1";
            Q1Option4.GetComponentInChildren<TextMeshProUGUI>().text = "MCTP 04.1";


            Question2.text = "This is another question about MCTP:";
            Q2Option1.GetComponentInChildren<TextMeshProUGUI>().text = "MCTP 01.2";
            Q2Option2.GetComponentInChildren<TextMeshProUGUI>().text = "MCTP 02.2";
            Q2Option3.GetComponentInChildren<TextMeshProUGUI>().text = "MCTP 03.2";
            Q2Option4.GetComponentInChildren<TextMeshProUGUI>().text = "MCTP 04.2";
            */
        }        
    }

    public void SaveAnswers()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            string question1 = Question1.text;
            string question2 = Question2.text;
            string answer1 = "No Answer Selected";  // Default value if no toggle is selected
            string answer2 = "No Answer Selected";  // Default value if no toggle is selected

            foreach (Toggle toggle in toggleGroup1.ActiveToggles())
            {
                if (toggle.isOn)
                {
                    TextMeshProUGUI answerText = toggle.GetComponentInChildren<TextMeshProUGUI>();
                    answer1 = answerText.text;
                    break;  // Assuming only one toggle can be active at a time
                }
            }

            foreach (Toggle toggle in toggleGroup2.ActiveToggles())
            {
                if (toggle.isOn)
                {
                    TextMeshProUGUI answerText = toggle.GetComponentInChildren<TextMeshProUGUI>();
                    answer2 = answerText.text;
                    break;  // Assuming only one toggle can be active at a time
                }
            }

            string folderPath = Path.Combine(Application.streamingAssetsPath, "UserStudy02");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = $"{userStudy02Manager.participantName}_{userStudy02Manager.expType}_{userStudy02Manager.currentPDB}_Quiz.csv";
            string path = Path.Combine(folderPath, fileName);
            bool fileExists = File.Exists(path);
            using (StreamWriter sw = new StreamWriter(path, true))
            {
                if (!fileExists)
                {
                    sw.WriteLine("Participant, PDB, ExpType, Question, Answer");
                }
                sw.WriteLine($"{userStudy02Manager.participantName}, {userStudy02Manager.currentPDB}, {userStudy02Manager.expType}, {question1}, {answer1}");
                sw.WriteLine($"{userStudy02Manager.participantName}, {userStudy02Manager.currentPDB}, {userStudy02Manager.expType}, {question2}, {answer2}");
            }

            Debug.Log("Quiz completed.");
            quizPanel.SetActive(false);
        }
        else
        {
            Debug.Log("You are not the master client. Only the master client can submit it.");
        }
    }


    public void CancelQuizPanel()
    {
        photonView.RPC("DisableQuizPanel", RpcTarget.All);
    }

    [PunRPC]
    void DisableQuizPanel()
    {
        quizPanel.SetActive(false);
    }
}