using System;
using TMPro;
using UnityEngine;

public class GameDebug : MonoBehaviour
{
    public GameObject DebugInfoObject;
    private TextMeshProUGUI DebugInfo;
    private const float countTime = 0.5f;
    private float startTime;
    private float frameSum = 0f;
    private float fps = -1f;

    void Start()
    {
        DebugInfo = DebugInfoObject.GetComponent<TextMeshProUGUI>();
        startTime = Time.time;
    }

    void Update()
    {
        if (GameStateManager.Get() == GameState.LoadingCompleted)
        {
            frameSum++;
            if (Time.time - startTime >= countTime)
            {
                fps = frameSum / (Time.time - startTime);
                startTime = Time.time;
                frameSum = 0f;
            }
            DebugInfo.SetText($@"[Alpha OS]
FPS: {fps:F1}
CPU: {SystemInfo.processorType}
GPU: {SystemInfo.graphicsDeviceName}
Time: {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}
尽情探索这拥有无限可能的世界吧！");
        }

    }
}
