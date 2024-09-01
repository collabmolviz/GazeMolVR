using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;


namespace UMolPlayTests
{

/// <summary>
/// Ensure the scene MainUIScene is correctly loaded.
/// </summary>
public class LoadMainUISceneTest
{

    /// Path to the tested scene
    public string ScenePath = "Assets/Scenes/MainUIScene.unity";

    /// <summary>
    /// Array of Game Objects loaded in the tested scene.
    /// </summary>
    public string[] GOInstances = { "[DesactivateVR]", "CameraUMolX", "LoadedMolecules", "Directional Light",
                                    "EventSystem", "CanvasMainUI", "ConsolePython_Autocomplete",
                                    "ArgumentReader", "FileDrop", "UMolQuit", "PowerSaving2"};

    [UnitySetUp]
    public IEnumerator Setup() {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
    }


    /// <summary>
    /// Check if all Game Objects are loaded when the scene is loaded.
    /// </summary>
    [Test]
    public void GameObjectsTest()
    {
        TestUtils.AssertGO(GOInstances);
    }

    /// <summary>
    /// Test if some components of the camera are activated.
    /// </summary>
    [Test]
    public void CameraComponentsTest()
    {
        var camera = GameObject.Find("CameraUMolX");
        var components = new string[] { "Camera", "CameraManager", "ManipulationManager", "PostProcessVolume" };
        TestUtils.AssertComponents(camera, components);
    }

}
}
