using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.InputSystem.LowLevel; // 用于鼠标点击检测

public class TouchManager : MonoBehaviour
{
    private InputAction touchPressAction;
    private InputAction mousePressAction; // 添加鼠标输入

    void Start()
    {
        Debug.Log("TouchManager Started.");

        // 启用增强触摸功能
        EnhancedTouchSupport.Enable();

        // 订阅触摸事件
        Touch.onFingerDown += OnTouchBegan;

        // 订阅鼠标点击事件
        SetupMouseInput();
    }

    void SetupMouseInput()
    {
        // 创建鼠标左键点击的 InputAction
        mousePressAction = new InputAction("MousePress", InputActionType.Button, "<Mouse>/leftButton");

        // 订阅鼠标点击事件
        mousePressAction.performed += OnMouseClicked;
        mousePressAction.Enable();

        Debug.Log("Mouse input enabled.");
    }

    // 鼠标点击回调
    private void OnMouseClicked(InputAction.CallbackContext context)
    {
        // 获取鼠标位置
        Vector2 screenPosition = Mouse.current.position.ReadValue();

        // 执行射线检测，获取点击到的所有物体
        GameObject[] clickedObjects = GetObjectsAtScreenPosition(screenPosition);

        // 调用判定逻辑（使用 -1 作为鼠标的 fingerId）
        OnTouchClicked(clickedObjects, screenPosition, -1);
    }

    private void OnTouchBegan(Finger finger)
    {
        int fingerId = finger.index;
        Vector2 screenPosition = finger.screenPosition;

        // 执行射线检测，获取点击到的所有物体
        GameObject[] clickedObjects = GetObjectsAtScreenPosition(screenPosition);

        // 调用判定逻辑
        OnTouchClicked(clickedObjects, screenPosition, fingerId);
    }

    void Update()
    {
        int cnt = 0;
        if (Keyboard.current.aKey.wasPressedThisFrame) cnt++; 
        if (Keyboard.current.sKey.wasPressedThisFrame) cnt++; 
        if (Keyboard.current.dKey.wasPressedThisFrame) cnt++;
        if (Keyboard.current.fKey.wasPressedThisFrame ) cnt++;
        if (Keyboard.current.gKey.wasPressedThisFrame ) cnt++; 
        if (Keyboard.current.hKey.wasPressedThisFrame ) cnt++;
        if (Keyboard.current.jKey.wasPressedThisFrame ) cnt++; 
        if (Keyboard.current.kKey.wasPressedThisFrame ) cnt++; 
        if (Keyboard.current.lKey.wasPressedThisFrame ) cnt++;
        if (Keyboard.current.semicolonKey.wasPressedThisFrame) cnt++;
        if (Keyboard.current.spaceKey.wasPressedThisFrame) cnt++;
        for (int i = 0; i < cnt; i++)
        {
            if (Music.Instance.notesQueue.Count != 0)
            {
                Judge(Music.Instance.notesQueue.Peek());
            }
        }
    }

    private void OnTouchClicked(GameObject[] clickedObjects, Vector2 screenPosition, int fingerId)
    {
        //Debug.Log($"点击 {(fingerId >= 0 ? $"手指 {fingerId}" : "鼠标")}，位置 {screenPosition}，找到 {clickedObjects.Length} 个物体");

        foreach (var obj in clickedObjects)
        {
            if (obj.tag == Tags.Note)
            {
                if (Judge(obj))
                {
                    break;
                    // 成功判定后退出循环，避免一个点击判定多个物体
                }
            }
        }
    }

    private bool Judge(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }
        NoteController noteController = obj.GetComponent<NoteController>();
        int elapsedMs = (int)((Time.time - Music.Instance.startTime) * 1000);
        int delay = noteController.time - elapsedMs;

        if (delay <= NoteConstant.badRange && delay >= -NoteConstant.goodRange)
        {
            if (Math.Abs(delay) <= NoteConstant.perfectRange)
            {
                Music.Instance.perfectNumber++;
                Music.Instance.nowComboNumber++;
            }
            else if (Math.Abs(delay) <= NoteConstant.goodRange)
            {
                Music.Instance.goodNumber++;
                Music.Instance.nowComboNumber++;
                if (delay > 0)
                {
                    Music.Instance.earlyNumber++;
                }
                else
                {
                    Music.Instance.lateNumber++;
                }
            }
            else
            {
                Music.Instance.badNumber++;
                Music.Instance.nowComboNumber = 0;
            }
            Music.Instance.notesQueue.Dequeue();
            Music.Instance.notesPool.Release(obj);
            return true;
        }
        return false;
        // 否则不产生任何判定，Miss 判定由 Music 类处理
    }

    private GameObject[] GetObjectsAtScreenPosition(Vector2 screenPosition)
    {
        // 创建射线
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);

        // 获取所有点击到的物体
        RaycastHit[] hits = Physics.RaycastAll(ray);
        GameObject[] clickedObjects = new GameObject[hits.Length];

        for (int i = 0; i < hits.Length; i++)
        {
            clickedObjects[i] = hits[i].collider.gameObject;
        }

        return clickedObjects;
    }

    void OnDestroy()
    {
        if (EnhancedTouchSupport.enabled)
        {
            Touch.onFingerDown -= OnTouchBegan;
            EnhancedTouchSupport.Disable();
        }
        if (mousePressAction != null)
        {
            mousePressAction.performed -= OnMouseClicked;
            mousePressAction.Disable();
            mousePressAction.Dispose();
        }
        EnhancedTouchSupport.Disable();
    }
}