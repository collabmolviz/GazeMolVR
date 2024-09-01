using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class RankingGazeManager : MonoBehaviour
{
    public TextMeshProUGUI proteinViz;
    public GameObject rankingPanel;
    public TextMeshProUGUI[] questions;
    public Slider[] sliders;

    public void SaveRankingAnswers()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, "RankingFolder");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = proteinViz.text + "_Eye-Gaze_Ranking_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
        string path = Path.Combine(folderPath, fileName);

        using (StreamWriter writer = new StreamWriter(path, false))
        {
            for (int i = 0; i < questions.Length; i++)
            {
                // Write the question and answer as comma-separated values
                writer.WriteLine(questions[i].text + "," + sliders[i].value.ToString());
            }
        }

        Debug.Log("Ranking Answers saved.");
        SliderReset();
        rankingPanel.SetActive(false);
    }

    void SliderReset()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            sliders[i].value = sliders[i].minValue;
        }
    }

    public void CancelRankingPanel()
    {
        rankingPanel.SetActive(false);
    }
}
