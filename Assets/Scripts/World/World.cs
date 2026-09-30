using DG.Tweening;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

//public class WaveConfig
//{
//    public float zOffset;      // Z轴偏移量
//    public float heightOffset; // 高度基线偏移
//    public float noiseOffsetX; // 噪声X偏移
//    public float noiseOffsetY; // 噪声Y偏移
//}

//public class ChunkData
//{
//    public GameObject terrainObject;
//    public List<GameObject> items;
//    public ChunkData()
//    {
//        items = new List<GameObject>();
//        terrainObject = null;
//    }
//}

//public struct DeterministicRandom
//{
//    private uint state;

//    public DeterministicRandom(int seed, int x, int z, int y)
//    {
//        state = (uint)HashCode.Combine(seed, x, z, y);
//        state = (state * 0x9e3779b9) ^ (state >> 17);
//    }

//    public double NextDouble()
//    {
//        state = state * 0x9e3779b9 + 1;
//        return (double)state / uint.MaxValue;
//    }
//}

public class World : MonoBehaviour
{
    public static World Instance;

    [Header("Loading Canvas 加载界面")]
    public GameObject WorldLoadingCanvasObject;
    private Canvas WorldLoadingCanvas;
    public GameObject ProcessTMPObject;
    public static TextMeshProUGUI ProcessTMP;
    public GameObject UsedObject;
    private TextMeshProUGUI UsedTMP;
    public GameObject SliderObject;
    public static Slider Slider;
    public GameObject MessageObject;
    private TextMeshProUGUI MessageTMP;
    public GameObject ShowImageObject;
    private Image ShowImageImage;
    public GameObject GlassPlaneObject;

    public List<Sprite> ShowImageList;
    private List<string> MessageList = new List<string>
    {
        "Accepted? 诶，地形又出bug了，还挺好看的！",
        "Wrong Answer: 你看见了什么？——地形加载失败.png",
        "Run Time Error: 那个深深刻进 Aerissa 的名字，你无从知晓。",
        "Memory Limit Exceeded：凡人之躯，无法承载神的记忆。",
        "Time Limit Exceeded: 第一缕声音，劈开了永恒的寂静。\n你睁开眼时，看见的天空正在碎裂。"
    };
    // Message 用 中二文案+剧情相关（提前透露

    [Header("GameObject 游戏对象")]
    public GameObject TutorialObject;

    public GameObject player;
    //public GameObject crystal;
    //public GameObject loadingTextObject;
    //public GameObject SpherePerfab;
    public GameObject WelcomeWorldObject;

    public GameObject SettingsButtonObject;
    public GameObject MinimapObject;
    public GameObject DebugInfoObject;
    public GameObject ButtonPromptsObject; // 受其他脚本控制
    public GameObject PositionObject;

    private bool SettingsButtonLastState = true;
    private bool MinimapLastState = true;
    private bool DebugInfoLastState = true;
    private bool PositionLastState = true;

    //[Header("Terrain Settings 地形设置")]
    //public Material TerrainMaterial;

    //[Header("Item Settings 物件设置")]
    //[ShowAssetPreview] public GameObject Birch1Perfab;
    //[ShowAssetPreview] public GameObject Birch2Perfab;
    //[ShowAssetPreview] public GameObject Tree1Perfab;
    //[ShowAssetPreview] public GameObject Tree2Perfab;
    //[ShowAssetPreview] public GameObject CemeteryPebbles1Perfab;
    //[ShowAssetPreview] public GameObject CemeteryPebbles2Perfab;
    //[ShowAssetPreview] public GameObject Daffodil;
    //[ShowAssetPreview] public GameObject Grass1;
    //[ShowAssetPreview] public GameObject Grass2;
    //[ShowAssetPreview] public GameObject Hyacinth;
    //[ShowAssetPreview] public GameObject MushroomFantasyOrange1;
    //[ShowAssetPreview] public GameObject MushroomFantasyOrange2;
    //[ShowAssetPreview] public GameObject MushroomFantasyPurple1;
    //[ShowAssetPreview] public GameObject MushroomFantasyPurple2;
    //[ShowAssetPreview] public GameObject RockMossGrown1;
    //[ShowAssetPreview] public GameObject RockMossGrown2;
    //[ShowAssetPreview] public GameObject RockPileForestMoss1;
    //[ShowAssetPreview] public GameObject RockPileForestMoss2;
    //[ShowAssetPreview] public GameObject Sunflower;
    //[ShowAssetPreview] public GameObject Stele; // 石碑
    //public GameObject waterPrefab;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    private ISignalCenter signalCenter;

    private Camera mainCamera;

    private Vector3 cameraPosition = new Vector3(0f, 220f, -10f);

    private Coroutine updateUsedCoroutine;

    public void ClearUI()
    {
        States.IsLockUI = true;
        SettingsButtonLastState = SettingsButtonObject.activeSelf;
        MinimapLastState = MinimapObject.activeSelf;
        DebugInfoLastState = DebugInfoObject.activeSelf;
        PositionLastState = PositionObject.activeSelf;

        SettingsButtonObject.SetActive(false);
        MinimapObject.SetActive(false);
        DebugInfoObject.SetActive(false);
        PositionObject.SetActive(false);

        ButtonPromptsObject.SetActive(false);
    }

    public void RecoverUI()
    {
        States.IsLockUI = false;
        SettingsButtonObject.SetActive(SettingsButtonLastState);
        MinimapObject.SetActive(MinimapLastState);
        DebugInfoObject.SetActive(DebugInfoLastState);
        PositionObject.SetActive(PositionLastState);
    }

    public void OnChangeUIState(GameObject sender, object data)
    {
        if ((int)data == 0)
        {
            ClearUI();
        }
        else
        {
            RecoverUI();
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("警告：重复的 World 实例！");
            return;
        }
        GlassPlaneObject.SetActive(false);
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        WorldLoadingCanvasObject.SetActive(false);
        WorldLoadingCanvas = WorldLoadingCanvasObject.GetComponent<Canvas>();
        ProcessTMP = ProcessTMPObject.GetComponent<TextMeshProUGUI>();
        if (ProcessTMP == null)
        {
            Debug.LogError("ProcessTMP is null.");
        }
        ProcessTMP = ProcessTMPObject.GetComponent<TextMeshProUGUI>();
        UsedTMP = UsedObject.GetComponent<TextMeshProUGUI>();
        Slider = SliderObject.GetComponent<Slider>();
        MessageTMP = MessageObject.GetComponent<TextMeshProUGUI>();
        ShowImageImage = ShowImageObject.GetComponent<Image>();
        if (ShowImageList.Count == 0 || MessageList.Count == 0)
        {
            Debug.LogError("ShowImageList or MessageList is empty. Please assign them in the inspector.");
        }
    }

    void Start()
    {
        signalCenter.Subscribe(SignalType.ChangeUIState, OnChangeUIState);
        signalCenter.Subscribe(SignalType.WorldLoaded, OnWorldLoaded);
        signalCenter.Subscribe(SignalType.EnterWorld, OnEnterWorld);
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    public void OnWorldLoaded(GameObject sender, object data)
    {
        StopCoroutine(updateUsedCoroutine);
        GlassPlaneObject.SetActive(false);
        WorldLoadingCanvasObject.SetActive(false);

        var WelcomeWorldRectTransform = WelcomeWorldObject.GetComponent<RectTransform>();
        Vector2 anchoredPos = WelcomeWorldRectTransform.anchoredPosition;
        WelcomeWorldObject.SetActive(true);
        const float AnimationSpeed = 1f, WorldDisplayTime = 3f;
        DOVirtual.Float(-1024f, 0f, AnimationSpeed, (float x) =>
        {
            anchoredPos.x = x;
            WelcomeWorldRectTransform.anchoredPosition = anchoredPos;
        }).SetEase(Ease.OutBack);
        DOVirtual.DelayedCall(AnimationSpeed + WorldDisplayTime, () =>
        {
            DOVirtual.Float(0f, -1024f, AnimationSpeed, (float x) =>
            {
                anchoredPos.x = x;
                WelcomeWorldRectTransform.anchoredPosition = anchoredPos;
            }).SetEase(Ease.OutBack);
            DOVirtual.DelayedCall(AnimationSpeed, () =>
            {
                WelcomeWorldObject.SetActive(false);
                ITutorial interTutorial = TutorialObject.GetComponent<ITutorial>();
                interTutorial?.EnterTutorial(TutorialSet.BeginTutorial);
            });
        });
    }

    public void OnEnterWorld(GameObject sender, object data)
    {
        GameStateManager.Set(GameState.LoadBeginning);
        PlayerMovement.Instance.gameObject.transform.SetPositionAndRotation(
            cameraPosition, Quaternion.Euler(30, 0, 0));
        System.Random rng = new System.Random();
        int idx = rng.Next(0, ShowImageList.Count - 1);
        ShowImageImage.sprite = ShowImageList[idx];
        if (ShowImageList.Count == MessageList.Count)
        {
            MessageTMP.SetText(MessageList[idx]);
        }
        else
        {
            MessageTMP.SetText(MessageList[rng.Next(0, MessageList.Count - 1)]);
        }
        Process("初始化世界 Instantiation World...");
        player.transform.position = new Vector3(0, 300, 0);
        Slider.value = 0;
        GlassPlaneObject.SetActive(true);
        updateUsedCoroutine = StartCoroutine(UpdateUsedTime());
        WorldLoadingCanvasObject.SetActive(true);
    }

    private IEnumerator UpdateUsedTime()
    {
        float elapsedTime = 0f;
        while (true) // 后续会 StopCoroutine()
        {
            elapsedTime += Time.deltaTime;
            TimeSpan timeSpan = TimeSpan.FromSeconds(elapsedTime);
            UsedTMP.SetText($"Used: {timeSpan:hh\\:mm\\:ss\\.ff}");
            yield return null;
        }
    }

    public static void Process(string state, int instantiatedChunkCount = -1)
    {
        Debug.Log($"[TerrainLoader] {state}");
        ProcessTMP.SetText(state);
        if (instantiatedChunkCount > 0)
        {
            Slider.value += 1f / instantiatedChunkCount;
        }
        else
        {
            //Slider.value += 0.1f / 7f;
        }
    }
}

//    [Header("GameObject 游戏对象")]
//    public GameObject TutorialObject;

//    public GameObject player;
//    public GameObject crystal;
//    public GameObject loadingTextObject;
//    public GameObject SpherePerfab;
//    public GameObject WelcomeWorldObject;

//    public GameObject SettingsButtonObject;
//    public GameObject MinimapObject;
//    public GameObject DebugInfoObject;
//    public GameObject ButtonPromptsObject; // 受其他脚本控制
//    public GameObject PositionObject;

//    private bool SettingsButtonLastState = true;
//    private bool MinimapLastState = true;
//    private bool DebugInfoLastState = true;
//    private bool PositionLastState = true;

//    public void ClearUI()
//    {
//        States.IsLockUI = true;
//        SettingsButtonLastState = SettingsButtonObject.activeSelf;
//        MinimapLastState = MinimapObject.activeSelf;
//        DebugInfoLastState = DebugInfoObject.activeSelf;
//        PositionLastState = PositionObject.activeSelf;

//        SettingsButtonObject.SetActive(false);
//        MinimapObject.SetActive(false);
//        DebugInfoObject.SetActive(false);
//        PositionObject.SetActive(false);

//        ButtonPromptsObject.SetActive(false);
//    }

//    public void RecoverUI()
//    {
//        States.IsLockUI = false;
//        SettingsButtonObject.SetActive(SettingsButtonLastState);
//        MinimapObject.SetActive(MinimapLastState);
//        DebugInfoObject.SetActive(DebugInfoLastState);
//        PositionObject.SetActive(PositionLastState);
//    }

//    public void OnChangeUIState(GameObject sender, object data)
//    {
//        if ((int)data == 0)
//        {
//            ClearUI();
//        }
//        else
//        {
//            RecoverUI();
//        }
//    }

//    [Header("Terrain Settings 地形设置")]
//    public Material TerrainMaterial;

//    [Header("Item Settings 物件设置")]
//    [ShowAssetPreview] public GameObject Birch1Perfab;
//    [ShowAssetPreview] public GameObject Birch2Perfab;
//    [ShowAssetPreview] public GameObject Tree1Perfab;
//    [ShowAssetPreview] public GameObject Tree2Perfab;
//    [ShowAssetPreview] public GameObject CemeteryPebbles1Perfab;
//    [ShowAssetPreview] public GameObject CemeteryPebbles2Perfab;
//    [ShowAssetPreview] public GameObject Daffodil;
//    [ShowAssetPreview] public GameObject Grass1;
//    [ShowAssetPreview] public GameObject Grass2;
//    [ShowAssetPreview] public GameObject Hyacinth;
//    [ShowAssetPreview] public GameObject MushroomFantasyOrange1;
//    [ShowAssetPreview] public GameObject MushroomFantasyOrange2;
//    [ShowAssetPreview] public GameObject MushroomFantasyPurple1;
//    [ShowAssetPreview] public GameObject MushroomFantasyPurple2;
//    [ShowAssetPreview] public GameObject RockMossGrown1;
//    [ShowAssetPreview] public GameObject RockMossGrown2;
//    [ShowAssetPreview] public GameObject RockPileForestMoss1;
//    [ShowAssetPreview] public GameObject RockPileForestMoss2;
//    [ShowAssetPreview] public GameObject Sunflower;
//    [ShowAssetPreview] public GameObject Stele; // 石碑
//    public GameObject waterPrefab;

//    [Header("Scripts Instance References 脚本实例引用")]
//    public GameObject signalCenterObject;
//    private ISignalCenter signalCenter;

//    private int mapSeed;
//    private Vector3 lastRefreshPosition;

//    private Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();

//    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
//    private TextMeshProUGUI loadingText;
//    private Vector3 cameraPosition = new Vector3(0f, 220f, -10f);
//    private Vector3 crystalPosition = new Vector3(0f, 210f, 0f);

//    private Camera mainCamera;
//    private Coroutine loadCoroutine;
//    private float elapsedTime = 0f;

//    private Dictionary<Vector3, List<Vector3>> edgeVertexNormalsCache = new Dictionary<Vector3, List<Vector3>>();
//    private Dictionary<Vector3, Vector3> averagedNormalsCache = new Dictionary<Vector3, Vector3>();
//    private List<Vector3> tempNormalList = new List<Vector3>(); // 用于临时存储

//    // 线程安全的队列，用于存储待生成的区块数据
//    private ConcurrentQueue<ChunkGenerationData> chunksToGenerate = new ConcurrentQueue<ChunkGenerationData>();

//    // 区块生成数据（可在其他线程中安全创建）
//    private class ChunkGenerationData
//    {
//        public int chunkX;
//        public int chunkZ;
//        public Vector3[] vertices;
//        public int[] triangles;
//        public Vector2[] uv;
//        public Vector3[] normals;
//        public Color[] colors;
//        public List<Vector3> items;
//        public bool haveWater;
//    }

//    private Dictionary<GameObject, IObjectPool<GameObject>> pools = new Dictionary<GameObject, IObjectPool<GameObject>>();
//    private Dictionary<GameObject, IObjectPool<GameObject>> itemToPool = new Dictionary<GameObject, IObjectPool<GameObject>>();

//    // 存储所有生成的区块数据，用于后续法线平滑
//    private Dictionary<Vector2Int, ChunkMeshData> generatedChunks = new Dictionary<Vector2Int, ChunkMeshData>();

//    // 区块Mesh数据
//    private class ChunkMeshData
//    {
//        public GameObject gameObject;
//        public Mesh mesh;
//        public Vector3[] vertices;
//        public Vector3[] normals;
//        public int chunkX;
//        public int chunkZ;
//    }

//    // 迷宫数据
//    private struct MazeData
//    {
//        public int minX;
//        public int minZ;
//        public int maxX;
//        public int maxZ;
//        public float Y;
//    }

//    //private Queue<MazeData> MazeDataQueue = new Queue<MazeData>();
//    private GameObject TerrainGroup;
//    private GameObject TerrainItemGroup;
//    private GameObject WaveGroup;
//    private GameObject WaterGroup;

//    // 懒加载获取对象池
//    private IObjectPool<GameObject> GetPool(GameObject prefab)
//    {
//        if (!pools.ContainsKey(prefab))
//        {
//            // 创建对象池
//            pools[prefab] = new ObjectPool<GameObject>(
//                createFunc: () => Instantiate(prefab),
//                actionOnGet: (obj) => obj.SetActive(true),
//                actionOnRelease: (obj) => obj.SetActive(false),
//                actionOnDestroy: (obj) => Destroy(obj),
//                collectionCheck: true,
//                defaultCapacity: 30,
//                maxSize: 500
//            );
//        }
//        return pools[prefab];
//    }

//    void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//            loadingText = loadingTextObject.GetComponent<TextMeshProUGUI>();
//        }
//        else
//        {
//            Debug.LogWarning("重复的 World 实例被销毁！");
//            Destroy(gameObject);
//        }
//    }

//    void Start()
//    {
//        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
//        signalCenter.Subscribe(SignalType.EnterWorld, OnEnterWorld);
//        signalCenter.Subscribe(SignalType.ChangeUIState, OnChangeUIState);
//        mainCamera = Camera.main;
//        if (mainCamera == null)
//        {
//            Debug.LogError("Main Camera not found!");
//        }
//        if (crystal == null)
//        {
//            Debug.LogError("Crystal reference not assigned!");
//        }
//    }

//    public void OnEnterWorld(GameObject sender, object data)
//    {
//        WelcomeWorldObject.SetActive(false);
//        if (GameStateManager.Get() != GameState.LoadNotStarted && GameStateManager.Get() != GameState.LoadingCompleted)
//        {
//            Debug.LogWarning("In OnEnterWorld(): Wrong loadState!");
//            return;
//        }
//        if (loadCoroutine != null)
//        {
//            StopCoroutine(loadCoroutine);
//        }
//        loadCoroutine = StartCoroutine(LoadWorldCoroutine());
//    }

//    private IEnumerator LoadWorldCoroutine()
//    {
//        GameStateManager.Set(GameState.LoadBeginning);
//        //LightingManager.SetSkybox(LightingManager.Instance.WR134morroneSkyboxMaterial);
//        loadingTextObject.SetActive(true);
//        InitializePositions();
//        crystal.SetActive(true);
//        StartCoroutine(AnimateCrystalWhileLoading());
//        StartCoroutine(UpdateLoadingText());
//        StartCoroutine(GenerateWaveCoroutine());
//        yield return StartCoroutine(CheckResourcesCoroutine());
//        yield return StartCoroutine(GenerateTerrainCoroutine());
//        lastRefreshPosition = player.transform.position;
//        GameStateManager.Set(GameState.LoadingCompleted);
//        yield return _waitForSeconds0_5;
//        var WelcomeWorldRectTransform = WelcomeWorldObject.GetComponent<RectTransform>();
//        Vector2 anchoredPos = WelcomeWorldRectTransform.anchoredPosition;
//        WelcomeWorldObject.SetActive(true);
//        const float AnimationSpeed = 1f, WorldDisplayTime = 3f;
//        signalCenter.Emit(SignalType.WorldLoaded, gameObject);
//        DOVirtual.Float(-1024f, 0f, AnimationSpeed, (float x) =>
//        {
//            anchoredPos.x = x;
//            WelcomeWorldRectTransform.anchoredPosition = anchoredPos;
//        }).SetEase(Ease.OutBack);
//        DOVirtual.DelayedCall(AnimationSpeed + WorldDisplayTime, () =>
//        {
//            DOVirtual.Float(0f, -1024f, AnimationSpeed, (float x) =>
//            {
//                anchoredPos.x = x;
//                WelcomeWorldRectTransform.anchoredPosition = anchoredPos;
//            }).SetEase(Ease.OutBack);
//            DOVirtual.DelayedCall(AnimationSpeed, () =>
//            {
//                WelcomeWorldObject.SetActive(false);
//                ITutorial interTutorial = TutorialObject.GetComponent<ITutorial>();
//                interTutorial?.EnterTutorial(TutorialSet.BeginTutorial);
//            });
//        });
//    }

//    private void InitializePositions()
//    {
//        PlayerMovement.Instance.gameObject.transform.position = cameraPosition;
//        PlayerMovement.Instance.gameObject.transform.rotation = Quaternion.Euler(30, 0, 0);
//        crystal.transform.position = crystalPosition;
//    }

//    private IEnumerator CheckResourcesCoroutine()
//    {
//        Debug.Log("Checking resources...");
//        yield return _waitForSeconds0_5;
//    }

//    private IEnumerator GenerateWaveCoroutine()
//    {
//        Debug.Log("Generating wave...");
//        int seed = new System.Random().Next();
//        var noise = new FastNoiseLite(seed);
//        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
//        //terrainSettings
//        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
//        noise.SetFractalOctaves(4);       // 叠加n层细节（倍频数）
//        noise.SetFractalLacunarity(2.0f); // 高频细节的尺度倍数（余隙度）
//        noise.SetFractalGain(0.5f);       // 每层细节的强度衰减（持续度）
//        noise.SetFrequency(0.05f);        // 噪声频率
//        const int size = 32;

//        SpherePerfab.SetActive(true);
//        WaveGroup = new GameObject("WaveGroup");

//        // 定义三条波浪的配置
//        WaveConfig[] waves = new WaveConfig[]
//        {
//    new WaveConfig { zOffset = 0,   heightOffset = 202f, noiseOffsetX = 0,    noiseOffsetY = 0    },      // 第一条：原来的位置
//    new WaveConfig { zOffset = -3,  heightOffset = 200f, noiseOffsetX = 100,  noiseOffsetY = 100  },    // 第二条：z偏移-3，高度基线-5
//    new WaveConfig { zOffset = -6,  heightOffset = 198f, noiseOffsetX = 200,  noiseOffsetY = 200  }     // 第三条：z偏移-6，高度基线-10
//        };
//        // 存储每条波浪的数据（每条波浪独立存储自己的 gameObjects, renderers, transforms）
//        List<List<GameObject>> allGameObjects = new List<List<GameObject>>();
//        List<List<Renderer[]>> allRenderers = new List<List<Renderer[]>>();
//        List<List<Transform>> allTransforms = new List<List<Transform>>();
//        // 为每条波浪初始化存储列表
//        for (int w = 0; w < waves.Length; w++)
//        {
//            allGameObjects.Add(new List<GameObject>());
//            allRenderers.Add(new List<Renderer[]>());
//            allTransforms.Add(new List<Transform>());
//        }
//        float lastTime;
//        //for (int y = -size; y < 1048576; y++)
//        for (int y = -size; ; y++)
//        {
//            lastTime = Time.time;
//            if (y == -size)
//            {
//                // 初始创建：为每条波浪分别创建一排球体
//                for (int w = 0; w < waves.Length; w++)
//                {
//                    WaveConfig config = waves[w];
//                    for (int x = -size; x < size; x++)
//                    {
//                        // 每条波浪使用不同的噪声偏移
//                        float noiseValue = noise.GetNoise(
//                            (x + config.noiseOffsetX) / 8f,
//                            (y + config.noiseOffsetY) / 8f
//                        );
//                        // 计算高度：基线高度 + 噪声波动
//                        float height = config.heightOffset + noiseValue * 15f;
//                        float zPos = 20 + config.zOffset;  // 20是原来的z位置
//                        GameObject lodParent = Instantiate(
//                            SpherePerfab,
//                            new Vector3(x, height, zPos),
//                            Quaternion.identity
//                        );
//                        lodParent.transform.SetParent(WaveGroup.transform);
//                        Color color = Color.HSVToRGB(
//                            (noiseValue + 1f) / 2f * 0.75f,
//                            1.0f,
//                            0.7f + (noiseValue + 1f) / 2f * 0.3f
//                        );
//                        allRenderers[w].Add(lodParent.GetComponentsInChildren<Renderer>());
//                        allTransforms[w].Add(lodParent.GetComponent<Transform>());

//                        foreach (Renderer renderer in allRenderers[w][allRenderers[w].Count - 1])
//                        {
//                            renderer.material.color = color;
//                        }
//                        allGameObjects[w].Add(lodParent);
//                    }
//                }
//                SpherePerfab.SetActive(false);
//            }
//            else
//            {
//                for (int w = 0; w < waves.Length; w++)
//                {
//                    WaveConfig config = waves[w];
//                    for (int i = 0; i < allGameObjects[w].Count; i++)
//                    {
//                        int x = i - size;
//                        float noiseValue = noise.GetNoise(
//                            (x + config.noiseOffsetX) / 8f,
//                            (y + config.noiseOffsetY) / 8f
//                        );
//                        Color color = Color.HSVToRGB(
//                            (noiseValue + 1f) / 2f * 0.75f,
//                            1.0f,
//                            0.7f + (noiseValue + 1f) / 2f * 0.3f
//                        );
//                        foreach (Renderer renderer in allRenderers[w][i])
//                        {
//                            renderer.material.color = color;
//                        }
//                        float newHeight = config.heightOffset + noiseValue * 15f;
//                        Vector3 pos = allTransforms[w][i].position;
//                        allTransforms[w][i].position = new Vector3(pos.x, newHeight, pos.z);
//                    }
//                }
//            }
//            float waitTime = WorldConfig.loadSPF - (Time.time - lastTime);
//            yield return new WaitForSeconds(waitTime);
//        }
//    }

//    private IEnumerator AnimateCrystalWhileLoading()
//    {
//        elapsedTime = 0f;
//        float deltaTime;
//        //while (gameState != GameState.LoadingCompleted || elapsedTime < MinimumLoadingTime)
//        while (true)
//        {
//            if (crystal == null || !crystal.activeSelf)
//            {
//                Debug.LogWarning("In AnimateCrystalWhileLoading(): Crystal is null or inactive!");
//                break;
//            }
//            deltaTime = Time.deltaTime;
//            elapsedTime += deltaTime;
//            float scale = Mathf.Abs(Mathf.Sin(elapsedTime * 2f)) / 20f + 0.5f;
//            crystal.transform.localScale = new Vector3(scale, scale, scale);
//            crystal.transform.position = new Vector3(crystal.transform.position.x,
//                crystal.transform.position.y + Mathf.Sin(elapsedTime) / 1000f,
//                crystal.transform.position.z);
//            crystal.transform.Rotate(0, 0, deltaTime * 45f);
//            yield return null;
//        }
//        crystal.SetActive(false);
//        loadingTextObject.SetActive(false);
//    }

//    private IEnumerator UpdateLoadingText()
//    {
//        while (true)
//        {
//            if (GameStateManager.Get() == GameState.LoadingCompleted) { break; }
//            loadingText.SetText("Loading.");
//            yield return _waitForSeconds0_5;
//            if (GameStateManager.Get() == GameState.LoadingCompleted) { break; }
//            loadingText.SetText("Loading..");
//            yield return _waitForSeconds0_5;
//            if (GameStateManager.Get() == GameState.LoadingCompleted) { break; }
//            loadingText.SetText("Loading...");
//            yield return _waitForSeconds0_5;
//        }
//        loadingTextObject.SetActive(false);
//    }

//    float MapFloat(float value, float from1, float from2, float to1, float to2)
//    {
//        return (value - from1) / (from2 - from1) * (to2 - to1) + to1;
//    }

//    private Coroutine refreshCoroutine;
//    private IEnumerator RefreshTerrain(Vector2Int centerChunk)
//    {
//        float frameStartTime = Time.realtimeSinceStartup;
//        // 1. 删除超出视距的区块（保留原有分帧逻辑）
//        List<Vector2Int> willRemove = new List<Vector2Int>();
//        foreach (KeyValuePair<Vector2Int, ChunkData> kvp in chunks)
//        {
//            if (Vector2Int.Distance(kvp.Key, centerChunk) > Convert.ToInt32(Alpha.gameSettings.Categories["Graphics"]["ViewDistance"].CurrentValue))
//            {
//                if (kvp.Value.terrainObject != null)
//                    Destroy(kvp.Value.terrainObject);
//                foreach (GameObject obj in kvp.Value.items)
//                {
//                    if (obj != null && itemToPool.TryGetValue(obj, out var pool))
//                        pool.Release(obj);
//                    else
//                        Destroy(obj);
//                    if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
//                    {
//                        yield return null;
//                        frameStartTime = Time.realtimeSinceStartup;
//                    }
//                }
//                kvp.Value.items.Clear();
//                willRemove.Add(kvp.Key);
//            }
//            if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
//            {
//                yield return null;
//                frameStartTime = Time.realtimeSinceStartup;
//            }
//        }
//        foreach (var key in willRemove)
//        {
//            chunks.Remove(key);
//            generatedChunks.Remove(key);
//        }

//        // 2. 生成新视距内缺失的区块（异步）
//        List<Task> tasks = new List<Task>();
//        int kSquared = Convert.ToInt32(Alpha.gameSettings.Categories["Graphics"]["ViewDistance"].CurrentValue) *
//            Convert.ToInt32(Alpha.gameSettings.Categories["Graphics"]["ViewDistance"].CurrentValue);
//        for (int i = -Convert.ToInt32(Alpha.gameSettings.Categories["Graphics"]["ViewDistance"].CurrentValue);
//            i <= Convert.ToInt32(Alpha.gameSettings.Categories["Graphics"]["ViewDistance"].CurrentValue); i++)
//        {
//            int maxJ = (int)Math.Sqrt(kSquared - i * i);
//            for (int j = -maxJ; j <= maxJ; j++)
//            {
//                int chunkX = i + centerChunk.x;
//                int chunkZ = j + centerChunk.y;
//                Vector2Int coord = new Vector2Int(chunkX, chunkZ);
//                if (!chunks.ContainsKey(coord) || chunks[coord].terrainObject == null)
//                {
//                    Task task = Task.Run(() => GenerateChunkDataAsync(mapSeed, chunkX, chunkZ));
//                    tasks.Add(task);
//                }
//            }
//        }
//        while (tasks.Any(t => !t.IsCompleted))
//            yield return null;

//        // 3. 实例化区块和物品（分帧）
//        frameStartTime = Time.realtimeSinceStartup;
//        while (chunksToGenerate.TryDequeue(out ChunkGenerationData data))
//        {
//            if (data.haveWater)
//            {
//                GameObject obj = Instantiate(waterPrefab,
//                    new Vector3(data.chunkX * WorldConfig.chunkSize + WorldConfig.chunkSize / 2,
//                    WorldConfig.waterLevelHeight * WorldConfig.heightMultiplier,
//                    data.chunkZ * WorldConfig.chunkSize + WorldConfig.chunkSize / 2), Quaternion.identity);
//                obj.transform.localScale = new Vector3(WorldConfig.chunkSize, 1, WorldConfig.chunkSize);
//                obj.transform.name = $"Water_{data.chunkX}_{data.chunkZ}";
//                obj.transform.SetParent(WaterGroup.transform);
//                obj.SetActive(true);
//                Vector2Int chunkCoord = new Vector2Int(data.chunkX, data.chunkZ);
//                if (!chunks.ContainsKey(chunkCoord))
//                    chunks[chunkCoord] = new ChunkData();
//                chunks[chunkCoord].items.Add(obj);
//            }

//            // 防御性检查：确保 data.items 不为 null
//            if (data.items == null)
//            {
//                Debug.LogError($"data.items is null for chunk ({data.chunkX},{data.chunkZ})");
//                continue;
//            }

//            Vector2Int key = new Vector2Int(data.chunkX, data.chunkZ);
//            if (!chunks.ContainsKey(key))
//                chunks[key] = new ChunkData();

//            chunks[key].terrainObject = CreateChunkGameObject(data);

//            // 物品生成
//            foreach (var u in data.items)
//            {
//                DeterministicRandom rng = new DeterministicRandom(mapSeed, (int)u.x, (int)u.z, (int)u.y);
//                //System.Random rng = new System.Random(mapSeed + (int)(u.x) * 2 + (int)(u.z) * 3 + (int)(u.y) * 5);
//                GameObject gcItem = null;
//                double val = rng.NextDouble();
//                if (u.y >= -0.5f && u.y <= 0.8f)
//                {
//                    if (val < 0.1)
//                    {
//                        val = rng.NextDouble();
//                        if (val < 0.3) gcItem = CemeteryPebbles1Perfab;
//                        else if (val < 0.6) gcItem = CemeteryPebbles2Perfab;
//                        else if (val < 0.62) gcItem = RockMossGrown1;
//                        else if (val < 0.64) gcItem = RockMossGrown2;
//                        else if (val < 0.65) gcItem = RockPileForestMoss1;
//                        else if (val < 0.66) gcItem = RockPileForestMoss2;
//                    }
//                    else
//                    {
//                        val = rng.NextDouble();
//                        if (val < 0.15) gcItem = Birch1Perfab;
//                        else if (val < 0.3) gcItem = Birch2Perfab;
//                        else if (val < 0.45) gcItem = Tree1Perfab;
//                        else if (val < 0.6) gcItem = Tree2Perfab;
//                        else if (val < 0.68) gcItem = Daffodil;
//                        else if (val < 0.76) gcItem = Grass1;
//                        else if (val < 0.84) gcItem = Grass2;
//                        else if (val < 0.87) gcItem = Hyacinth;
//                        else if (val < 0.9) gcItem = MushroomFantasyOrange1;
//                        else if (val < 0.92) gcItem = MushroomFantasyOrange2;
//                        else if (val < 0.94) gcItem = MushroomFantasyPurple1;
//                        else if (val < 0.96) gcItem = MushroomFantasyPurple2;
//                        else if (val <= 1.0) gcItem = Sunflower;
//                    }
//                }
//                else
//                {
//                    if (val < 0.2) gcItem = CemeteryPebbles1Perfab;
//                    else if (val < 0.4) gcItem = CemeteryPebbles2Perfab;
//                    else if (val < 0.42) gcItem = RockMossGrown1;
//                    else if (val < 0.44) gcItem = RockMossGrown2;
//                    else if (val < 0.45) gcItem = RockPileForestMoss1;
//                    else if (val < 0.46) gcItem = RockPileForestMoss2;
//                }
//                if (u.y < 0.8f && u.y > 0f && rng.NextDouble() < 0.005)
//                {
//                    gcItem = Stele;
//                }

//                var v = new Vector3(u.x, u.y * WorldConfig.heightMultiplier - 0.5f, u.z);
//                if (gcItem != null)
//                {
//                    IObjectPool<GameObject> pool = GetPool(gcItem);
//                    GameObject obj = pool.Get();
//                    obj.transform.SetPositionAndRotation(v, Quaternion.identity);
//                    itemToPool[obj] = pool;
//                    //obj.transform.scale = obj.transform.localScale * MapFloat((float)rng.NextDouble(), -1f, 1f, 0.8f, 1.3f);

//                    obj.transform.localScale = gcItem.transform.localScale * MapFloat((float)rng.NextDouble(), -1f, 1f, 0.8f, 1.3f);
//                    Vector2Int chunkCoord = new Vector2Int(data.chunkX, data.chunkZ);
//                    if (!chunks.ContainsKey(chunkCoord))
//                        chunks[chunkCoord] = new ChunkData();
//                    obj.transform.SetParent(TerrainItemGroup.transform);
//                    chunks[chunkCoord].items.Add(obj);
//                }

//                if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
//                {
//                    yield return null;
//                    frameStartTime = Time.realtimeSinceStartup;
//                }
//            }

//            if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
//            {
//                yield return null;
//                frameStartTime = Time.realtimeSinceStartup;
//            }
//        }

//        yield return StartCoroutine(SmoothAllEdgeNormals());
//        refreshCoroutine = null;
//    }

//    private IEnumerator GenerateTerrainCoroutine()
//    {
//        Debug.Log("Generating terrain...");
//        float startTime = Time.time;
//        mapSeed = new System.Random().Next();
//        //MazeDataQueue.Clear();
//        TerrainGroup = new GameObject("TerrainGroup");
//        TerrainItemGroup = new GameObject("TerrainItemGroup");
//        WaterGroup = new GameObject("WaterGroup");
//        yield return StartCoroutine(RefreshTerrain(new Vector2Int(0, 0)));
//        //CreateMaze(new Vector3(0, 150, 0), new Vector2Int(1, 1), new Vector2Int(64, 64), 1f, 30f);
//        Debug.Log($"Terrain generation completed! Total time: {Time.time - startTime} seconds.");
//    }

//    private GameObject CreateChunkGameObject(ChunkGenerationData data)
//    {
//        var terrainObject = new GameObject($"ProceduralTerrain_Chunk({data.chunkX},{data.chunkZ})");
//        MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
//        MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();
//        MeshCollider meshCollider = terrainObject.AddComponent<MeshCollider>();
//        meshRenderer.material = TerrainMaterial;
//        //meshCollider.convex = true;

//        var mesh = new Mesh
//        {
//            vertices = data.vertices,
//            triangles = data.triangles,
//            uv = data.uv,
//            normals = data.normals,
//            colors = data.colors
//        };
//        meshRenderer.material.EnableKeyword("VERTEX_COLORS_ON");
//        mesh.RecalculateBounds();
//        mesh.RecalculateTangents();

//        meshFilter.mesh = mesh;
//        meshCollider.sharedMesh = mesh;

//        // 存储区块数据，用于后续法线平滑
//        generatedChunks[new Vector2Int(data.chunkX, data.chunkZ)] = new ChunkMeshData
//        {
//            gameObject = terrainObject,
//            mesh = mesh,
//            vertices = data.vertices,
//            normals = data.normals,
//            chunkX = data.chunkX,
//            chunkZ = data.chunkZ
//        };

//        terrainObject.transform.SetParent(TerrainGroup.transform);
//        return terrainObject;
//    }

//    private FastNoiseLite GetNoiseObject(int seed)
//    {
//        var noise = new FastNoiseLite(seed);
//        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
//        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
//        noise.SetFractalOctaves(6);
//        noise.SetFractalLacunarity(2.5f);
//        noise.SetFractalGain(0.4f);
//        noise.SetFrequency(0.00125f);
//        return noise;
//    }

//    private void GenerateChunkDataAsync(int seed, int chunkX, int chunkZ)
//    {
//        var noise = GetNoiseObject(seed);

//        int size = WorldConfig.chunkSize + 1;
//        int vertexCount = size * size;
//        int triangleCount = WorldConfig.chunkSize * WorldConfig.chunkSize * 6;

//        ChunkGenerationData data = new ChunkGenerationData();
//        data.chunkX = chunkX;
//        data.chunkZ = chunkZ;
//        data.vertices = new Vector3[vertexCount];
//        data.colors = new Color[vertexCount];
//        data.triangles = new int[triangleCount];
//        data.uv = new Vector2[vertexCount];
//        data.normals = new Vector3[vertexCount];
//        data.items = new List<Vector3>();

//        float h0 = 1f, h01 = 0.6f, h12 = 0.2f, h23 = -0.4f, h34 = -0.8f, h4 = -1f;
//        const double gcItemProbability = 0.002;
//        DeterministicRandom rng = new DeterministicRandom(seed, chunkX, chunkZ, 0);

//        for (int i = 0; i < size; i++)
//        {
//            for (int j = 0; j < size; j++)
//            {
//                float x = (chunkX * WorldConfig.chunkSize + i) * WorldConfig.xSpacing;
//                float z = (chunkZ * WorldConfig.chunkSize + j) * WorldConfig.zSpacing;
//                float y = noise.GetNoise(x, z);
//                data.vertices[i * size + j] = new Vector3(x, y * WorldConfig.heightMultiplier, z);
//                if (y < WorldConfig.waterLevelHeight)
//                {
//                    data.haveWater = true;
//                }

//                if (y >= h01)
//                    data.colors[i * size + j] = Color.Lerp(WorldConfig.color1, WorldConfig.color0, (y - h01) / (h0 - h01));
//                else if (y >= h12)
//                    data.colors[i * size + j] = Color.Lerp(WorldConfig.color2, WorldConfig.color1, (y - h12) / (h01 - h12));
//                else if (y >= h23)
//                    data.colors[i * size + j] = Color.Lerp(WorldConfig.color3, WorldConfig.color2, (y - h23) / (h12 - h23));
//                else if (y >= h34)
//                    data.colors[i * size + j] = Color.Lerp(WorldConfig.color4, WorldConfig.color3, (y - h34) / (h23 - h34));
//                else
//                    data.colors[i * size + j] = Color.Lerp(WorldConfig.color5, WorldConfig.color4, (y - h4) / (h34 - h4));

//                if (y >= -0.25f && y <= 0.9f && rng.NextDouble() < gcItemProbability)
//                    data.items.Add(new Vector3(x, y, z));
//            }
//        }

//        int triIndex = 0;
//        for (int i = 0; i < WorldConfig.chunkSize; i++)
//        {
//            for (int j = 0; j < WorldConfig.chunkSize; j++)
//            {
//                int current = i * size + j;
//                int nextRow = (i + 1) * size + j;
//                data.triangles[triIndex] = current;
//                data.triangles[triIndex + 1] = current + 1;
//                data.triangles[triIndex + 2] = nextRow;
//                data.triangles[triIndex + 3] = nextRow;
//                data.triangles[triIndex + 4] = current + 1;
//                data.triangles[triIndex + 5] = nextRow + 1;
//                triIndex += 6;
//            }
//        }

//        for (int i = 0; i < size; i++)
//            for (int j = 0; j < size; j++)
//                data.uv[i * size + j] = new Vector2((float)i / WorldConfig.chunkSize, (float)j / WorldConfig.chunkSize);

//        CalculateNormalsReuse(data.vertices, data.triangles, data.normals);

//        chunksToGenerate.Enqueue(data);
//    }

//    // 计算法线的方法
//    private void CalculateNormalsReuse(Vector3[] vertices, int[] triangles, Vector3[] externalNormals)
//    {
//        int triCount = triangles.Length / 3;
//        Vector3[] triangleNormals = new Vector3[triCount]; // 这个数组较小，但可进一步池化（暂略）

//        for (int i = 0; i < triangles.Length; i += 3)
//        {
//            Vector3 v0 = vertices[triangles[i]];
//            Vector3 v1 = vertices[triangles[i + 1]];
//            Vector3 v2 = vertices[triangles[i + 2]];
//            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
//            triangleNormals[i / 3] = normal;
//        }

//        // 清空外部法线数组
//        for (int i = 0; i < externalNormals.Length; i++)
//            externalNormals[i] = Vector3.zero;

//        // 累加三角形法线
//        for (int i = 0; i < triangles.Length; i += 3)
//        {
//            for (int j = 0; j < 3; j++)
//            {
//                int vIdx = triangles[i + j];
//                externalNormals[vIdx] += triangleNormals[i / 3];
//            }
//        }

//        // 归一化
//        for (int i = 0; i < externalNormals.Length; i++)
//            externalNormals[i].Normalize();
//    }

//    // 平滑所有区块的边缘法线
//    private IEnumerator SmoothAllEdgeNormals()
//    {
//        float frameStartTime = Time.realtimeSinceStartup;

//        // 清空缓存字典，但复用其内部 List 对象
//        foreach (var list in edgeVertexNormalsCache.Values)
//            list.Clear();
//        edgeVertexNormalsCache.Clear();
//        averagedNormalsCache.Clear();

//        // 第一步：收集所有边缘顶点的法线
//        foreach (var chunk in generatedChunks.Values)
//        {
//            Vector3[] vertices = chunk.vertices;
//            Vector3[] normals = chunk.normals;

//            for (int i = 0; i < vertices.Length; i++)
//            {
//                if (IsEdgeVertex(vertices[i], chunk.chunkX, chunk.chunkZ))
//                {
//                    Vector3 precisePos = RoundVector3(vertices[i]);
//                    if (!edgeVertexNormalsCache.TryGetValue(precisePos, out List<Vector3> list))
//                    {
//                        list = new List<Vector3>();
//                        edgeVertexNormalsCache[precisePos] = list;
//                    }
//                    list.Add(normals[i]);
//                }
//            }

//            if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
//            {
//                yield return null;
//                frameStartTime = Time.realtimeSinceStartup;
//            }
//        }

//        // 第二步：计算平均法线
//        foreach (var kvp in edgeVertexNormalsCache)
//        {
//            Vector3 avg = Vector3.zero;
//            foreach (Vector3 n in kvp.Value)
//                avg += n;
//            avg.Normalize();
//            averagedNormalsCache[kvp.Key] = avg;

//            if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
//            {
//                yield return null;
//                frameStartTime = Time.realtimeSinceStartup;
//            }
//        }

//        // 第三步：更新区块法线
//        foreach (var chunk in generatedChunks.Values)
//        {
//            bool updated = false;
//            for (int i = 0; i < chunk.vertices.Length; i++)
//            {
//                if (IsEdgeVertex(chunk.vertices[i], chunk.chunkX, chunk.chunkZ))
//                {
//                    Vector3 precisePos = RoundVector3(chunk.vertices[i]);
//                    if (averagedNormalsCache.TryGetValue(precisePos, out Vector3 newNormal))
//                    {
//                        chunk.normals[i] = newNormal;
//                        updated = true;
//                    }
//                }
//            }
//            if (updated)
//                chunk.mesh.normals = chunk.normals;

//            if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
//            {
//                yield return null;
//                frameStartTime = Time.realtimeSinceStartup;
//            }
//        }

//        Debug.Log($"边缘法线平滑完成，处理了 {averagedNormalsCache.Count} 个边缘顶点");
//    }

//    // 判断顶点是否在区块边缘
//    private bool IsEdgeVertex(Vector3 vertex, int chunkX, int chunkZ)
//    {
//        // 计算相对于区块的位置
//        float localX = (vertex.x / WorldConfig.xSpacing) - chunkX * WorldConfig.chunkSize;
//        float localZ = (vertex.z / WorldConfig.zSpacing) - chunkZ * WorldConfig.chunkSize;

//        // 检查是否在边缘（容差值）
//        const float epsilon = 0.001f;
//        return Mathf.Abs(localX) < epsilon ||
//                      Mathf.Abs(localX - WorldConfig.chunkSize) < epsilon ||
//                      Mathf.Abs(localZ) < epsilon ||
//                      Mathf.Abs(localZ - WorldConfig.chunkSize) < epsilon;
//    }

//    // 精确取整Vector3，用于字典Key
//    private Vector3 RoundVector3(Vector3 v)
//    {
//        // 由于浮点数精度问题，需要四舍五入到一定精度
//        const float precision = 0.0001f;
//        return new Vector3(
//            Mathf.Round(v.x / precision) * precision,
//            Mathf.Round(v.y / precision) * precision,
//            Mathf.Round(v.z / precision) * precision
//        );
//    }

//    void Update()
//    {
//        if (GameStateManager.Get() == GameState.LoadingCompleted)
//        {
//            if (Vector3.Distance(lastRefreshPosition, player.transform.position) > WorldConfig.chunkSize)
//            {
//                lastRefreshPosition = player.transform.position;
//                Vector2Int center = new Vector2Int(Mathf.FloorToInt(player.transform.position.x / (WorldConfig.xSpacing * WorldConfig.chunkSize)),
//                                                    Mathf.FloorToInt(player.transform.position.z / (WorldConfig.zSpacing * WorldConfig.chunkSize)));
//                if (refreshCoroutine == null)
//                {
//                    refreshCoroutine = StartCoroutine(RefreshTerrain(center));
//                }
//                else
//                {
//                    Debug.Log("refreshCoroutine is running.");
//                }
//            }
//        }
//    }

//    private void OnDestroy()
//    {
//        foreach (var pool in pools.Values)
//        {
//            pool.Clear();
//        }
//        pools.Clear();
//        itemToPool.Clear();
//    }
//}

////using DG.Tweening;
////using NaughtyAttributes;
////using System;
////using System.Collections;
////using System.Collections.Concurrent;
////using System.Collections.Generic;
////using System.Linq;
////using System.Threading.Tasks;
////using TMPro;
////using UnityEngine;
////using UnityEngine.Pool;

////public class WaveConfig
////{
////    public float zOffset;      // Z轴偏移量
////    public float heightOffset; // 高度基线偏移
////    public float noiseOffsetX; // 噪声X偏移
////    public float noiseOffsetY; // 噪声Y偏移
////}

////public struct DeterministicRandom
////{
////    private uint state;

////    public DeterministicRandom(int seed, int x, int z, int y)
////    {
////        state = (uint)HashCode.Combine(seed, x, z, y);
////        state = (state * 0x9e3779b9) ^ (state >> 17);
////    }

////    public double NextDouble()
////    {
////        state = state * 0x9e3779b9 + 1;
////        return (double)state / uint.MaxValue;
////    }
////}

////public class TerrainChunk
////{
////    public Vector2Int gridPos; // 区块网格坐标 (x, z)
////    public Vector3 worldPos;   // 区块中心或左下角世界坐标
////}

////public class World : MonoBehaviour
////{
////    public static World Instance;

////    [Header("GameObject 游戏对象")]
////    public GameObject TutorialObject;

////    public GameObject player;
////    public GameObject crystal;
////    public GameObject loadingTextObject;
////    public GameObject SpherePerfab;
////    public GameObject WelcomeWorldObject;

////    public GameObject SettingsButtonObject;
////    public GameObject MinimapObject;
////    public GameObject DebugInfoObject;
////    public GameObject ButtonPromptsObject; // 受其他脚本控制
////    public GameObject PositionObject;

////    private bool SettingsButtonLastState = true;
////    private bool MinimapLastState = true;
////    private bool DebugInfoLastState = true;
////    private bool PositionLastState = true;

////    public void ClearUI()
////    {
////        States.IsLockUI = true;
////        SettingsButtonLastState = SettingsButtonObject.activeSelf;
////        MinimapLastState = MinimapObject.activeSelf;
////        DebugInfoLastState = DebugInfoObject.activeSelf;
////        PositionLastState = PositionObject.activeSelf;

////        SettingsButtonObject.SetActive(false);
////        MinimapObject.SetActive(false);
////        DebugInfoObject.SetActive(false);
////        PositionObject.SetActive(false);

////        ButtonPromptsObject.SetActive(false);
////    }

////    public void RecoverUI()
////    {
////        States.IsLockUI = false;
////        SettingsButtonObject.SetActive(SettingsButtonLastState);
////        MinimapObject.SetActive(MinimapLastState);
////        DebugInfoObject.SetActive(DebugInfoLastState);
////        PositionObject.SetActive(PositionLastState);
////    }

////    public void OnChangeUIState(GameObject sender, object data)
////    {
////        if ((int)data == 0)
////        {
////            ClearUI();
////        }
////        else
////        {
////            RecoverUI();
////        }
////    }

////    [Header("Terrain Settings 地形设置")]
////    public Material TerrainMaterial;

////    [Header("Item Settings 物件设置")]
////    [ShowAssetPreview] public GameObject Birch1Perfab;
////    [ShowAssetPreview] public GameObject Birch2Perfab;
////    [ShowAssetPreview] public GameObject Tree1Perfab;
////    [ShowAssetPreview] public GameObject Tree2Perfab;
////    [ShowAssetPreview] public GameObject CemeteryPebbles1Perfab;
////    [ShowAssetPreview] public GameObject CemeteryPebbles2Perfab;
////    [ShowAssetPreview] public GameObject Daffodil;
////    [ShowAssetPreview] public GameObject Grass1;
////    [ShowAssetPreview] public GameObject Grass2;
////    [ShowAssetPreview] public GameObject Hyacinth;
////    [ShowAssetPreview] public GameObject MushroomFantasyOrange1;
////    [ShowAssetPreview] public GameObject MushroomFantasyOrange2;
////    [ShowAssetPreview] public GameObject MushroomFantasyPurple1;
////    [ShowAssetPreview] public GameObject MushroomFantasyPurple2;
////    [ShowAssetPreview] public GameObject RockMossGrown1;
////    [ShowAssetPreview] public GameObject RockMossGrown2;
////    [ShowAssetPreview] public GameObject RockPileForestMoss1;
////    [ShowAssetPreview] public GameObject RockPileForestMoss2;
////    [ShowAssetPreview] public GameObject Sunflower;
////    [ShowAssetPreview] public GameObject Stele; // 石碑
////    public GameObject waterPrefab;

////    [Header("Scripts Instance References 脚本实例引用")]
////    public GameObject signalCenterObject;
////    private ISignalCenter signalCenter;

////    private int mapSeed;
////    private Vector3 lastRefreshPosition;

////    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
////    private TextMeshProUGUI loadingText;
////    private Vector3 cameraPosition = new Vector3(0f, 220f, -10f);
////    private Vector3 crystalPosition = new Vector3(0f, 210f, 0f);

////    private Camera mainCamera;
////    private Coroutine loadCoroutine;
////    private float elapsedTime = 0f;

////    private Dictionary<Vector3, List<Vector3>> edgeVertexNormalsCache = new Dictionary<Vector3, List<Vector3>>();
////    private Dictionary<Vector3, Vector3> averagedNormalsCache = new Dictionary<Vector3, Vector3>();
////    private List<Vector3> tempNormalList = new List<Vector3>(); // 用于临时存储

////    // 线程安全的队列，用于存储待生成的区块数据
////    private ConcurrentQueue<ChunkGenerationData> chunksToGenerate = new ConcurrentQueue<ChunkGenerationData>();

////    // 区块生成数据（可在其他线程中安全创建）
////    private class ChunkGenerationData
////    {
////        public int chunkX;
////        public int chunkZ;
////        public Vector3[] vertices;
////        public int[] triangles;
////        public Vector2[] uv;
////        public Vector3[] normals;
////        public Color[] colors;
////        public List<Vector3> items;
////        public bool haveWater;
////    }

////    private Dictionary<GameObject, IObjectPool<GameObject>> pools = new Dictionary<GameObject, IObjectPool<GameObject>>();
////    private Dictionary<GameObject, IObjectPool<GameObject>> itemToPool = new Dictionary<GameObject, IObjectPool<GameObject>>();

////    // 存储所有生成的区块数据，用于后续法线平滑
////    private Dictionary<Vector2Int, ChunkMeshData> generatedChunks = new Dictionary<Vector2Int, ChunkMeshData>();

////    // 区块Mesh数据
////    private class ChunkMeshData
////    {
////        public GameObject gameObject;
////        public Mesh mesh;
////        public Vector3[] vertices;
////        public Vector3[] normals;
////        public int chunkX;
////        public int chunkZ;
////    }

////    // 迷宫数据
////    private struct MazeData
////    {
////        public int minX;
////        public int minZ;
////        public int maxX;
////        public int maxZ;
////        public float Y;
////    }

////    //private Queue<MazeData> MazeDataQueue = new Queue<MazeData>();
////    private GameObject TerrainGroup;
////    private GameObject TerrainItemGroup;
////    private GameObject WaveGroup;
////    private GameObject WaterGroup;

////    // 懒加载获取对象池
////    private IObjectPool<GameObject> GetPool(GameObject prefab)
////    {
////        if (!pools.ContainsKey(prefab))
////        {
////            // 创建对象池
////            pools[prefab] = new ObjectPool<GameObject>(
////                createFunc: () => Instantiate(prefab),
////                actionOnGet: (obj) => obj.SetActive(true),
////                actionOnRelease: (obj) => obj.SetActive(false),
////                actionOnDestroy: (obj) => Destroy(obj),
////                collectionCheck: true,
////                defaultCapacity: 30,
////                maxSize: 500
////            );
////        }
////        return pools[prefab];
////    }

////    void Awake()
////    {
////        if (Instance == null)
////        {
////            Instance = this;
////            loadingText = loadingTextObject.GetComponent<TextMeshProUGUI>();
////        }
////        else
////        {
////            Debug.LogWarning("重复的 World 实例被销毁！");
////            Destroy(gameObject);
////        }
////    }

////    void Start()
////    {
////        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
////        signalCenter.Subscribe(SignalType.EnterWorld, OnEnterWorld);
////        signalCenter.Subscribe(SignalType.ChangeUIState, OnChangeUIState);
////        mainCamera = Camera.main;
////        if (mainCamera == null)
////        {
////            Debug.LogError("Main Camera not found!");
////        }
////        if (crystal == null)
////        {
////            Debug.LogError("Crystal reference not assigned!");
////        }
////    }

////    public void OnEnterWorld(GameObject sender, object data)
////    {
////        WelcomeWorldObject.SetActive(false);
////        if (GameStateManager.Get() != GameState.LoadNotStarted && GameStateManager.Get() != GameState.LoadingCompleted)
////        {
////            Debug.LogWarning("In OnEnterWorld(): Wrong loadState!");
////            return;
////        }
////        if (loadCoroutine != null)
////        {
////            StopCoroutine(loadCoroutine);
////        }
////        loadCoroutine = StartCoroutine(LoadWorldCoroutine());
////    }

////    private IEnumerator LoadWorldCoroutine()
////    {
////        GameStateManager.Set(GameState.LoadBeginning);
////        //LightingManager.SetSkybox(LightingManager.Instance.WR134morroneSkyboxMaterial);
////        loadingTextObject.SetActive(true);
////        InitializePositions();
////        crystal.SetActive(true);
////        StartCoroutine(AnimateCrystalWhileLoading());
////        StartCoroutine(UpdateLoadingText());
////        StartCoroutine(GenerateWaveCoroutine());
////        yield return StartCoroutine(CheckResourcesCoroutine());
////        //yield return StartCoroutine(GenerateTerrainCoroutine());
////        lastRefreshPosition = player.transform.position;
////        GameStateManager.Set(GameState.LoadingCompleted);
////        yield return _waitForSeconds0_5;
////        var WelcomeWorldRectTransform = WelcomeWorldObject.GetComponent<RectTransform>();
////        Vector2 anchoredPos = WelcomeWorldRectTransform.anchoredPosition;
////        WelcomeWorldObject.SetActive(true);
////        const float AnimationSpeed = 1f, WorldDisplayTime = 3f;
////        signalCenter.Emit(SignalType.WorldLoaded, gameObject);
////        DOVirtual.Float(-1024f, 0f, AnimationSpeed, (float x) =>
////        {
////            anchoredPos.x = x;
////            WelcomeWorldRectTransform.anchoredPosition = anchoredPos;
////        }).SetEase(Ease.OutBack);
////        DOVirtual.DelayedCall(AnimationSpeed + WorldDisplayTime, () =>
////        {
////            DOVirtual.Float(0f, -1024f, AnimationSpeed, (float x) =>
////            {
////                anchoredPos.x = x;
////                WelcomeWorldRectTransform.anchoredPosition = anchoredPos;
////            }).SetEase(Ease.OutBack);
////            DOVirtual.DelayedCall(AnimationSpeed, () =>
////            {
////                WelcomeWorldObject.SetActive(false);
////                ITutorial interTutorial = TutorialObject.GetComponent<ITutorial>();
////                interTutorial?.EnterTutorial(TutorialSet.BeginTutorial);
////            });
////        });
////    }

////    private void InitializePositions()
////    {
////        PlayerMovement.Instance.gameObject.transform.position = cameraPosition;
////        PlayerMovement.Instance.gameObject.transform.rotation = Quaternion.Euler(30, 0, 0);
////        crystal.transform.position = crystalPosition;
////    }

////    private IEnumerator CheckResourcesCoroutine()
////    {
////        Debug.Log("Checking resources...");
////        yield return _waitForSeconds0_5;
////    }

////    private IEnumerator GenerateWaveCoroutine()
////    {
////        Debug.Log("Generating wave...");
////        int seed = new System.Random().Next();
////        var noise = new FastNoiseLite(seed);
////        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
////        //terrainSettings
////        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
////        noise.SetFractalOctaves(4);       // 叠加n层细节（倍频数）
////        noise.SetFractalLacunarity(2.0f); // 高频细节的尺度倍数（余隙度）
////        noise.SetFractalGain(0.5f);       // 每层细节的强度衰减（持续度）
////        noise.SetFrequency(0.05f);        // 噪声频率
////        const int size = 32;

////        SpherePerfab.SetActive(true);
////        WaveGroup = new GameObject("WaveGroup");

////        // 定义三条波浪的配置
////        WaveConfig[] waves = new WaveConfig[]
////        {
////    new WaveConfig { zOffset = 0,   heightOffset = 202f, noiseOffsetX = 0,    noiseOffsetY = 0    },      // 第一条：原来的位置
////    new WaveConfig { zOffset = -3,  heightOffset = 200f, noiseOffsetX = 100,  noiseOffsetY = 100  },    // 第二条：z偏移-3，高度基线-5
////    new WaveConfig { zOffset = -6,  heightOffset = 198f, noiseOffsetX = 200,  noiseOffsetY = 200  }     // 第三条：z偏移-6，高度基线-10
////        };
////        // 存储每条波浪的数据（每条波浪独立存储自己的 gameObjects, renderers, transforms）
////        List<List<GameObject>> allGameObjects = new List<List<GameObject>>();
////        List<List<Renderer[]>> allRenderers = new List<List<Renderer[]>>();
////        List<List<Transform>> allTransforms = new List<List<Transform>>();
////        // 为每条波浪初始化存储列表
////        for (int w = 0; w < waves.Length; w++)
////        {
////            allGameObjects.Add(new List<GameObject>());
////            allRenderers.Add(new List<Renderer[]>());
////            allTransforms.Add(new List<Transform>());
////        }
////        float lastTime;
////        //for (int y = -size; y < 1048576; y++)
////        for (int y = -size; ; y++)
////        {
////            lastTime = Time.time;
////            if (y == -size)
////            {
////                // 初始创建：为每条波浪分别创建一排球体
////                for (int w = 0; w < waves.Length; w++)
////                {
////                    WaveConfig config = waves[w];
////                    for (int x = -size; x < size; x++)
////                    {
////                        // 每条波浪使用不同的噪声偏移
////                        float noiseValue = noise.GetNoise(
////                            (x + config.noiseOffsetX) / 8f,
////                            (y + config.noiseOffsetY) / 8f
////                        );
////                        // 计算高度：基线高度 + 噪声波动
////                        float height = config.heightOffset + noiseValue * 15f;
////                        float zPos = 20 + config.zOffset;  // 20是原来的z位置
////                        GameObject lodParent = Instantiate(
////                            SpherePerfab,
////                            new Vector3(x, height, zPos),
////                            Quaternion.identity
////                        );
////                        lodParent.transform.SetParent(WaveGroup.transform);
////                        Color color = Color.HSVToRGB(
////                            (noiseValue + 1f) / 2f * 0.75f,
////                            1.0f,
////                            0.7f + (noiseValue + 1f) / 2f * 0.3f
////                        );
////                        allRenderers[w].Add(lodParent.GetComponentsInChildren<Renderer>());
////                        allTransforms[w].Add(lodParent.GetComponent<Transform>());

////                        foreach (Renderer renderer in allRenderers[w][allRenderers[w].Count - 1])
////                        {
////                            renderer.material.color = color;
////                        }
////                        allGameObjects[w].Add(lodParent);
////                    }
////                }
////                SpherePerfab.SetActive(false);
////            }
////            else
////            {
////                for (int w = 0; w < waves.Length; w++)
////                {
////                    WaveConfig config = waves[w];
////                    for (int i = 0; i < allGameObjects[w].Count; i++)
////                    {
////                        int x = i - size;
////                        float noiseValue = noise.GetNoise(
////                            (x + config.noiseOffsetX) / 8f,
////                            (y + config.noiseOffsetY) / 8f
////                        );
////                        Color color = Color.HSVToRGB(
////                            (noiseValue + 1f) / 2f * 0.75f,
////                            1.0f,
////                            0.7f + (noiseValue + 1f) / 2f * 0.3f
////                        );
////                        foreach (Renderer renderer in allRenderers[w][i])
////                        {
////                            renderer.material.color = color;
////                        }
////                        float newHeight = config.heightOffset + noiseValue * 15f;
////                        Vector3 pos = allTransforms[w][i].position;
////                        allTransforms[w][i].position = new Vector3(pos.x, newHeight, pos.z);
////                    }
////                }
////            }
////            float waitTime = WorldConfig.loadSPF - (Time.time - lastTime);
////            yield return new WaitForSeconds(waitTime);
////        }
////    }

////    private IEnumerator AnimateCrystalWhileLoading()
////    {
////        elapsedTime = 0f;
////        float deltaTime;
////        //while (gameState != GameState.LoadingCompleted || elapsedTime < MinimumLoadingTime)
////        while (true)
////        {
////            if (crystal == null || !crystal.activeSelf)
////            {
////                Debug.LogWarning("In AnimateCrystalWhileLoading(): Crystal is null or inactive!");
////                break;
////            }
////            deltaTime = Time.deltaTime;
////            elapsedTime += deltaTime;
////            float scale = Mathf.Abs(Mathf.Sin(elapsedTime * 2f)) / 20f + 0.5f;
////            crystal.transform.localScale = new Vector3(scale, scale, scale);
////            crystal.transform.position = new Vector3(crystal.transform.position.x,
////                crystal.transform.position.y + Mathf.Sin(elapsedTime) / 1000f,
////                crystal.transform.position.z);
////            crystal.transform.Rotate(0, 0, deltaTime * 45f);
////            yield return null;
////        }
////        crystal.SetActive(false);
////        loadingTextObject.SetActive(false);
////    }

////    private IEnumerator UpdateLoadingText()
////    {
////        while (true)
////        {
////            if (GameStateManager.Get() == GameState.LoadingCompleted) { break; }
////            loadingText.SetText("Loading.");
////            yield return _waitForSeconds0_5;
////            if (GameStateManager.Get() == GameState.LoadingCompleted) { break; }
////            loadingText.SetText("Loading..");
////            yield return _waitForSeconds0_5;
////            if (GameStateManager.Get() == GameState.LoadingCompleted) { break; }
////            loadingText.SetText("Loading...");
////            yield return _waitForSeconds0_5;
////        }
////        loadingTextObject.SetActive(false);
////    }

////    float MapFloat(float value, float from1, float from2, float to1, float to2)
////    {
////        return (value - from1) / (from2 - from1) * (to2 - to1) + to1;
////    }

////    private FastNoiseLite GetNoiseObject(int seed)
////    {
////        var noise = new FastNoiseLite(seed);
////        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
////        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
////        noise.SetFractalOctaves(6);
////        noise.SetFractalLacunarity(2.5f);
////        noise.SetFractalGain(0.4f);
////        noise.SetFrequency(0.00125f);
////        return noise;
////    }
////}