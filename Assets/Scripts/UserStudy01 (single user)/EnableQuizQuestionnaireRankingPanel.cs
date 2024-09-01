using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableQuizQuestionnaireRankingPanel : MonoBehaviour
{
    public GameObject QuizPanel;
    public GameObject MAQPanel;
    public GameObject RankingPanel;

    public void EnableQuizPanel()
    {
        QuizPanel.SetActive(true);
        QuizPanel.transform.position = new Vector3(-1.845f, 1.356f, 1.9f);
        QuizPanel.transform.eulerAngles = new Vector3(0f, -31f, 0f);
    }

    public void EnableMAQPanel()
    {
        MAQPanel.SetActive(true);
        MAQPanel.transform.position = new Vector3(-1.845f, 1.356f, 1.9f);
        MAQPanel.transform.eulerAngles = new Vector3(0f, -31f, 0f);
    }

    public void EnableGazeRankingPanel()
    {
        RankingPanel.SetActive(true);
        RankingPanel.transform.position = new Vector3(-1.845f, 1.356f, 1.9f);
        RankingPanel.transform.eulerAngles = new Vector3(0f, -31f, 0f);
    }
}
