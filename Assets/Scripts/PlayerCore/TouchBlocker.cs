using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TouchBlocker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.isTouchingUI = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.isTouchingUI = false;
        }
    }
}