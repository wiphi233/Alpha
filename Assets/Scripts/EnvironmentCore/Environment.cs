using NaughtyAttributes;
using UnityEngine;

public class Environment : MonoBehaviour
{
    [Foldout("Day and Night 昼夜交替")]
    public GameObject sunLightObject;
    [Foldout("Day and Night 昼夜交替")]
    [Range(5f, 3600f)] public float cycleDuration = 300f; // 60 to 300
    [Foldout("Day and Night 昼夜交替")]
    [Range(0f, 2f)] public float maxIntensity = 1f;
    [Foldout("Day and Night 昼夜交替")]
    [Range(0f, 90f)] public float goldenAngle = 15f; // 日出/日落金黄过渡范围（度）

    [Foldout("Day and Night 昼夜交替")]
    public Color dayColor = Color.white;
    [Foldout("Day and Night 昼夜交替")]
    public Color dawnDuskColor = new Color(1f, 0.6f, 0.2f);

    // 环境光将被全局 Shader 属性覆盖，这里保留仅为编辑器展示
    [Foldout("Day and Night 昼夜交替")]
    public Color ambientDay = new Color(0.5f, 0.5f, 0.5f);
    [Foldout("Day and Night 昼夜交替")]
    public Color ambientNight = new Color(0.1f, 0.1f, 0.1f);

    private Light sunLight;
    private float startTime = -1f;
    private bool lastDayState = false; // true = 白天，false = 夜晚

    //[HorizontalLine(color: EColor.Green)]
    //[Foldout("Weather 天气")]
    //public GameObject rainObject;
    //[Foldout("Weather 天气")]
    //private bool enableWeatherReplace = true;
    //[Foldout("Weather 天气")]
    //private bool enableRain = true;
    
    //public bool EnableWeatherReplace
    //{
    //    get => enableWeatherReplace;
    //    set
    //    {
    //        enableWeatherReplace = value;
    //        rainObject.SetActive(EnableRain);
    //    }
    //}

    //public bool EnableRain
    //{
    //    get => enableRain;
    //    set
    //    {
    //        enableRain = value;
    //        if (EnableWeatherReplace)
    //        {
    //            rainObject.SetActive(value);
    //        }
    //    }
    //}

    //[Foldout("Weather 天气更替")]
    //public bool enableSnow = true;

    //[HorizontalLine(color: EColor.Green)]
    //[Foldout("Four Seasons 四季交替")]
    //public bool enableSeasonReplace = true;

    void Start()
    {
        sunLight = sunLightObject.GetComponent<Light>();
        //RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        // 彻底禁用内置环境光贡献
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientNight;

        // ⭐ 新增：禁用反射
        //RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        //RenderSettings.reflectionIntensity = 0f;
    }

    void Update()
    {
        if (GameStateManager.Get() != GameState.LoadingCompleted) return;

        if (startTime < 0f) startTime = Time.time;

        float t = ((Time.time - startTime) % cycleDuration) / cycleDuration;
        float elevation = -Mathf.Cos(Mathf.PI * 2f * t) * 90f;
        float azimuth = t * 360f;
        sunLightObject.transform.rotation = Quaternion.Euler(elevation, azimuth, 0f);

        // 1. 方向光强度：仅当高度角 > 0° 时线性增加，否则为 0
        float intensity = (elevation > 0f) ? Mathf.Lerp(0f, maxIntensity, elevation / 90f) : 0f;
        sunLight.intensity = intensity;

        // 2. 方向光颜色：高度角在 [0, goldenAngle] 内金黄 → 白色
        float colorBlend = Mathf.InverseLerp(0f, goldenAngle, Mathf.Max(0f, elevation));
        sunLight.color = Color.Lerp(dawnDuskColor, dayColor, colorBlend);

        Color globalAmbient;
        if (elevation <= 0f)
        {
            globalAmbient = new Color(0.1f, 0.1f, 0.1f);
            if (lastDayState)
            {
                LightingManager.SetSkyboxCubemap(LightingManager.Instance.belfast_sunset_puresky_4k, 1f, azimuth / 2);
                lastDayState = false;
            }
        }
        else if (elevation >= goldenAngle)
        {
            globalAmbient = ambientDay;
            if (lastDayState == false)
            {
                LightingManager.SetSkyboxCubemap(LightingManager.Instance.citrus_orchard_puresky_4k, 1f, azimuth / 2);
                lastDayState = true;
            }
        }
        else
        {
            float blend = elevation / goldenAngle; // 0→1
            globalAmbient = Color.Lerp(dawnDuskColor * 0.3f, ambientDay, blend);
            if (lastDayState == false)
            {
                LightingManager.SetSkyboxCubemap(LightingManager.Instance.citrus_orchard_puresky_4k, 1f, azimuth / 2);
                lastDayState = true;
            }
        }
        LightingManager.hdriSky.rotation.value = azimuth / 2;
        Shader.SetGlobalColor("_GlobalAmbientColor", globalAmbient);
        RenderSettings.ambientLight = globalAmbient;
    }
}