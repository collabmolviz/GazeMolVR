using UnityEngine;
using UnityEngine.EventSystems;


public class ExpandMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler{

    public float reducedWidth = 15.0f;

    float initWidth = 100.0f;

    RectTransform rt;
    Vector2 rsize = Vector2.zero;

    void Start(){
        rt = GetComponent<RectTransform>();
        initWidth = rt.rect.width;
        rsize = rt.sizeDelta;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        rsize.x = initWidth;
        rt.sizeDelta = rsize;
        foreach(Transform t in transform){
            t.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rsize.x = reducedWidth;
        rt.sizeDelta = rsize;
        foreach(Transform t in transform){
            t.gameObject.SetActive(false);
        }
    }
}