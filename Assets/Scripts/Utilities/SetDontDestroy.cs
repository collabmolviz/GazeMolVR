using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UMol {
public class SetDontDestroy : MonoBehaviour
{
    public List<GameObject> toSave = new List<GameObject>();

    public bool switchScene = false;


    void Start()
    {
        foreach (GameObject go in toSave) {
            if (go != null)
                DontDestroyOnLoad(go);
        }
    }
}
}
