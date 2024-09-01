using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class EyeGazeRankingManager : MonoBehaviour
{
    public RecordAndReplay recordAndReplay;

    public TextMeshProUGUI proteinViz;
    public GameObject rankingPanel;
    public TextMeshProUGUI[] questions;
    public Slider[] sliders;

    public void SaveRankingAnswers()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, "UserStudy01QuantitativeData");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = recordAndReplay.participantName + "_QuantitativeData" + ".csv";
        string path = Path.Combine(folderPath, fileName);

        bool fileExists = File.Exists(path);

        using (StreamWriter writer = new StreamWriter(path, true))
        {
            if (!fileExists)
            {
                writer.WriteLine("Label, Question, Answer");
            }

            for (int i = 0; i < questions.Length; i++)
            {
                string label = proteinViz.text;
                string question = questions[i].text;
                string answer = sliders[i].value.ToString();

                writer.WriteLine($"{label}, {question}, {answer}");
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
