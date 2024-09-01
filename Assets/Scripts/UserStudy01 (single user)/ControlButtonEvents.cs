using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using System.Text;

public class ControlButtonEvents : MonoBehaviour
{
    public RecordAndReplay recordAndReplay;
    private TextMeshProUGUI textMeshProteinViz;
    private string proteinVizName;
    private Vector3 activeChildPosUnderStudentPlayer;
    private bool replayFlag = false;

    private int currentStateIndex = 0;
    private List<RecordAndReplay.UnityMolEyeGazeState> loadedStates;
    private Transform childTransform;

    private AudioSource audioSource;
    private float audioStartTime;

    private Vector3 targetAvatarPosition; // Store the target position for interpolation
    private Quaternion targetAvatarRotation; // Store the target rotation for interpolation
    private float lerpTime = 0.5f; // Adjust the interpolation time as needed
    private float lerpStartTime; // Store the start time of the interpolation

    [HideInInspector]
    public bool deactiveGameObjectFlag = false;

    public SceneStates currentSceneState;
    public enum SceneStates
    {
        Default,
        Record,
        Save
    }

    [System.Serializable]
    private class TeacherStudentGazeInteractionState
    {
        public Vector3 teacherGazePos;
        public Vector3 studentGazePos;
        public float timestamp;
        public float dist;
    }
    private List<TeacherStudentGazeInteractionState> gazeStatesDuringReplay = new List<TeacherStudentGazeInteractionState>();


    void Start()
    {
        currentSceneState = SceneStates.Default;

        GameObject textMeshPro = GameObject.Find("Protein Viz");
        textMeshProteinViz = textMeshPro.GetComponent<TextMeshProUGUI>();

        recordAndReplay.teacherAvatar.SetActive(false);
        recordAndReplay.teacherLeftController.SetActive(false);
        recordAndReplay.teacherRightController.SetActive(false);
    }

    public void OnStartButtonClick()
    {
        if (currentSceneState == SceneStates.Default || currentSceneState == SceneStates.Save)
        {
            currentSceneState = SceneStates.Record;
            Debug.Log("START button pressed");
        }
    }

    public void OnStopButtonClick()
    {
        if (currentSceneState == SceneStates.Record)
        {
            currentSceneState = SceneStates.Save;
            Debug.Log("STOP button pressed");
        }
    }

    private void StartAudioPlayback()
    {
        string audioFileName = recordAndReplay.GenerateFileName4Recording("wav", "Recording");
        string currentAudioFilePath = Path.Combine(recordAndReplay.folderPath, audioFileName);

        if (!string.IsNullOrEmpty(currentAudioFilePath) && File.Exists(currentAudioFilePath))
        {
            AudioClip loadedClip = recordAndReplay.LoadWavAudio(currentAudioFilePath);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = loadedClip;
        }
        else
        {
            Debug.LogWarning("Audio file does not exist or path is empty.");
        }

        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
            audioStartTime = Time.time;
        }
        else
        {
            Debug.LogError("AudioSource or AudioClip is null.");
        }
    }

    public void OnReplayButtonClick()
    {
        Debug.Log("REPLAY button pressed");

        replayFlag = true;
        currentStateIndex = 0;

        StartAudioPlayback();

        gazeStatesDuringReplay = new List<TeacherStudentGazeInteractionState>();

        string recordedSceneFileName = recordAndReplay.GenerateFileName4Recording("json", "Recording");
        string currentRecordedScenePath = Path.Combine(recordAndReplay.folderPath, recordedSceneFileName);

        if (!string.IsNullOrEmpty(currentRecordedScenePath) && File.Exists(currentRecordedScenePath))
        {
            loadedStates = recordAndReplay.LoadSceneStatesFromFile(currentRecordedScenePath);
        }
        else
        {
            Debug.LogWarning("Scene file does not exist or path is empty.");
        }

        if (loadedStates == null || loadedStates.Count == 0)
        {
            Debug.LogError("loadedStates is null or empty.");
        }

        if (recordAndReplay == null || recordAndReplay.loadedMolecule == null)
        {
            Debug.LogError("recordAndReplay or loadedMolecule is null.");
        }

        childTransform = recordAndReplay.loadedMolecule.GetChild(0).transform;
    }

    void Update()
    {
        if (!replayFlag)
        {
            return;
        }

        GameObject textMeshPro = GameObject.Find("Protein Viz");
        textMeshProteinViz = textMeshPro.GetComponent<TextMeshProUGUI>();
        proteinVizName = textMeshProteinViz.text;

        GameObject studentPlayer = GameObject.FindGameObjectWithTag("Students"); // Instantiated Player
        if (studentPlayer != null)
        {
            for (int i = 0; i < studentPlayer.transform.childCount; i++)
            {
                Transform child = studentPlayer.transform.GetChild(i);
                if (child.gameObject.activeInHierarchy)
                {
                    activeChildPosUnderStudentPlayer = child.position;
                }
            }
        }

        if (audioSource != null && loadedStates != null && recordAndReplay != null && recordAndReplay.loadedMolecule != null && childTransform != null)
        {
            if (audioSource.isPlaying && currentStateIndex < loadedStates.Count)
            {
                float elapsedTime = Time.time - audioStartTime;
                while (currentStateIndex < loadedStates.Count)
                {
                    var currentState = loadedStates[currentStateIndex];
                    if (currentState != null && elapsedTime >= currentState.timestamp)
                    {
                        deactiveGameObjectFlag = false;
                        ProcessState(currentState);
                        currentStateIndex++;
                    }
                    else
                    {
                        break; // Exit the loop if the timestamp of the current state is not reached
                    }
                }
            }

            // Update the avatar's position and rotation using lerp
            recordAndReplay.teacherAvatar.transform.position = Vector3.Lerp(recordAndReplay.teacherAvatar.transform.position,
                targetAvatarPosition, (Time.time - lerpStartTime) / lerpTime);

            recordAndReplay.teacherAvatar.transform.rotation = Quaternion.Slerp(recordAndReplay.teacherAvatar.transform.rotation,
                targetAvatarRotation, (Time.time - lerpStartTime) / lerpTime);


            if (currentStateIndex == loadedStates.Count - 1)
            {
                Debug.Log("length of gazeStatesDuringReplay is: " + gazeStatesDuringReplay.Count);

                StringBuilder csvContent = new StringBuilder();
                csvContent.AppendLine("Timestamp,TeacherGazePosX,TeacherGazePosY,TeacherGazePosZ,StudentGazePosX,StudentGazePosY,StudentGazePosZ,Distance");

                foreach (TeacherStudentGazeInteractionState gaze_state in gazeStatesDuringReplay)
                {
                    csvContent.AppendLine($"{gaze_state.timestamp},{gaze_state.teacherGazePos.x},{gaze_state.teacherGazePos.y},{gaze_state.teacherGazePos.z}," +
                                $"{gaze_state.studentGazePos.x},{gaze_state.studentGazePos.y},{gaze_state.studentGazePos.z},{gaze_state.dist}");
                }

                recordAndReplay.SaveToFile(csvContent.ToString(), "csv", "Replaying");
                gazeStatesDuringReplay.Clear();
                replayFlag = false;
                deactiveGameObjectFlag = true;

                recordAndReplay.teacherAvatar.SetActive(false);
                recordAndReplay.teacherLeftController.SetActive(false);
                recordAndReplay.teacherRightController.SetActive(false);

                audioSource.Stop();
                audioSource = null;
            }
        }
    }

    private void ProcessState(RecordAndReplay.UnityMolEyeGazeState state)
    {
        childTransform.position = state.position;
        childTransform.rotation = state.rotation;

        Vector3 parentWorldScale = childTransform.parent.lossyScale;
        Vector3 targetLocalScale = new Vector3(state.scale.x / parentWorldScale.x, state.scale.y / parentWorldScale.y, state.scale.z / parentWorldScale.z);
        childTransform.localScale = targetLocalScale;

        targetAvatarPosition = state.avatarPos;
        targetAvatarRotation = state.avatarRot;

        lerpStartTime = Time.time;

        recordAndReplay.teacherAvatar.SetActive(true);
        recordAndReplay.teacherLeftController.SetActive(true);
        recordAndReplay.teacherRightController.SetActive(true);

        recordAndReplay.teacherAvatar.transform.position = state.avatarPos;
        recordAndReplay.teacherAvatar.transform.rotation = state.avatarRot;
        recordAndReplay.teacherLeftController.transform.position = state.leftControllerPos;
        recordAndReplay.teacherLeftController.transform.rotation = state.leftControllerRot;
        recordAndReplay.teacherRightController.transform.position = state.rightControllerPos;
        recordAndReplay.teacherRightController.transform.rotation = state.rightControllerRot;

        if (recordAndReplay.currentEyeGaze != RecordAndReplay.EyeGazeVizType.None)
        {
            switch (recordAndReplay.currentEyeGaze)
            {
                case RecordAndReplay.EyeGazeVizType.GazePoint:
                    recordAndReplay.teacherGazePointer.SetActive(true);
                    recordAndReplay.teacherGazePointer.transform.position = state.avatarGazePos;
                    SaveInteraction(state.avatarGazePos, Time.time);
                    break;

                case RecordAndReplay.EyeGazeVizType.GazeArrow:
                    recordAndReplay.teacherGazeArrow.SetActive(true);
                    recordAndReplay.teacherGazeArrow.transform.position = state.avatarGazePos + new Vector3(0, 0.055f, 0);
                    recordAndReplay.teacherGazeArrow.transform.rotation = state.avatarGazeRot;
                    SaveInteraction(state.avatarGazePos, Time.time);
                    break;

                case RecordAndReplay.EyeGazeVizType.GazeTrail:
                    recordAndReplay.teacherGazeTrail.SetActive(true);
                    recordAndReplay.teacherGazeTrail.transform.position = state.avatarGazePos;
                    recordAndReplay.teacherParticleTrail.transform.position = recordAndReplay.teacherGazeTrail.transform.position;
                    SaveInteraction(state.avatarGazePos, Time.time);
                    break;

                case RecordAndReplay.EyeGazeVizType.GazeSpotlight:
                    // Set the color, remotePos, and radius based on proteinVizName
                    Color receivedColor = new Color(1.0f, 1.0f, 1.0f, 1.0f); // float r, float g, float b, float a
                    Vector3 remotePos = new Vector3(0, 0, 0);
                    float radius = 0.1f;

                    if (proteinVizName == "Cartoon" || proteinVizName == "HyperBall")
                    {
                        enablePointLightForCartoonAndHyperBall(state.avatarGazePos, remotePos, radius, receivedColor);
                    }
                    else if (proteinVizName == "Surface")
                    {
                        enableSpotLightForSurface(state.avatarGazePos, remotePos, radius, receivedColor);
                    }

                    SaveInteraction(state.avatarGazePos, Time.time);
                    break;
            }
        }
    }

    private void SaveInteraction(Vector3 teacherEyePos, float t)
    {
        TeacherStudentGazeInteractionState state1 = new TeacherStudentGazeInteractionState
        {
            timestamp = t,
            teacherGazePos = teacherEyePos,
            studentGazePos = activeChildPosUnderStudentPlayer,
            dist = Vector3.Distance(teacherEyePos, activeChildPosUnderStudentPlayer)
        };
        gazeStatesDuringReplay.Add(state1);
    }

    private void enableSpotLightForSurface(Vector3 pos, Vector3 remotePos, float radius, Color receivedColor)
    {
        recordAndReplay.teacherGazePointer.SetActive(true);
        recordAndReplay.teacherGazePointer.transform.position = pos;

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

                material.SetVector("_SpotLight1Pos", new Vector4(pos.x, pos.y, pos.z, 1));
                material.SetColor("_SpotLight1Color", recordAndReplay.teacherGazeSpotlight.GetComponent<Light>().color);
                material.SetFloat("_SpotLight1Range", recordAndReplay.teacherGazeSpotlight.GetComponent<Light>().range);
                material.SetFloat("_SpotLight1Intensity", recordAndReplay.teacherGazeSpotlight.GetComponent<Light>().intensity);
            }
        }
    }

    void enablePointLightForCartoonAndHyperBall(Vector3 pos, Vector3 remotePos, float radius, Color receivedColor)
    {
        recordAndReplay.teacherGazePointLight.SetActive(true);
        recordAndReplay.teacherGazePointLight.transform.position = pos;

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

                material.SetVector("_PointLightPosition0", new Vector4(pos.x, pos.y, pos.z, 1));
                material.SetColor("_PointLightColor0", recordAndReplay.teacherGazePointLight.transform.GetChild(0).GetComponent<Light>().color);
                material.SetFloat("_PointLightRadius0", recordAndReplay.teacherGazePointLight.transform.GetChild(0).GetComponent<Light>().range);
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

                material.SetVector("_PointLightPosition0", new Vector4(pos.x, pos.y, pos.z, 1));
                material.SetColor("_PointLightColor0", recordAndReplay.teacherGazePointLight.transform.GetChild(0).GetComponent<Light>().color);
                material.SetFloat("_PointLightRadius0", recordAndReplay.teacherGazePointLight.transform.GetChild(0).GetComponent<Light>().range);
            }
        }
    }
}