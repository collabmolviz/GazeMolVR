using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RightClickOption : MonoBehaviour, IPointerClickHandler {

    // public GameObject Options;
    public UnityEvent onRightClick;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // if(Options != null){
            //     Options.SetActive(!Options.activeInHierarchy);
            // }
            onRightClick.Invoke();
        }
    }
}
