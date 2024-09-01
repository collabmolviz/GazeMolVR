using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;

namespace UMol {
public class ClickController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{

    [SerializeField]
    [Tooltip("How long to trigger a long press")]
    private float holdTime = 0.5f;

    public float timeDoubleLimit = 0.25f;
    public PointerEventData.InputButton button;

    [System.Serializable]
    public class OnSingleClick : UnityEvent {};
    public OnSingleClick onSingleClick;

    [System.Serializable]
    public class OnDoubleClick : UnityEvent {};
    public OnDoubleClick onDoubleClick;

    [System.Serializable]
    public class OnLongClick : UnityEvent {};
    public OnLongClick onLongClick;

    private int clickCount;
    private float firstClickTime;
    private float currentTime;

    bool longPressDelayInvoked = false;
    bool ignoreClickWhenLong = false;
    private Coroutine clickCo;

    private ClickController () {
        clickCount = 0;
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (!longPressDelayInvoked) {
            Invoke("OnLongPress", holdTime);
            longPressDelayInvoked = true;
        }
    }
    public void OnPointerUp(PointerEventData eventData) {
        CancelInvoke("OnLongPress");
        longPressDelayInvoked = false;
    }

    public void OnPointerExit(PointerEventData eventData) {
        CancelInvoke("OnLongPress");
        longPressDelayInvoked = false;
    }
    private void OnLongPress() {
        if (clickCo != null) {
            StopCoroutine(clickCo);
            clickCo = null;
        }
        ignoreClickWhenLong = true;
        onLongClick.Invoke();
        longPressDelayInvoked = false;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (ignoreClickWhenLong) {
            ignoreClickWhenLong = false;
            return;
        }

        if (this.button != eventData.button)
            return;

        this.clickCount++;

        if (this.clickCount == 1) {
            firstClickTime = eventData.clickTime;
            currentTime = firstClickTime;
            clickCo = StartCoroutine(ClickRoutine());
        }
    }

    private IEnumerator ClickRoutine () {

        while (clickCount != 0)
        {
            yield return new WaitForEndOfFrame();

            currentTime += Time.deltaTime;

            if (currentTime >= firstClickTime + timeDoubleLimit) {
                if (clickCount == 1) {
                    onSingleClick.Invoke();
                } else {
                    onDoubleClick.Invoke();
                }
                clickCount = 0;
            }
        }
    }
}
}