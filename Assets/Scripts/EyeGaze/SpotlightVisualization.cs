using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpotlightVisualization : MonoBehaviour
{
    public GameObject spotLightLocation;

    void Update()
    {
        this.transform.position = spotLightLocation.transform.position - new Vector3(0, 0, 0.45f);
    }
}
