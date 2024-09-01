#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Photon.Pun;

public class PhotonViewFinder : EditorWindow
{
    private int searchID;

    [MenuItem("Tools/Find PhotonView by ID")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<PhotonViewFinder>("PhotonView Finder");
    }

    void OnGUI()
    {
        GUILayout.Label("Find GameObject by PhotonView ID", EditorStyles.boldLabel);
        searchID = EditorGUILayout.IntField("PhotonView ID:", searchID);

        if (GUILayout.Button("Find"))
        {
            FindGameObjectByPhotonViewID();
        }
    }

    void FindGameObjectByPhotonViewID()
    {
        PhotonView[] allPhotonViews = GameObject.FindObjectsOfType<PhotonView>();

        foreach (PhotonView pv in allPhotonViews)
        {
            if (pv.ViewID == searchID)
            {
                EditorGUIUtility.PingObject(pv.gameObject);
                Selection.activeGameObject = pv.gameObject;
                return;
            }
        }

        Debug.LogWarning("No GameObject found with PhotonView ID: " + searchID);
    }
}
#endif
