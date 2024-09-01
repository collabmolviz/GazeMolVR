using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class RecordAndReplay : MonoBehaviour
{
    public ControlButtonEvents controlButtonEvents;

    public enum EyeGazeVizType
    {
        GazePoint,
        GazeArrow,
        GazeTrail,
        GazeSpotlight,
        None
    }

    public enum PDBList
    {
        Cartoon_MCTP,
        Cartoon_Fzo1,
        Cartoon_NOX2,
        Cartoon_PvdRTOpmQ,
        Cartoon_PAR1,
        HB_6X3Z,
        HB_6X3S,
        HB_4A98,
        HB_6X3U,
        HB_4A97,
        Surface_6X3T,
        Surface_6X3V,
        Surface_6X3W,
        Surface_6X3X,
        Surface_6X40
    }

    public string participantName;
    public EyeGazeVizType currentEyeGaze;
    public PDBList currentPDB;

    public Transform eyePos; // Still unused ??

    [System.Serializable]
    public class UnityMolEyeGazeState
    {
        // This part is for the molecule transform except scaling
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        // This part is for all the game-objects associated with the Player called "Teacher"
        public Vector3 avatarPos;
        public Quaternion avatarRot;
        public Vector3 leftControllerPos;
        public Quaternion leftControllerRot;
        public Vector3 rightControllerPos;
        public Quaternion rightControllerRot;

        public Vector3 avatarGazePos;
        public Quaternion avatarGazeRot;

        public float timeDifference;
        public float timestamp;
    }

    public GameObject teacherAvatar;
    public GameObject teacherLeftController;
    public GameObject teacherRightController;
    public GameObject teacherGazePointer;
    public GameObject teacherGazeArrow;
    public GameObject teacherGazeTrail;
    public GameObject teacherParticleTrail; // testing this
    public GameObject teacherGazePointLight;
    public GameObject teacherGazeSpotlight;

    private Vector3 activeChildPosUnderTeacherPlayer;
    private Quaternion activeChildRotUnderTeacherPlayer;

    public Transform loadedMolecule;
    private Transform molTransform;
    private string molName;
    private List<UnityMolEyeGazeState> recordedStates = new List<UnityMolEyeGazeState>();

    private string recordedScenePath;
    private bool hasExecutedSaveOnce = false;

    private string audioFilePath;
    private int audioSampleRate = 44100; // CD quality sample rate
    private AudioClip recordedAudio;
    private bool isRecordingAudio = false;
    private float audioStartTimestamp;

    [HideInInspector]
    public string folderPath;
    private string fileName;


    void Start()
    {
        currentEyeGaze = EyeGazeVizType.GazePoint;
        currentPDB = PDBList.Cartoon_MCTP;

        folderPath = Path.Combine(Application.streamingAssetsPath, "UserStudy01RecordSceneInfo");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    void Update()
    {
        GameObject teacherPlayer = GameObject.FindGameObjectWithTag("Teacher"); // Instantiated Player
        if (teacherPlayer != null)
        {
            for (int i = 0; i < teacherPlayer.transform.childCount; i++)
            {
                Transform child = teacherPlayer.transform.GetChild(i);
                if (child.gameObject.activeInHierarchy)
                {
                    if (child.gameObject.name == "ParticleTrail2Sphere")
                    {
                        // do something if needed
                    }

                    activeChildPosUnderTeacherPlayer = child.position;
                    activeChildRotUnderTeacherPlayer = child.rotation;
                }
            }
        }

        if (controlButtonEvents.deactiveGameObjectFlag)
        {
            //teacherAvatar.SetActive(false);
            //teacherLeftController.SetActive(false);
            //teacherRightController.SetActive(false);
            teacherGazePointer.SetActive(false);
            teacherGazeArrow.SetActive(false);
            teacherGazeTrail.SetActive(false);
            //teacherGazeTrail.transform.position = new Vector3(0, -1000, 0); // Note: disabling and enabling are not working for particle system
            teacherGazePointLight.SetActive(false);
            teacherGazeSpotlight.SetActive(false);
            resetLight();
        }


        if (loadedMolecule.childCount > 0)
        {
            molName = loadedMolecule.GetChild(0).transform.name;
        }

        if (GameObject.Find(molName) != null)
        {
            molTransform = GameObject.Find(molName).transform;
        }

        // Clicked START button in VR
        if (controlButtonEvents != null && controlButtonEvents.currentSceneState == ControlButtonEvents.SceneStates.Record)
        {
            if (molTransform != null && teacherPlayer != null)
            {
                if (!isRecordingAudio)  // Start audio recording only if it's not already recording
                {
                    StartAudioRecording();
                }

                Vector3 parentWorldScale = molTransform.parent.lossyScale;
                Vector3 localScale = molTransform.localScale;
                Vector3 worldScale = new Vector3(
                    parentWorldScale.x * localScale.x,
                    parentWorldScale.y * localScale.y,
                    parentWorldScale.z * localScale.z
                );

                UnityMolEyeGazeState state = new UnityMolEyeGazeState
                {
                    position = molTransform.position,
                    rotation = molTransform.rotation,
                    scale = worldScale,

                    avatarPos = teacherPlayer.transform.GetChild(0).transform.position,
                    avatarRot = teacherPlayer.transform.GetChild(0).transform.rotation,
                    leftControllerPos = teacherPlayer.transform.GetChild(1).transform.position,
                    leftControllerRot = teacherPlayer.transform.GetChild(1).transform.rotation,
                    rightControllerPos = teacherPlayer.transform.GetChild(2).transform.position,
                    rightControllerRot = teacherPlayer.transform.GetChild(2).transform.rotation,

                    avatarGazePos = activeChildPosUnderTeacherPlayer,
                    avatarGazeRot = activeChildRotUnderTeacherPlayer,

                    timeDifference = Time.deltaTime,
                    timestamp = Time.time - audioStartTimestamp  // Relative timestamp
                };

                recordedStates.Add(state);
            }
            hasExecutedSaveOnce = false;
        }

        // Clicked STOP button in VR -> Save all data -> Run this condition only once
        if (controlButtonEvents != null && controlButtonEvents.currentSceneState == ControlButtonEvents.SceneStates.Save)
        {
            if (!hasExecutedSaveOnce)
            {
                hasExecutedSaveOnce = true;
                SaveSceneRecording();
                Debug.Log("Saved Recordings at " + recordedScenePath);
            }
        }
    }

    void resetLight()
    {
        GameObject atomOptiHBRep = GameObject.Find("AtomOptiHBRepresentation");
        if (atomOptiHBRep != null)
        {
            int childCount = atomOptiHBRep.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = atomOptiHBRep.transform.GetChild(i);
                GameObject childGameObject = childTransform.gameObject;

                Renderer renderer = childGameObject.GetComponent<Renderer>();
                Material material = renderer.material;

                material.SetVector("_PointLightPosition0", new Vector4(0, -1000, 0, 1));
                material.SetColor("_PointLightColor0", teacherGazePointLight.transform.GetChild(0).GetComponent<Light>().color);
                material.SetFloat("_PointLightRadius0", teacherGazePointLight.transform.GetChild(0).GetComponent<Light>().range);
            }
        }

        GameObject bondOptiHSRep = GameObject.Find("BondOptiHSRepresentation");
        if (bondOptiHSRep != null)
        {
            int childCount = bondOptiHSRep.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = bondOptiHSRep.transform.GetChild(i);
                GameObject childGameObject = childTransform.gameObject;

                Renderer renderer = childGameObject.GetComponent<Renderer>();
                Material material = renderer.material;

                material.SetVector("_PointLightPosition0", new Vector4(0, -1000, 0, 1));
                material.SetColor("_PointLightColor0", teacherGazePointLight.transform.GetChild(0).GetComponent<Light>().color);
                material.SetFloat("_PointLightRadius0", teacherGazePointLight.transform.GetChild(0).GetComponent<Light>().range);
            }
        }

        GameObject atomSurfaceRep = GameObject.Find("AtomSurfaceRepresentation");
        if (atomSurfaceRep != null)
        {
            int childCount = atomSurfaceRep.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = atomSurfaceRep.transform.GetChild(i);
                GameObject childGameObject = childTransform.gameObject;

                Renderer renderer = childGameObject.GetComponent<Renderer>();
                Material material = renderer.material;

                material.SetVector("_SpotLight1Pos", new Vector4(0, -1000, 0, 1));
                material.SetColor("_SpotLight1Color", teacherGazeSpotlight.GetComponent<Light>().color);
                material.SetFloat("_SpotLight1Range", teacherGazeSpotlight.GetComponent<Light>().range);
                material.SetFloat("_SpotLight1Intensity", teacherGazeSpotlight.GetComponent<Light>().intensity);
            }
        }
    }

    public string GenerateFileName4Recording(string extension, string expType)
    {
        //return $"{currentPDB}_{currentEyeGaze}_{expType}.{extension}";
        return $"{currentPDB}_{expType}.{extension}";
    }

    public string GenerateFileName4Replaying(string extension, string expType)
    {
        return $"{participantName}_{currentPDB}_{currentEyeGaze}_{expType}.{extension}";
    }

    public void SaveToFile(string content, string extension, string expType)
    {
        if (expType == "Recording")
        {
            fileName = GenerateFileName4Recording(extension, expType);
        }

        if (expType == "Replaying")
        {
            fileName = GenerateFileName4Replaying(extension, expType);
        }

        string filePath = Path.Combine(folderPath, fileName);

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine(content);
        }

        if (extension == "json" || extension == "csv")
        {
            recordedScenePath = filePath;
        }
    }

    private void SaveSceneRecording()
    {
        StopAudioRecording();

        StringBuilder jsonStates = new StringBuilder();
        foreach (UnityMolEyeGazeState state in recordedStates)
        {
            jsonStates.AppendLine(JsonUtility.ToJson(state));
        }

        SaveToFile(jsonStates.ToString(), "json", "Recording");
        recordedStates.Clear();
    }

    void StartAudioRecording()
    {
        if (isRecordingAudio)
            return;

        if (Microphone.devices.Length <= 0)
        {
            Debug.LogWarning("No microphone detected.");
            return;
        }

        string micDevice = Microphone.devices[0];
        recordedAudio = Microphone.Start(micDevice, true, 180, audioSampleRate);
        isRecordingAudio = true;
        audioStartTimestamp = Time.time;

        Debug.Log("recording audio ...");
    }

    private void SaveAudioFile()
    {
        fileName = GenerateFileName4Recording("wav", "Recording");
        string filePath = Path.Combine(folderPath, fileName);

        try
        {
            SavWav.Save(Path.GetDirectoryName(filePath), Path.GetFileName(filePath), recordedAudio);
            audioFilePath = filePath;  // Set the public variable
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save audio: " + ex.Message);
        }
    }

    void StopAudioRecording()
    {
        if (!isRecordingAudio)
            return;

        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
            isRecordingAudio = false;

            if (recordedAudio)
            {
                SaveAudioFile();
            }
        }
        Debug.Log("audio recording stopped");
    }

    public List<UnityMolEyeGazeState> LoadSceneStatesFromFile(string currentRecordedScenePath)
    {
        List<UnityMolEyeGazeState> loadedStates = new List<UnityMolEyeGazeState>();
        foreach (string line in File.ReadLines(currentRecordedScenePath))
        {
            UnityMolEyeGazeState state = JsonUtility.FromJson<UnityMolEyeGazeState>(line);
            loadedStates.Add(state);
        }
        return loadedStates;
    }

    public AudioClip LoadWavAudio(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        AudioClip clip = SavWav.FromWavBytes(fileData);
        return clip;
    }
}