using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 单株草的初始数据结构体
/// 必须是 blittable 类型（仅含值类型），字段顺序与VFX端严格对应
/// </summary>
[System.Serializable]
[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
public struct GrassParticleData
{
    /// <summary> 世界空间位置（xyz），w分量预留用于对齐 </summary>
    public Vector4 position;

    /// <summary> 基础颜色（rgba），生物群系差异化用 </summary>
    public Vector4 baseColor;

    /// <summary> 草的大小缩放 x=宽度缩放 y=高度缩放 z=弯曲阻力 w=预留 </summary>
    public Vector4 sizeAndParam;
}