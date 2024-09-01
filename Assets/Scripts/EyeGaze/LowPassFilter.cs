using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LowPassFilter : MonoBehaviour
{
    // The smoothing factor(between 0 and 1)
    public float smoothingFactor = 0.9f;

    // The current filtered position
    private Vector3 filteredPosition;

    // Update is called once per frame
    private void Update()
    {
        // Get the current position
        Vector3 currentPosition = transform.position;

        // Apply low-pass filter to the position
        filteredPosition = Vector3.Lerp(filteredPosition, currentPosition, smoothingFactor);

        // Update the object's position with the filtered position
        transform.position = filteredPosition;
    }
}
