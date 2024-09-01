using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class EnableRankingAndMAQPanel : MonoBehaviourPun
{
    public GameObject collaborationQuestionsFirstPanel;
    public GameObject collaborationQuestionsSecondPanel;
    public GameObject nasaTlxPanel;

    public void EnableCollaborationQuestionsFirstPanel()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            collaborationQuestionsFirstPanel.SetActive(true);
            collaborationQuestionsFirstPanel.transform.position = new Vector3(2.624f, 1.356f, 0.984f);
            collaborationQuestionsFirstPanel.transform.eulerAngles = new Vector3(0f, 40f, 0f);
        }
        else
        {
            collaborationQuestionsFirstPanel.SetActive(true);
            collaborationQuestionsFirstPanel.transform.position = new Vector3(-1.845f, 1.356f, 1.9f);
            collaborationQuestionsFirstPanel.transform.eulerAngles = new Vector3(0f, -31f, 0f);
        }
    }

    public void EnableCollaborationQuestionsSecondPanel()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            collaborationQuestionsSecondPanel.SetActive(true);
            collaborationQuestionsSecondPanel.transform.position = new Vector3(2.624f, 1.356f, 0.984f);
            collaborationQuestionsSecondPanel.transform.eulerAngles = new Vector3(0f, 40f, 0f);
        }
        else
        {
            collaborationQuestionsSecondPanel.SetActive(true);
            collaborationQuestionsSecondPanel.transform.position = new Vector3(-1.845f, 1.356f, 1.9f);
            collaborationQuestionsSecondPanel.transform.eulerAngles = new Vector3(0f, -31f, 0f);
        }
    }


    public void EnableNasaTlxPanel()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            nasaTlxPanel.SetActive(true);
            nasaTlxPanel.transform.position = new Vector3(2.624f, 1.356f, 0.984f);
            nasaTlxPanel.transform.eulerAngles = new Vector3(0f, 40f, 0f);
        }
        else
        {
            nasaTlxPanel.SetActive(true);
            nasaTlxPanel.transform.position = new Vector3(-1.845f, 1.356f, 1.9f);
            nasaTlxPanel.transform.eulerAngles = new Vector3(0f, -31f, 0f);
        }
    }
}
