/*
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using static FastNoiseLite;
using static UnityEditor.PlayerSettings;

public class WorldObject
{
    public int ID;
    public Vector3 Position;
    public Vector3 Scale;
    public GameObject gameObject;
    public WorldObject(int ID = 0, Vector3 Position = new Vector3(), Vector3 Scale = new Vector3(), GameObject gameObject = null)
    {
        if (gameObject != null)
        {
            Position = gameObject.transform.position;
            Scale = gameObject.transform.localScale;
        }
        this.ID = ID;
        this.Position = Position;
        this.Scale = Scale;
        this.gameObject = gameObject;
    }
}

public struct ChunkData
{
    public Vector2Int ChunkPosition;
    public float[,] height;
    public List<WorldObject> worldObjects;
    public GameObject TerrainObject;
    public float[,,] splatmap;
    public GrassParticleData[] grassData;
    //public ChunkData()
    //{
    //    this.chunkPosition = new Vector2Int();
    //    this.height = new float[WorldConfig.chunkSize, WorldConfig.chunkSize];
    //    this.worldObjects = new List<WorldObject>();
    //    this.TerrainObject = null;
    //    this.haveWater = false;
    //}
    public ChunkData(Vector2Int ChunkPositiion, float[,] height, List<WorldObject> worldObjects, float[,,] splatmap, GrassParticleData[] grassData, GameObject TerrainObject = null)
    {
        this.ChunkPosition = ChunkPositiion;
        this.height = height;
        this.worldObjects = worldObjects;
        this.TerrainObject = TerrainObject;
        this.splatmap = splatmap;
        this.grassData = grassData;
        //height = new List<List<float>>(WorldConfig.chunkSize);
    }
}

public struct DeterministicRandom
{
    private uint state;
    public DeterministicRandom(int seed, int x, int z, int y)
    {
        state = (uint)HashCode.Combine(seed, x, z, y);
        state = (state * 0x9e3779b9) ^ (state >> 17);
    }

    public double NextDouble()
    {
        state = state * 0x9e3779b9 + 1;
        return (double)state / uint.MaxValue;
    }
}

public class NoiseSet
{
    public FastNoiseLite noise;
    public string styleName;
    public float bias; // 丰度偏移

    public NoiseSet(int seed, float frequency, int octaves, float lacunarity, float gain, float bias = 0f, NoiseType noiseType = NoiseType.OpenSimplex2, FractalType fractalType = FractalType.FBm)
    {
        noise = new FastNoiseLite(seed);
        noise.SetNoiseType(noiseType);
        noise.SetFractalType(fractalType);
        noise.SetFractalOctaves(octaves);
        noise.SetFractalLacunarity(lacunarity);
        noise.SetFractalGain(gain);
        noise.SetFrequency(frequency);
        this.bias = bias;
    }
}

public class TerrainLoader : MonoBehaviour
{
    public static TerrainLoader Instance;

    [Header("Enity 实体")]
    public GameObject player;

    [Header("Terrain Generation Options 地形生成参数")]
    public float distortionLevel = 200f;

    [Header("Terrain Textures 地形 Textures")]
    public Texture2D[] albedoMaps;  // 4种，顺序对应生物群系：plains, mountain, plateau, cliff
    public Texture2D[] maskMaps;
    public Texture2D[] normalMaps;
    public Material TerrainMaterial;

    [Header("Terrain Perfab 地形预制体")]
    public GameObject Ocean;
    //public GameObject waterPrefab;
    public GameObject GrassPrefab;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    private ISignalCenter signalCenter;

    private List<NoiseSet> TerrainNoiseSet = new List<NoiseSet>();
    private List<NoiseSet> BiomeNoiseSet = new List<NoiseSet>();
    private FastNoiseLite WarpNoise;
    private FastNoiseLite grassNoise;
    private float frameStartTime;
    private Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
    private Dictionary<int, ObjectPool<WorldObject>> WorldObjectPool = new Dictionary<int, ObjectPool<WorldObject>>();
    // ObjectPool<WorldObject>(CreateNote, GetNote, ReleaseNote, DestroyNote)
    // 参考 Music.cs
    private ConcurrentQueue<ChunkData> completedChunks = new ConcurrentQueue<ChunkData>();
    //private GameObject WaterGroup;
    private GameObject TerrainGroup;
    private GameObject GrassGroup;

    private int heightmapResolution = WorldConfig.chunkSize + 1;
    private int alphamapResolution = 64;
    //private int detailResolution = 512;

    private Vector3 lastRefreshPosition = Vector3.zero;
    private int calculatedChunkCount = int.MaxValue;
    private int instantiatedChunkCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("警告：重复的 TerrainLoader 实例！");
            return;
        }
        //WaterGroup = new GameObject("WaterGroup");
        TerrainGroup = new GameObject("TerrainGroup");
        GrassGroup = new GameObject("GrassGroup");
    }

    void Start()
    {
        Ocean.SetActive(false);
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        signalCenter.Subscribe(SignalType.EnterWorld, OnEnterWorld);
    }

    public void OnEnterWorld(GameObject sender, object data)
    {
        StartCoroutine(StartLoadCoroutine());
    }

    public IEnumerator StartLoadCoroutine()
    {
        Ocean.SetActive(true);
        Ocean.transform.position = new Vector3(0, WorldConfig.heightMultiplier * WorldConfig.waterLevelHeight, 0);
        World.Process("初始化噪声 Initialize Noise...");
        System.Random rng = new System.Random();
        TerrainNoiseSet = new List<NoiseSet>()
        {
            new NoiseSet(rng.Next(), 0.002f, 3, 2.0f, 0.5f), // plainsNoise
            new NoiseSet(rng.Next(), 0.001f, 6, 2.5f, 0.4f), // mountainNoise
            new NoiseSet(rng.Next(), 0.0015f, 4, 2.2f, 0.5f), // plateauNoise
            new NoiseSet(rng.Next(), 0.0025f, 5, 2.3f, 0.45f) // cliffNoise
        };

        BiomeNoiseSet = new List<NoiseSet>(TerrainNoiseSet.Count);
        for (int i = 0; i < TerrainNoiseSet.Count; i++)
        {
            //BiomeNoiseSet.Add(new NoiseSet(rng.Next(), 0.0005f, 2, 0.5f, 2.0f));
            BiomeNoiseSet.Add(new NoiseSet(rng.Next(), 0.005f, 2, 0.5f, 2.0f));
        }

        WarpNoise = new FastNoiseLite(rng.Next());
        WarpNoise.SetNoiseType(NoiseType.OpenSimplex2);
        WarpNoise.SetFrequency(0.0002f); // 极低频

        grassNoise = new FastNoiseLite(rng.Next());
        grassNoise.SetNoiseType(NoiseType.OpenSimplex2);
        grassNoise.SetFrequency(0.008f);

        yield return null;
        World.Process("刷新地形 RefreshTerrain...");
        StartCoroutine(InstantiationChunk());
        yield return StartCoroutine(RefreshTerrain(new Vector2Int(0, 0)));
    }

    private float[] GetBiomeWeights(float x, float z)
    {
        float[] raw = new float[BiomeNoiseSet.Count];
        float value, sum = 0f;
        int idx = 0;
        foreach (NoiseSet noiseSet in BiomeNoiseSet)
        {
            // Domain Warping 域扭曲
            float warpX = x + distortionLevel * WarpNoise.GetNoise(x, z);
            float warpZ = z + distortionLevel * WarpNoise.GetNoise(x + 5000, z + 5000);
            // Bias 丰度偏移
            value = Mathf.Exp(noiseSet.noise.GetNoise(warpX, warpZ) + noiseSet.bias);
            sum += value;
            raw[idx] = value;
            idx++;
        }
        for (int i = 0; i < BiomeNoiseSet.Count; i++)
        {
            raw[i] /= sum;
        }
        return raw;
    }

    private float GetHeight(float x, float z)
    {
        float[] weight = GetBiomeWeights(x, z);
        float result = 0f;
        for (int i = 0; i < TerrainNoiseSet.Count; i++)
        {
            result += TerrainNoiseSet[i].noise.GetNoise(x, z) * weight[i];
        }
        float normalized = (result + 1f) * 0.5f;
        return Mathf.Clamp01(normalized); // 归一化
    }

    private void CalculateChunkData(Vector2Int chunkPosition)
    {
        //Debug.Log($"Begin CalculateChunkData {chunkPosition.x} {chunkPosition.y}");
        int x = chunkPosition.x * WorldConfig.chunkSize;
        int z = chunkPosition.y * WorldConfig.chunkSize;
        float[,] height = new float[WorldConfig.chunkSize + 1, WorldConfig.chunkSize + 1];
        List<WorldObject> objects = new List<WorldObject>();
        float value;
        System.Random rng = new System.Random(chunkPosition.x ^ chunkPosition.y);
        List<GrassParticleData> grassList = new List<GrassParticleData>();
        DeterministicRandom drng = new DeterministicRandom(rng.Next(), chunkPosition.x, chunkPosition.y, chunkPosition.x ^ chunkPosition.y);
        for (int i = x, hi = 0; i <= x + WorldConfig.chunkSize; i++, hi++)
        {
            for (int j = z, hj = 0; j <= z + WorldConfig.chunkSize; j++, hj++)
            {
                value = GetHeight(i, j);
                height[hi, hj] = value;
                if (value > WorldConfig.waterLevelHeight)
                {
                    if (rng.NextDouble() < Mathf.Pow((grassNoise.GetNoise(i, j) + 1f) * 0.5f, 0.5f) && rng.NextDouble() < 0.1)
                    {
                        GrassParticleData grassParticleData = new GrassParticleData();
                        grassParticleData.position = new Vector4(i, value * WorldConfig.heightMultiplier, j, 0);
                        grassList.Add(grassParticleData);
                    }
                }
                // 注意 objects 花草等要用 ObjectPool 缓存！
            }
        }

        completedChunks.Enqueue(new ChunkData(chunkPosition, height, objects, CalculateSplatmap(chunkPosition), grassList.ToArray()));
    }

    private IEnumerator RemoveFarChunks(Vector2Int centerChunk)
    {
        List<Vector2Int> willRemove = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, ChunkData> kvp in chunks)
        {
            Debug.Log("尝试移除区块...");
            if (Vector2Int.Distance(kvp.Key, centerChunk) > Convert.ToInt32(Alpha.gameSettings.Categories["Graphics"]["ViewDistance"].CurrentValue))
            {
                if (kvp.Value.TerrainObject != null)
                {
                    Destroy(kvp.Value.TerrainObject);
                }
                foreach (WorldObject obj in kvp.Value.worldObjects)
                {
                    if (obj != null && WorldObjectPool.TryGetValue(obj.ID, out var pool))
                    {
                        pool.Release(obj);
                    }
                    else
                    {
                        Destroy(obj.gameObject);
                    }
                    Destroy(obj.gameObject);
                    if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
                    {
                        yield return null;
                        frameStartTime = Time.realtimeSinceStartup;
                    }
                }
                kvp.Value.worldObjects.Clear();
                willRemove.Add(kvp.Key);
            }
            if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
            {
                yield return null;
                frameStartTime = Time.realtimeSinceStartup;
            }
        }
        foreach (var key in willRemove)
        {
            chunks.Remove(key);
        }
        Debug.Log("所有区块移除完毕");
    }

    private IEnumerator CalculateAllData(Vector2Int centerChunk)
    {
        Debug.Log("CalculateAllData 开始");
        List<Task> tasks = new List<Task>();
        int viewDistance = Convert.ToInt32(Alpha.gameSettings.Categories["Graphics"]["ViewDistance"].CurrentValue);
        int kSquared = viewDistance * viewDistance;
        calculatedChunkCount = 0;
        instantiatedChunkCount = 0;
        for (int i = -viewDistance; i <= viewDistance; i++)
        {
            int maxJ = (int)Math.Sqrt(kSquared - i * i);
            for (int j = -maxJ; j <= maxJ; j++)
            {
                int chunkX = i + centerChunk.x; // 注意避免闭包问题
                int chunkZ = j + centerChunk.y;
                Vector2Int coord = new Vector2Int(chunkX, chunkZ);
                if (!chunks.ContainsKey(coord) || chunks[coord].TerrainObject == null)
                {
                    calculatedChunkCount++;
                    Task task = Task.Run(() => CalculateChunkData(new Vector2Int(chunkX, chunkZ)));
                    tasks.Add(task);
                }
            }
        }
        Debug.Log("CalculateAllData 等待。。。");
        while (tasks.Any(t => !t.IsCompleted))
        {
            yield return null;
        }
        Debug.Log("CalculateAllData 结束");
    }

    //private float[,,] CalculateSplatmap(Vector2Int chunkPosition)
    //{
    //    int alphamapWidth = alphamapResolution;
    //    int alphamapHeight = alphamapResolution;
    //    float[,,] splatmap = new float[alphamapWidth, alphamapHeight, 4];

    //    float chunkSize = WorldConfig.chunkSize;
    //    float minX = chunkPosition.x * chunkSize;
    //    float minZ = chunkPosition.y * chunkSize;

    //    for (int py = 0; py < alphamapHeight; py++)
    //    {
    //        for (int px = 0; px < alphamapWidth; px++)
    //        {
    //            // 计算当前 splatmap 像素对应的世界坐标
    //            float normX = px / (float)alphamapWidth;
    //            float normZ = py / (float)alphamapHeight;
    //            float worldX = minX + normX * chunkSize;
    //            float worldZ = minZ + normZ * chunkSize;

    //            // 获取该点的生物群系权重
    //            float[] weights = GetBiomeWeights(worldX, worldZ);
    //            // 获取高度，判断是否在水下
    //            //float height = GetHeight(worldX, worldZ);
    //            //if (height < WorldConfig.waterLevelHeight)
    //            //{
    //            //    // 水下强制使用石头贴图（假设石头对应索引 1，即 mountainNoise）
    //            //    for (int i = 0; i < 4; i++) weights[i] = 0f;
    //            //    weights[3] = 1f;
    //            //}

    //            // 写入 splatmap
    //            for (int i = 0; i < 4; i++)
    //            {
    //                splatmap[px, py, i] = weights[i];
    //            }
    //        }
    //    }
    //    return splatmap;
    //}

    private float[,,] CalculateSplatmap(Vector2Int chunkPosition)
    {
        float[,,] splatmap = new float[alphamapResolution, alphamapResolution, 1];
        for (int py = 0; py < alphamapResolution; py++)
        {
            for (int px = 0; px < alphamapResolution; px++)
            {
                splatmap[px, py, 0] = 1f;
            }
        }
        return splatmap;
    }

    private GameObject CreateChunkGameObject(ChunkData chunk)
    {
        // Generate a Mesh-based terrain chunk from the height[,] data
        int sizeX = WorldConfig.chunkSize + 1;
        int sizeZ = WorldConfig.chunkSize + 1;

        // Create GameObject for this chunk
        GameObject obj = new GameObject($"TerrainChunk_{chunk.ChunkPosition.x}_{chunk.ChunkPosition.y}");
        obj.transform.parent = TerrainGroup.transform;
        obj.transform.position = new Vector3(chunk.ChunkPosition.x * WorldConfig.xSpacing * WorldConfig.chunkSize, 0,
            chunk.ChunkPosition.y * WorldConfig.zSpacing * WorldConfig.chunkSize);

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        MeshCollider mc = obj.AddComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[sizeX * sizeZ];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[(sizeX - 1) * (sizeZ - 1) * 6];

        for (int z = 0; z < sizeZ; z++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                int idx = z * sizeX + x;
                float heightValue = chunk.height[x, z];
                float vx = x * WorldConfig.xSpacing;
                float vz = z * WorldConfig.zSpacing;
                float vy = heightValue * WorldConfig.heightMultiplier;
                vertices[idx] = new Vector3(vx, vy, vz);
                uvs[idx] = new Vector2(x / (float)(sizeX - 1), z / (float)(sizeZ - 1));
            }
        }

        int t = 0;
        for (int z = 0; z < sizeZ - 1; z++)
        {
            for (int x = 0; x < sizeX - 1; x++)
            {
                int i0 = z * sizeX + x;
                int i1 = i0 + 1;
                int i2 = i0 + sizeX;
                int i3 = i2 + 1;

                triangles[t++] = i0;
                triangles[t++] = i2;
                triangles[t++] = i1;

                triangles[t++] = i1;
                triangles[t++] = i2;
                triangles[t++] = i3;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;

        Material mat = TerrainMaterial;
        if (albedoMaps != null && albedoMaps.Length > 0 && albedoMaps[0] != null)
        {
            mat.mainTexture = albedoMaps[0];
        }
        mr.sharedMaterial = mat;

        return obj;
    }

    // openedColliderTerrains was used with TerrainData; removed because terrain replaced with Mesh-based chunks

    private IEnumerator InstantiationChunk()
    {
        while (true)
        {
            frameStartTime = Time.realtimeSinceStartup;
            while (completedChunks.TryDequeue(out ChunkData data))
            {
                instantiatedChunkCount++;
                World.Process($"实例化区块 Instantiation Chunk：{instantiatedChunkCount} / {calculatedChunkCount}", instantiatedChunkCount);

                chunks[data.ChunkPosition] = data;

                // 防御性检查：确保 data.items 不为 null
                if (data.worldObjects == null)
                {
                    Debug.LogError($"data.items is null for chunk ({data.ChunkPosition.x},{data.ChunkPosition.y})");
                    continue;
                }

                Vector2Int key = new Vector2Int(data.ChunkPosition.x, data.ChunkPosition.y);
                if (!chunks.ContainsKey(key))
                {
                    chunks[key] = new ChunkData();
                }
                ChunkData chunk = chunks[key];
                chunk.TerrainObject = CreateChunkGameObject(data);
                chunks[key] = chunk;

                if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
                {
                    yield return null;
                    frameStartTime = Time.realtimeSinceStartup;
                }

                GameObject grassObj = Instantiate(GrassPrefab, new Vector3(data.ChunkPosition.x * WorldConfig.chunkSize, 0, data.ChunkPosition.y * WorldConfig.chunkSize), Quaternion.identity);
                grassObj.name = $"Grass_{data.ChunkPosition.x}_{data.ChunkPosition.y}";
                grassObj.transform.parent = GrassGroup.transform;
                grassObj.GetComponent<GrassVFXDataBinder>().InitializeGrassData(data.grassData);
                chunks[key].worldObjects.Add(new WorldObject(1, default, default, grassObj));

                // 物品生成
                //foreach (var u in data.items)
                //{
                //    DeterministicRandom rng = new DeterministicRandom(mapSeed, (int)u.x, (int)u.z, (int)u.y);
                //    //System.Random rng = new System.Random(mapSeed + (int)(u.x) * 2 + (int)(u.z) * 3 + (int)(u.y) * 5);
                //    GameObject gcItem = null;
                //    double val = rng.NextDouble();
                //    if (u.y >= -0.5f && u.y <= 0.8f)
                //    {
                //        if (val < 0.1)
                //        {
                //            val = rng.NextDouble();
                //            if (val < 0.3) gcItem = CemeteryPebbles1Perfab;
                //            else if (val < 0.6) gcItem = CemeteryPebbles2Perfab;
                //            else if (val < 0.62) gcItem = RockMossGrown1;
                //            else if (val < 0.64) gcItem = RockMossGrown2;
                //            else if (val < 0.65) gcItem = RockPileForestMoss1;
                //            else if (val < 0.66) gcItem = RockPileForestMoss2;
                //        }
                //        else
                //        {
                //            val = rng.NextDouble();
                //            if (val < 0.15) gcItem = Birch1Perfab;
                //            else if (val < 0.3) gcItem = Birch2Perfab;
                //            else if (val < 0.45) gcItem = Tree1Perfab;
                //            else if (val < 0.6) gcItem = Tree2Perfab;
                //            else if (val < 0.68) gcItem = Daffodil;
                //            else if (val < 0.76) gcItem = Grass1;
                //            else if (val < 0.84) gcItem = Grass2;
                //            else if (val < 0.87) gcItem = Hyacinth;
                //            else if (val < 0.9) gcItem = MushroomFantasyOrange1;
                //            else if (val < 0.92) gcItem = MushroomFantasyOrange2;
                //            else if (val < 0.94) gcItem = MushroomFantasyPurple1;
                //            else if (val < 0.96) gcItem = MushroomFantasyPurple2;
                //            else if (val <= 1.0) gcItem = Sunflower;
                //        }
                //    }
                //    else
                //    {
                //        if (val < 0.2) gcItem = CemeteryPebbles1Perfab;
                //        else if (val < 0.4) gcItem = CemeteryPebbles2Perfab;
                //        else if (val < 0.42) gcItem = RockMossGrown1;
                //        else if (val < 0.44) gcItem = RockMossGrown2;
                //        else if (val < 0.45) gcItem = RockPileForestMoss1;
                //        else if (val < 0.46) gcItem = RockPileForestMoss2;
                //    }
                //    if (u.y < 0.8f && u.y > 0f && rng.NextDouble() < 0.005)
                //    {
                //        gcItem = Stele;
                //    }

                //    var v = new Vector3(u.x, u.y * WorldConfig.heightMultiplier - 0.5f, u.z);
                //    if (gcItem != null)
                //    {
                //        IObjectPool<GameObject> pool = GetPool(gcItem);
                //        GameObject obj = pool.Get();
                //        obj.transform.SetPositionAndRotation(v, Quaternion.identity);
                //        itemToPool[obj] = pool;
                //        //obj.transform.scale = obj.transform.localScale * MapFloat((float)rng.NextDouble(), -1f, 1f, 0.8f, 1.3f);

                //        obj.transform.localScale = gcItem.transform.localScale * MapFloat((float)rng.NextDouble(), -1f, 1f, 0.8f, 1.3f);
                //        Vector2Int chunkCoord = new Vector2Int(data.chunkX, data.chunkZ);
                //        if (!chunks.ContainsKey(chunkCoord))
                //            chunks[chunkCoord] = new ChunkData();
                //        obj.transform.SetParent(TerrainItemGroup.transform);
                //        chunks[chunkCoord].items.Add(obj);
                //    }

                //    if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
                //    {
                //        yield return null;
                //        frameStartTime = Time.realtimeSinceStartup;
                //    }
                //}

                if (Time.realtimeSinceStartup - frameStartTime > WorldConfig.loadSPF)
                {
                    yield return null;
                    frameStartTime = Time.realtimeSinceStartup;
                }
            }
            if (GameStateManager.Get() == GameState.LoadBeginning)
            {
                if (calculatedChunkCount == instantiatedChunkCount)
                {
                    GameStateManager.Set(GameState.LoadingCompleted);
                    signalCenter.Emit(SignalType.WorldLoaded, gameObject);
                }
            }
            yield return null;
        }
    }

    private Coroutine refreshCoroutine;
    private IEnumerator RefreshTerrain(Vector2Int centerChunk)
    {
        frameStartTime = Time.realtimeSinceStartup;
        World.Process("移除远处区块 Remove Far Chunks...");
        yield return StartCoroutine(RemoveFarChunks(centerChunk));
        World.Process("计算数据与实例化世界 Calculate Data and Instantiation World...");
        yield return StartCoroutine(CalculateAllData(centerChunk));
        World.Process("实例化世界 Instantiation World...");
        while (calculatedChunkCount < instantiatedChunkCount)
        {
            yield return null;
        }
        World.Process("刷新完成 Refresh Complete.");
        refreshCoroutine = null;
    }

    void Update()
    {
        if (GameStateManager.Get() == GameState.LoadingCompleted)
        {
            if (Vector3.Distance(lastRefreshPosition, player.transform.position) > WorldConfig.chunkSize)
            {
                lastRefreshPosition = player.transform.position;
                Vector2Int center = new Vector2Int(Mathf.FloorToInt(player.transform.position.x / (WorldConfig.xSpacing * WorldConfig.chunkSize)),
                                                    Mathf.FloorToInt(player.transform.position.z / (WorldConfig.zSpacing * WorldConfig.chunkSize)));
                if (refreshCoroutine == null)
                {
                    refreshCoroutine = StartCoroutine(RefreshTerrain(center));
                }
                else
                {
                    Debug.Log("refreshCoroutine is running.");
                }
            }
        }
    }
}
*/