using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputFocusManager : MonoBehaviour
{
    private static bool _isInputFocused;
    private static GameObject _lastSelected;
    private static bool _hasCached;

    void Update()
    {
        var current = EventSystem.current?.currentSelectedGameObject;

        // 只在变化时更新
        if (current != _lastSelected || !_hasCached)
        {
            _lastSelected = current;
            _isInputFocused = current != null && IsInputField(current);
            _hasCached = true;
        }
    }

    private static bool IsInputField(GameObject obj)
    {
        // 缓存组件检查结果
        return obj.GetComponent<InputField>() != null ||
               obj.GetComponent<TMPro.TMP_InputField>() != null;
    }

    public static bool IsFocused => _isInputFocused;
}
