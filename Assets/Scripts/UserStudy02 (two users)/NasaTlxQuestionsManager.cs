﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class NasaTlxQuestionsManager : MonoBehaviour
{
    public GameObject nasaTlxPanel;
    public TextMeshProUGUI[] questions;
    public Slider[] sliders;
    public UserStudy02Manager userStudy02Manager;

    void Start()
    {
        if (userStudy02Manager == null)
        {
            Debug.LogError("UserStudy02Manager component not found!");
        }
    }

    public void SaveNasaTlxAnswers()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, "UserStudy02");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = $"{userStudy02Manager.participantName}_NasaTlx_{userStudy02Manager.currentPDB}_{userStudy02Manager.expType}_{userStudy02Manager.proteinRepresentaionType}.csv";
        string path = Path.Combine(folderPath, fileName);

        bool fileExists = File.Exists(path);

        using (StreamWriter writer = new StreamWriter(path, true))
        {
            if (!fileExists)
            {
                writer.WriteLine("Question, Answer");
            }

            for (int i = 0; i < questions.Length; i++)
            {
                string questionText = questions[i].text;
                string singleLineQuestion = questionText.Replace("\n", " ").Replace("\r", "");
                string answer = sliders[i].value.ToString();
                writer.WriteLine($"{singleLineQuestion}, {answer}");                
            }
        }
        Debug.Log("Answers saved." + path);
        SliderReset();
        nasaTlxPanel.SetActive(false);
    }

    void SliderReset()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            sliders[i].value = sliders[i].minValue;
        }
    }

    public void CancelNasaTlxPanel()
    {
        nasaTlxPanel.SetActive(false);
    }
}
