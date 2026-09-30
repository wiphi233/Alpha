using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SignalCenter : MonoBehaviour, ISignalCenter
{
    private static SignalCenter Instance;

    // 信号字典：信号名称 -> 订阅者列表
    private Dictionary<SignalType, List<System.Action<GameObject, object>>> signalListeners;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            signalListeners = new Dictionary<SignalType, List<System.Action<GameObject, object>>>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 订阅信号
    public void Subscribe(SignalType signalName, System.Action<GameObject, object> callback)
    {
        if (!signalListeners.ContainsKey(signalName))
            signalListeners[signalName] = new List<System.Action<GameObject, object>>();

        signalListeners[signalName].Add(callback);
    }

    // 取消订阅
    public void Unsubscribe(SignalType signalName, System.Action<GameObject, object> callback)
    {
        if (signalListeners.ContainsKey(signalName))
            signalListeners[signalName].Remove(callback);
    }

    // 发送信号
    public void Emit(SignalType signalType, GameObject sender, object data = null)
    {
        if (signalListeners.ContainsKey(signalType))
        {
            foreach (var callback in signalListeners[signalType])
            {
                callback?.Invoke(sender, data);
            }
        }
    }
}