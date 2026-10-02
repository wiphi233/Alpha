using UnityEngine;
using UnityEngine.Rendering;


public class CustomDynamicResolution : MonoBehaviour
{
    [Range(0.5f, 1f)]
    public float minScale = 0.6f; // 60% 最低分辨率
    [Range(0.5f, 1f)]
    public float maxScale = 1.0f; // 100% 最高分辨率
    public float targetFrameRate = 60f;
    public float evaluationInterval = 0.5f; // 每 0.5 秒评估一次

    private float _currentScale = 1.0f;
    private float _evaluationTimer = 0f;

    void OnEnable()
    {
        // 使用 ReturnsMinMaxLerpFactor 策略，我们返回一个 0-1 的插值因子
        DynamicResolutionHandler.SetDynamicResScaler(
            SetDynamicResolutionScale,
            DynamicResScalePolicyType.ReturnsMinMaxLerpFactor
        );
    }

    void OnDisable()
    {
        // 禁用时移除缩放器，恢复默认
        DynamicResolutionHandler.SetDynamicResScaler(null, DynamicResScalePolicyType.ReturnsMinMaxLerpFactor);
    }

    // 这个函数返回一个 0-1 的 lerp 因子，0 代表最小分辨率，1 代表最大分辨率
    private float SetDynamicResolutionScale()
    {
        return _currentScale; // 返回我们计算好的插值因子
    }

    void Update()
    {
        _evaluationTimer += Time.unscaledDeltaTime;
        if (_evaluationTimer < evaluationInterval) return;
        _evaluationTimer = 0f;

        // 获取当前帧率
        float currentFPS = 1.0f / Time.unscaledDeltaTime;
        float targetFrameTime = 1.0f / targetFrameRate;

        // 简单的比例控制器：计算当前帧率与目标帧率的偏差
        float frameTimeRatio = targetFrameTime / Time.unscaledDeltaTime;

        // 根据偏差调整插值因子 _currentScale
        // 如果帧时间超过目标，frameTimeRatio < 1，_currentScale 减小
        // 如果帧时间短于目标，frameTimeRatio > 1，_currentScale 增大
        _currentScale *= Mathf.Lerp(0.9f, 1.1f, Mathf.InverseLerp(0.5f, 2.0f, frameTimeRatio));

        // 将插值因子限制在 0-1 范围内
        _currentScale = Mathf.Clamp01(_currentScale);

        // 注意：实际分辨率百分比 = Mathf.Lerp(minScale * 100, maxScale * 100, _currentScale)
        // 这个 minScale 和 maxScale 需要在 HDRP Asset 中设置好
    }
}