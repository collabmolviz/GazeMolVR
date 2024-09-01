using UnityEngine;

public class ScriptAttacher : MonoBehaviour
{
    private bool hasAttachedScript = false; // To make sure you only attach the script once

    void Update()
    {
        if (!hasAttachedScript)
        {
            GameObject go = GameObject.Find("all_8gz3_B_CartoonMesh");

            if (go != null)
            {
                go.AddComponent<VertexMarker>().vertexIndex = 5456; // Adding VertexMarker script and setting vertexIndex to 0
                hasAttachedScript = true; // Set this to true so you don't keep attaching the script
            }
        }
    }
}
