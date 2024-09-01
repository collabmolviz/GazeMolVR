using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class QuestionnaireManager : MonoBehaviour
{
    public RecordAndReplay recordAndReplay;

    public TextMeshProUGUI pdbName;
    public TextMeshProUGUI eyegazeViz;
    public GameObject nasaTLXPanel;

    public TextMeshProUGUI[] questions;
    public Slider[] sliders;

    public void SaveAnswers()
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
                string label = recordAndReplay.currentPDB.ToString() + " x " + recordAndReplay.currentEyeGaze.ToString();

                string questionText = questions[i].text;
                string singleLineQuestion = questionText.Replace("\n", " ").Replace("\r", "");

                string answer = sliders[i].value.ToString();

                writer.WriteLine($"{label}, {singleLineQuestion}, {answer}");
            }
        }

        Debug.Log("Questionnaire completed.");
        SliderReset();
        nasaTLXPanel.SetActive(false);
    }

    void SliderReset()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            sliders[i].value = sliders[i].minValue;
        }
    }

    public void CancelQPanel()
    {
        nasaTLXPanel.SetActive(false);
    }
}
