using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class HDRPFrameSettingsHelper
{
    // 缓存反射获取的 Attribute 信息，避免每次调用都反射
    private static Dictionary<FrameSettingsField, string> _tooltipCache;

    /// <summary>
    /// 动态启用或禁用指定相机的特定 Frame Setting。
    /// </summary>
    public static void SetFrameSetting(Camera camera, FrameSettingsField field, bool enabled)
    {
        if (camera == null)
        {
            Debug.LogError("[HDRPFrameSettingsHelper] Camera 不能为空。");
            return;
        }

        if (!camera.TryGetComponent<HDAdditionalCameraData>(out var hdCamData))
        {
            Debug.LogError($"[HDRPFrameSettingsHelper] 相机 {camera.name} 上未找到 HDAdditionalCameraData 组件。");
            return;
        }

        if (!hdCamData.customRenderingSettings)
        {
            hdCamData.customRenderingSettings = true;
        }

        // ✅ 修复：强制转为 uint，因为 mask 索引器需要 uint 类型
        uint fieldIndex = (uint)field;
        if (!hdCamData.renderingPathCustomFrameSettingsOverrideMask.mask[fieldIndex])
        {
            hdCamData.renderingPathCustomFrameSettingsOverrideMask.mask[fieldIndex] = true;
        }

        hdCamData.renderingPathCustomFrameSettings.SetEnabled(field, enabled);

        // ✅ 修复：通过 RenderPipelineManager 获取当前 HDRP 实例
        if (field == FrameSettingsField.ShadowMaps && !enabled)
        {
            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            hdrp?.ReleasePersistentShadowAtlases();
        }
    }

    /// <summary>
    /// 获取所有 FrameSettingsField 枚举及其对应的 Tooltip 描述信息。
    /// 通过反射读取内部的 FrameSettingsFieldAttribute。
    /// 其中，FrameSettingsField.ToString() 可以获取枚举名称
    /// </summary>
    public static Dictionary<FrameSettingsField, string> GetAllFrameSettingsFieldDescriptions()
    {
        if (_tooltipCache != null)
            return _tooltipCache;

        _tooltipCache = new Dictionary<FrameSettingsField, string>();
        var enumType = typeof(FrameSettingsField);
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

        // ✅ 修复：通过反射获取内部 Attribute 类型（跨程序集访问）
        var attrType = typeof(HDAdditionalCameraData).Assembly
            .GetType("UnityEngine.Rendering.HighDefinition.FrameSettingsFieldAttribute");

        if (attrType == null)
        {
            Debug.LogError("[HDRPFrameSettingsHelper] 未找到 FrameSettingsFieldAttribute 类型。");
            return _tooltipCache;
        }

        // ✅ 修复：获取 public readonly 的 tooltip 字段
        var tooltipField = attrType.GetField("tooltip",
            BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.GetCustomAttribute<ObsoleteAttribute>() != null)
                continue;

            var enumValue = (FrameSettingsField)field.GetValue(null);

            if (enumValue == FrameSettingsField.None)
                continue;

            var attr = field.GetCustomAttribute(attrType);
            string description = null;

            if (attr != null && tooltipField != null)
            {
                description = tooltipField.GetValue(attr) as string;
            }

            if (string.IsNullOrEmpty(description))
            {
                description = field.Name;
            }

            _tooltipCache[enumValue] = description;
        }

        return _tooltipCache;
    }
}