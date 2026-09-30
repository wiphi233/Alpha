using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
//using static UnityEditor.FilePathAttribute;


public class LightingManager : MonoBehaviour
{
    public static LightingManager Instance;
    private static Volume volume;
    public Cubemap citrus_orchard_puresky_4k; // 默认天空
    public Cubemap belfast_sunset_puresky_4k;

    public static HDRISky hdriSky;
    public static GradientSky gradientSky;
    public static VisualEnvironment visualEnvironment;

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("LightingManager: 已存在实例，无法创建新的实例！");
            Destroy(this);
            return;
        }
        volume = gameObject.GetComponent<Volume>();
        if (volume.profile.TryGet<HDRISky>(out var _hdriSky))
        {
            hdriSky = _hdriSky;
        }
        if (volume.profile.TryGet<GradientSky>(out var _gradientSky))
        {
            gradientSky = _gradientSky;
        }
        if (volume.profile.TryGet<VisualEnvironment>(out var _visualEnvironment))
        {
            visualEnvironment = _visualEnvironment;
        }
    }

    public static void SetSkyboxCubemap(Cubemap cubemap, float exposure = 1.0f , float rotation = 0f)
    {
        //RenderSettings.skybox = material;
        //DynamicGI.UpdateEnvironment();
        visualEnvironment.skyType.value = (int)SkyType.HDRI;
        hdriSky.hdriSky.value = cubemap;
        hdriSky.exposure.value = exposure; // 曝光值
        hdriSky.rotation.value = rotation;
    }

    public static void SetSkyboxGradient(Color topColor, Color ?middleColor = null, Color ?bottomColor = null, float exposure = 1.0f)
    {
        if (middleColor == null)
        {
            middleColor = topColor;
        }
        if (bottomColor == null)
        {
            bottomColor = middleColor;
        }
        visualEnvironment.skyType.value = (int)SkyType.Gradient;
        gradientSky.top.value = topColor;
        gradientSky.middle.value = (Color)middleColor;
        gradientSky.bottom.value = (Color)bottomColor;
        gradientSky.exposure.value = exposure;
    }
}