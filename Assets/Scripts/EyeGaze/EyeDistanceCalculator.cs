using Photon.Pun;
using UnityEngine;
using TMPro;
using System.IO; // For StreamWriter
using System.Text;
using System; // For StringBuilder

public class EyeDistanceCalculator : MonoBehaviour
{
    private TextMeshProUGUI debugText;
    private StringBuilder eyeDataStringBuilder;

    private DataSynchronization localEyeData;
    private DataSynchronization remoteEyeData;

    public UserStudy02Manager userStudy02Manager;

    private bool shouldCalculateOverlap;
    private bool saveResultFlag;

    void Start()
    {
        debugText = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();

        shouldCalculateOverlap = false;
        saveResultFlag = false;

        eyeDataStringBuilder = new StringBuilder();

        if (userStudy02Manager == null)
        {
            Debug.LogError("UserStudy02Manager component not found!");
        }
    }

    public void OnStartButtonClick()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Start Button clicked!");
            shouldCalculateOverlap = true;
            saveResultFlag = false;
        }
        else
        {
            Debug.Log("You are not the master client. Only the master client can start the experiment.");
        }
    }

    public void OnEndButtonClick()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("End Button clicked!");
            shouldCalculateOverlap = false;
            saveResultFlag = true;
        }
        else
        {
            Debug.Log("You are not the master client. Only the master client can end the experiment.");
        }
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient && userStudy02Manager)
        {
            if (shouldCalculateOverlap)
            {
                CheckGazeOverlap();
            }
            if (saveResultFlag)
            {
                SaveGazeData();
                saveResultFlag = false;
            }
        }
    }

    private void CheckGazeOverlap()
    {
        GameObject localPlayer = GameObject.FindGameObjectWithTag("LocalPlayer");
        if (localPlayer)
        {
            localEyeData = localPlayer.GetComponent<DataSynchronization>();
        }

        GameObject remotePlayer = GameObject.FindGameObjectWithTag("RemotePlayer");
        if (remotePlayer)
        {
            remoteEyeData = remotePlayer.GetComponent<DataSynchronization>();
        }

        if (localEyeData && remoteEyeData)
        {
            if (localEyeData.lookingAtProteinFlag && remoteEyeData.lookingAtProteinFlag)
            {
                Vector3 localEyePos = localEyeData.latestEyePosition;
                Vector3 remoteEyePos = remoteEyeData.latestEyePosition;
                float distance = Vector3.Distance(localEyePos, remoteEyePos);
                debugText.text = "EyeGaze Distance: " + distance;
                eyeDataStringBuilder.AppendLine($"{Time.time},{localEyePos.x},{localEyePos.y},{localEyePos.z},{remoteEyePos.x},{remoteEyePos.y},{remoteEyePos.z},{distance}");
            }
        }
    }

    private void SaveGazeData()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, "UserStudy02");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = $"EyeData_{userStudy02Manager.currentPDB}_{userStudy02Manager.expType}_{userStudy02Manager.proteinRepresentaionType}.csv";
        string path = Path.Combine(folderPath, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine("Time,LocalEyePositionX,LocalEyePositionY,LocalEyePositionZ,RemoteEyePositionX,RemoteEyePositionY,RemoteEyePositionZ,Distance");
                writer.Write(eyeDataStringBuilder.ToString());
            }
            Debug.Log("Gaze data saved to: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("Error writing to the file: " + e.Message);
        }
        eyeDataStringBuilder.Clear();
    }
}
