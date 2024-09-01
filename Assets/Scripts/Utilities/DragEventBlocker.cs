using UnityEngine;
using UnityEngine.EventSystems;

public class DragEventBlocker : MonoBehaviour
    , IDragHandler
{
    public void OnDrag(PointerEventData eventData)
    {
        // Debug.Log("Drag event blocked");
    }
}