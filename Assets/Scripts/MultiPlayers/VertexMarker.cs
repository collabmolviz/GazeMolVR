using UnityEngine;

public class VertexMarker : MonoBehaviour
{
    public int vertexIndex = 0; // The index of the vertex you want to mark
    private GameObject markerInstance; // The GameObject that will serve as a marker
    private int lastVertexIndex = -1; // Stores the last vertex index, for comparison

    void Start()
    {
        // Initialize the sphere
        markerInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        markerInstance.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);

        // Set the sphere color to red
        Renderer rend = markerInstance.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.red;
        }

        // Make the marker a child of the GameObject to keep the scene tidy
        markerInstance.transform.SetParent(this.transform);
    }

    void Update()
    {
        // Only update if the vertex index has changed
        if (vertexIndex != lastVertexIndex)
        {
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;

            if (vertexIndex >= 0 && vertexIndex < vertices.Length)
            {
                Vector3 vertexPosition = transform.TransformPoint(vertices[vertexIndex]); // Convert to world coordinates

                // Update the sphere position
                markerInstance.transform.position = vertexPosition;

                // Record the current vertex index
                lastVertexIndex = vertexIndex;
            }
        }
    }
}
