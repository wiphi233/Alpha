using DG.Tweening;
using NaughtyAttributes;
using NCalc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.Pool;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;


public static class FormulaCompiler
{
    // ✅ 使用稳定哈希码作为Key，避免Expression对象作为Key产生GC
    private static Dictionary<string, Expression> _compiledCache = new(100);
    private static Dictionary<string, float> _constantCache = new(50);
    private static Dictionary<int, int> _lastTValue = new(100);
    private static Dictionary<int, float> _cachedResult = new(100);

    // ✅ 对象池：复用Expression评估结果
    private static Stack<Dictionary<string, object>> _parameterPool = new(50);

    // ✅ 避免字符串重复创建
    private static readonly string TParameterName = "t";

    // ✅ 线程安全锁（如果有多线程访问）
    private static readonly object _lock = new object();

    public static float Evaluate(string formula, int t)
    {
        // 快速路径1：空公式
        if (string.IsNullOrEmpty(formula)) return 0f;

        // 快速路径2：常量缓存
        if (_constantCache.TryGetValue(formula, out float constantValue))
            return constantValue;

        // 获取或编译表达式
        if (!_compiledCache.TryGetValue(formula, out var expr))
        {
            expr = CompileFormula(formula);
            _compiledCache[formula] = expr;
        }

        // 获取表达式的稳定哈希码（缓存Key）
        int exprHash = expr.GetHashCode();

        // 检查缓存
        if (_lastTValue.TryGetValue(exprHash, out int lastT) && lastT == t)
        {
            return _cachedResult[exprHash];
        }

        // ✅ 优化：避免Convert.ToSingle的装箱
        float result = EvaluateExpressionFast(expr, t);

        // 更新缓存
        _lastTValue[exprHash] = t;
        _cachedResult[exprHash] = result;

        return result;
    }

    /// <summary>
    /// ✅ 快速评估，零GC分配
    /// </summary>
    private static float EvaluateExpressionFast(Expression expr, int t)
    {
        // 复用参数字典（对象池模式）
        if (!_parameterPool.TryPop(out var parameters))
        {
            parameters = new Dictionary<string, object>(1);
        }
        else
        {
            parameters.Clear();
        }

        // ✅ 避免装箱：int直接存储，后续使用时再转换
        parameters[TParameterName] = t;

        // 设置参数并评估
        expr.Parameters = parameters;

        // ✅ 直接获取原始值并转换，避免Convert.ToSingle的object分配
        object rawResult = expr.Evaluate();
        //object rawResult = expr.Evaluate(null);

        float result;
        if (rawResult is int intVal)
        {
            result = intVal;  // int→float隐式转换，无装箱
        }
        else if (rawResult is double doubleVal)
        {
            result = (float)doubleVal;
        }
        else if (rawResult is float floatVal)
        {
            result = floatVal;
        }
        else if (rawResult is decimal decimalVal)
        {
            result = (float)decimalVal;
        }
        else
        {
            // 最后的fallback，但应该不会走到这里
            result = Convert.ToSingle(rawResult);
        }

        // 归还参数字典到对象池
        expr.Parameters = null;
        _parameterPool.Push(parameters);

        return result;
    }

    public static void PreCompile(string formula)
    {
        if (string.IsNullOrEmpty(formula)) return;

        lock (_lock)
        {
            if (_compiledCache.ContainsKey(formula) || _constantCache.ContainsKey(formula))
                return;
        }

        // 检查是否为纯数字常量
        if (float.TryParse(formula, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float constVal))
        {
            lock (_lock)
            {
                _constantCache[formula] = constVal;
            }
            return;
        }

        // 编译表达式
        var expr = CompileFormula(formula);
        int exprHash = expr.GetHashCode();

        lock (_lock)
        {
            _compiledCache[formula] = expr;
            _lastTValue[exprHash] = -1;
            _cachedResult[exprHash] = 0;
        }
    }

    public static void PreCompileAll(IEnumerable<string> formulas)
    {
        foreach (var formula in formulas)
        {
            PreCompile(formula);
        }
    }

    private static Expression CompileFormula(string formula)
    {
        try
        {
            var expr = new Expression(formula, EvaluateOptions.IgnoreCase);
            // ✅ 预创建参数字典，避免后续频繁创建
            expr.Parameters = new Dictionary<string, object>(1) { [TParameterName] = 0 };
            return expr;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"公式编译失败: '{formula}', 错误: {ex.Message}");
            var fallbackExpr = new Expression("0");
            fallbackExpr.Parameters = new Dictionary<string, object>(1) { [TParameterName] = 0 };
            return fallbackExpr;
        }
    }

    public static void ClearCache()
    {
        lock (_lock)
        {
            _compiledCache.Clear();
            _constantCache.Clear();
            _lastTValue.Clear();
            _cachedResult.Clear();
            _parameterPool.Clear();
        }
    }

    /// <summary>
    /// ✅ 预热对象池（在游戏启动时调用）
    /// </summary>
    public static void WarmupPool(int poolSize = 50)
    {
        for (int i = 0; i < poolSize; i++)
        {
            _parameterPool.Push(new Dictionary<string, object>(1));
        }
    }

    public static string GetCacheStats()
    {
        return $"Compiled: {_compiledCache.Count}, Constants: {_constantCache.Count}, " +
               $"Cached Results: {_cachedResult.Count}, Pool Size: {_parameterPool.Count}";
    }
}

public class Music : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_8 = new WaitForSeconds(0.8f);
    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
    [Header("LoadCanvas UI 元素")]
    public GameObject LoadCanvasObject;
    public GameObject LoadCanvas_Illustration;
    public GameObject LoadCanvas_SongNameObject;
    public GameObject LoadCanvas_MusicAuthorObject;
    public GameObject LoadCanvas_PavementAuthorObject;
    public GameObject LoadCanvas_IllustrationAuthorObject;
    public GameObject LoadCanvas_TimeObject;
    public GameObject LoadCanvas_LoadingObject;

    [Header("MusicCanvas UI 元素")]
    public GameObject MusicCanvasObject;
    public GameObject MusicCanvas_ScoreObject;
    public GameObject MusicCanvas_ComboObject;
    public GameObject MusicCanvas_TypeObject;
    public GameObject MusicCanvas_InfoObject;

    [Header("ResultCanvas UI 元素")]
    public GameObject ResultCanvasObject;
    public GameObject ResultCanvas_Illustration;
    public GameObject ResultCanvas_MusicAuthor;
    public GameObject ResultCanvas_IllustrationAuthor;
    public GameObject ResultCanvas_PavementAuthor;
    public GameObject ResultCanvas_SongName;
    public GameObject ResultCanvas_Score;
    public GameObject ResultCanvas_Best;
    public GameObject ResultCanvas_PlayerName;
    public GameObject ResultCanvas_PlayerAvatar;
    public GameObject ResultCanvas_Back;
    public GameObject ResultCanvas_Perfect;
    public GameObject ResultCanvas_Good;
    public GameObject ResultCanvas_Bad;
    public GameObject ResultCanvas_Miss;
    public GameObject ResultCanvas_Early;
    public GameObject ResultCanvas_Late;
    public GameObject ResultCanvas_MaxCombo;

    [Header("Note and Judge 音符与判定")]
    [ShowAssetPreview] public Material JudgeMaterial;
    public GameObject NotePrefab;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    public GameObject dataManagerObject;
    private ISignalCenter signalCenter;
    private IDataManager dataManager;

    private TextMeshProUGUI ScoreObjectTMP;
    private TextMeshProUGUI ComboObjectTMP;
    private TextMeshProUGUI TypeObjectTMP;
    private TextMeshProUGUI InfoObjectTMP;

    private float musicLength = 0f;

    private int judgmentIndex = 0; // 当前正在处理的 JudgmentData 索引
    [HideInInspector]
    public float startTime = 0f;
    private Queue<JudgmentData> activeJudgments = new Queue<JudgmentData>();

    // 生成圆柱 Mesh
    private const float radius = 0.2f;   // 半径
    private const float height = 1000f;  // 高度
    private const int segments = 20;     // 圆周分段数，越大越平滑

    [HideInInspector]
    public static Music Instance;
    [HideInInspector]
    public MusicData musicData;
    [HideInInspector]
    public int perfectNumber;
    [HideInInspector]
    public int goodNumber;
    [HideInInspector]
    public int badNumber;
    [HideInInspector]
    public int missNumber;
    [HideInInspector]
    public int maxComboNumber;
    [HideInInspector]
    public int nowComboNumber;
    [HideInInspector]
    public int noteCountNumber;
    [HideInInspector]
    public int elapsedMs = 0;
    [HideInInspector]
    public int earlyNumber;
    [HideInInspector]
    public int lateNumber;

    private Mesh sharedCylinderMesh;
    [HideInInspector]
    public ObjectPool<GameObject> notesPool;

    private int noteIndex;
    private Sprite illustrationSprite;
    private GCLatencyMode originalGCMode;

    [HideInInspector] public Queue<GameObject> notesQueue = new Queue<GameObject>();

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        dataManager = dataManagerObject.GetComponent<IDataManager>();
        ScoreObjectTMP = MusicCanvas_ScoreObject.GetComponent<TextMeshProUGUI>();
        ComboObjectTMP = MusicCanvas_ComboObject.GetComponent<TextMeshProUGUI>();
        TypeObjectTMP = MusicCanvas_TypeObject.GetComponent<TextMeshProUGUI>();
        InfoObjectTMP = MusicCanvas_InfoObject.GetComponent<TextMeshProUGUI>();
        sharedCylinderMesh = GenerateCylinder();
        notesPool = new ObjectPool<GameObject>(CreateNote, GetNote, ReleaseNote, DestroyNote);
        signalCenter.Subscribe(SignalType.LoadMusic, OnLoadMusic);
    }

    GameObject CreateNote()
    {
        var obj = Instantiate(NotePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        if (gameObject.GetComponent<NoteController>() == null)
        {
            obj.AddComponent<NoteController>();
        }
        return obj;
    }

    void GetNote(GameObject note)
    {
        note.SetActive(true);
        var noteController = note.GetComponent<NoteController>();
        note.transform.position = new Vector3(0, 10000, 0);
        noteController.rendererComponent = note.GetComponent<Renderer>();
        Color color = noteController.rendererComponent.material.color;
        color.a = 1f;
        noteController.rendererComponent.material.color = color;

        //if (gameObject.GetComponent<NoteController>() == null)
        //{
        //    gameObject.AddComponent<NoteController>();
        //}

        //NoteController existing = note.GetComponent<NoteController>();
        //if (existing != null)
        //{
        //    DestroyImmediate(existing); // 立即删除，而不是在帧结束后删除
        //}

        //NoteController noteController = note.AddComponent<NoteController>();
        //noteController.judgmentIndex = judgmentIndex;
        //noteController.noteIndex = idx;
    }

    void ReleaseNote(GameObject note)
    {
        note.SetActive(false);
    }

    void DestroyNote(GameObject note)
    {
        Destroy(note);
    }

    void OnLoadMusic(GameObject sender, object data)
    {
        Debug.Log("OnLoadMusic");
        // 现在要做一个：
        /* 瞬间屏幕背景替换为毛玻璃的曲绘
         * 曲绘从右向左淡入，（把曲名、作曲、谱师之类的也放一起
         * 用 JSON 文件存储铺面（最后或许需要二进制压缩一下？）
         */
        //RectTransform rect = LoadCanvas_Illustration.GetComponent<RectTransform>();
        //rect.anchorMin = Vector2.zero;
        //rect.anchorMax = Vector2.one;
        //rect.sizeDelta = Vector2.zero;  // 填满父物体
        //string jsonData = ReadData();
        //Debug.Log(jsonData);

        string songName = "PRAGMATISMRESURRECTION.Laur";
        string fileName = $"Music/{songName}/{songName}";
        musicData = null;
        // 使用异步加载
        dataManager.LoadData<MusicData>(fileName, (loadedData) =>
        {
            if (loadedData != null)
            {
                musicData = loadedData;
                Debug.Log($"曲目数据加载成功：{fileName}\nname: {musicData.name}\nmusicAuthor: {musicData.musicAuthor}\npavementAuthor: {musicData.pavementAuthor}\nillustration: {musicData.illustration}\nillustrationAuthor: {musicData.illustrationAuthor}\naudio: {musicData.audio}");
                if (AudioManager.bgmSource.isPlaying)
                {
                    AudioManager.bgmSource.Pause();
                }
                StartCoroutine(LoadImageCoroutine("Music/" + songName + "/" + musicData.illustration));
                StartCoroutine(LoadAudio("Music/" + songName + "/" + musicData.audio));
            }
            else
            {
                Debug.LogError($"曲目数据加载失败：{fileName}");
            }
        }, DataFormat.JSON);
    }

    IEnumerator LoadAudio(string path)
    {
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, path);
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fullPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"音频加载失败: {www.error}\n路径: {fullPath}");
                yield break;
            }
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            if (clip != null)
            {
                clip.LoadAudioData();
                TimeSpan audioDuration = TimeSpan.FromSeconds(clip.length);
                musicLength = clip.length;
                LoadCanvas_TimeObject.GetComponent<TextMeshProUGUI>().SetText("时长：" + audioDuration.ToString(@"mm\:ss"));
                AudioManager.songSource.clip = clip;
                Debug.Log("音频加载成功！");
            }
        }
    }

    IEnumerator LoadImageCoroutine(string fileName)
    {
        Debug.Log("LoadImageCoroutine: " + fileName);
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);

        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullPath))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);

                // 将 Texture2D 转换为 Sprite
                illustrationSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                Image illustrationImage = LoadCanvas_Illustration.GetComponent<Image>();
                illustrationImage.sprite = illustrationSprite;
                RectTransform rectTransform = LoadCanvas_Illustration.GetComponent<RectTransform>();

                // ----- 弹入动画 -----
                float screenWidth = rectTransform.parent.GetComponent<Canvas>().GetComponent<RectTransform>().rect.width;
                Vector2 originalAnchoredPosition = rectTransform.anchoredPosition;
                rectTransform.anchoredPosition = new Vector2(screenWidth + rectTransform.rect.width, originalAnchoredPosition.y);

                float startX = rectTransform.anchoredPosition.x;
                float endX = originalAnchoredPosition.x;
                float duration = 1.0f;
                DOVirtual.Float(startX, endX, duration, (float x) =>
                {
                    Vector2 pos = rectTransform.anchoredPosition;
                    pos.x = x;
                    rectTransform.anchoredPosition = pos;
                }).SetEase(Ease.OutBack);

                Debug.Log($"成功加载图片: {fileName}");
            }
            else
            {
                Debug.LogError($"图片加载失败: {request.error}");
            }
        }
        LoadCanvas_Illustration.SetActive(true);
        LoadCanvas_SongNameObject.GetComponent<TextMeshProUGUI>().SetText(musicData.name);
        LoadCanvas_MusicAuthorObject.GetComponent<TextMeshProUGUI>().SetText("曲师：" + musicData.musicAuthor);
        LoadCanvas_PavementAuthorObject.GetComponent<TextMeshProUGUI>().SetText("谱师：" + musicData.pavementAuthor);
        LoadCanvas_IllustrationAuthorObject.GetComponent<TextMeshProUGUI>().SetText("画师：" + musicData.illustrationAuthor);
        StartCoroutine(LoadingAnimationCoroutine());
        GC.Collect();
    }

    IEnumerator LoadingAnimationCoroutine()
    {
        TextMeshProUGUI loadingText = LoadCanvas_LoadingObject.GetComponent<TextMeshProUGUI>();
        yield return _waitForSeconds0_8;
        for (int i = 1; i <= 2; i++)
        {
            loadingText.SetText("Loading.");
            yield return _waitForSeconds0_5;
            loadingText.SetText("Loading..");
            yield return _waitForSeconds0_5;
            loadingText.SetText("Loading...");
            yield return _waitForSeconds0_5;
        }
        if (!LoadCanvas_Illustration.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup = LoadCanvas_Illustration.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1;
        float duration = 0.5f;
        DOVirtual.Float(1, 0, duration, (float val) =>
        {
            canvasGroup.alpha = val;
            if (val == 0)
            {
                canvasGroup.alpha = 1;
                LoadCanvas_Illustration.SetActive(false);
            }
        }).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(duration);
        

        // ----- Load Pavement -----
        judgmentIndex = 0;
        musicData.pavement.judgment.Sort((a, b) => a.start.CompareTo(b.start));
        activeJudgments.Clear();
        perfectNumber = goodNumber = badNumber = missNumber = maxComboNumber = nowComboNumber = 0;
        noteIndex = earlyNumber = lateNumber = 0;

        // 一次性预编译所有公式
        var allFormulas = CollectAllFormulas(musicData);
        Debug.Log($"开始预编译 {allFormulas.Count} 个公式...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        FormulaCompiler.PreCompileAll(allFormulas);
        stopwatch.Stop();
        Debug.Log($"预编译完成，耗时: {stopwatch.ElapsedMilliseconds}ms");

        // 计算物量
        noteCountNumber = 0;
        foreach (JudgmentData judgment in musicData.pavement.judgment)
        {
            noteCountNumber += judgment.notes.Count;
        }

        // 预热对象池
        var pooledObjects = new List<GameObject>();
        int poolSize = Math.Max(noteCountNumber / 20, 30);
        for (int i = 0; i < poolSize; i++)
        {
            //var obj = Instantiate(NotePrefab);
            //obj.SetActive(false);
            //pooledObjects.Add(obj);
            var note = notesPool.Get();
            var noteController = note.GetComponent<NoteController>();
            noteController.x = null;
        }
        foreach (var obj in pooledObjects)
        {
            notesPool.Release(obj);
        }

        FormulaCompiler.WarmupPool(poolSize);

        // 低延迟模式，减少GC导致的卡顿
        originalGCMode = GCSettings.LatencyMode;
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

        // 初始化 Music Canvas
        MusicCanvasObject.SetActive(true);
        ScoreObjectTMP.SetText("00000");
        ComboObjectTMP.SetText("0");
        TypeObjectTMP.SetText("Combo");
        InfoObjectTMP.SetText($"完美 Perfect: {perfectNumber}\n很好 Good: {goodNumber}\n过早 Bad: {badNumber}\n漏键 Miss: {missNumber}\n最大连击 Max Combo: {maxComboNumber}\n物量 Note Count: {noteCountNumber}");
        
        AudioManager.songSource.Play();
        startTime = Time.time;
        notesQueue = new Queue<GameObject>();
    }

    void PlayEnd()
    {
        startTime = 0f;
        elapsedMs = 0;
        MusicCanvasObject.SetActive(false);
        AudioManager.songSource.Stop();
        AudioManager.songSource.clip.UnloadAudioData();
        AudioManager.songSource.clip = null;
        notesPool.Clear();
        while (activeJudgments.Count != 0)
        {
            Destroy(activeJudgments.Peek().judgmentObject);
            activeJudgments.Dequeue();
        }
        FormulaCompiler.ClearCache();

        ResultCanvas_SongName.GetComponent<TextMeshProUGUI>().SetText(musicData.name);
        ResultCanvas_Illustration.GetComponent<Image>().sprite = illustrationSprite;
        ResultCanvas_MusicAuthor.GetComponent<TextMeshProUGUI>().SetText("曲师：" + musicData.musicAuthor);
        ResultCanvas_PavementAuthor.GetComponent<TextMeshProUGUI>().SetText("谱师：" + musicData.pavementAuthor);
        ResultCanvas_IllustrationAuthor.GetComponent<TextMeshProUGUI>().SetText("画师：" + musicData.illustrationAuthor);
        ResultCanvas_Score.GetComponent<TextMeshProUGUI>().SetText("<gradient=\"PhoenixStart\">" + ScoreObjectTMP.text + "</gradient>");
        ResultCanvas_Best.SetActive(false);
        ResultCanvas_PlayerName.GetComponent<TextMeshProUGUI>().SetText("WiPhi233_awa");
        // Boot.cs (引导) 里面写用户登录（输入用户名）、初始化、选头像、同意EULA、未成年人防沉迷验证
        //ResultCanvas_PlayerAvater.GetComponent<Image>().sprite = playerAvatarSprite;
        ResultCanvas_Perfect.GetComponent<TextMeshProUGUI>().SetText(perfectNumber.ToString());
        ResultCanvas_Good.GetComponent<TextMeshProUGUI>().SetText(goodNumber.ToString());
        ResultCanvas_Bad.GetComponent<TextMeshProUGUI>().SetText(badNumber.ToString());
        ResultCanvas_Miss.GetComponent<TextMeshProUGUI>().SetText(missNumber.ToString());
        ResultCanvas_Early.GetComponent<TextMeshProUGUI>().SetText(earlyNumber.ToString());
        ResultCanvas_Late.GetComponent<TextMeshProUGUI>().SetText(lateNumber.ToString());
        ResultCanvas_MaxCombo.GetComponent<TextMeshProUGUI>().SetText(maxComboNumber.ToString());
        //ResultCanvas_Back 做动画
        var BackButtonRectTransform = ResultCanvas_Back.GetComponent<RectTransform>();
        DOVirtual.Float(-600f, 60f, 2f, (float x) =>
        {
            Vector2 anchoredPos = BackButtonRectTransform.anchoredPosition;
            anchoredPos.x = x;
            BackButtonRectTransform.anchoredPosition = anchoredPos;
        }).SetEase(Ease.OutBack);
        //var BackButtonRectTransform = ResultCanvas_Back.GetComponent<RectTransform>();
        //var BackButtonPosition = BackButtonRectTransform.position;
        //BackButtonPosition.y = -400f;
        //DOVirtual.Float(-600f, 60f, 2f, (float x) =>
        //{
        //    BackButtonPosition.x = x;
        //    BackButtonRectTransform.position = BackButtonPosition;
        //}).SetEase(Ease.OutBack);

        ResultCanvasObject.SetActive(true);

        // 恢复 GC 并手动调用
        GCSettings.LatencyMode = originalGCMode;
        GC.Collect();
    }

    public void OnResultCanvas_BackClicked()
    {
        ResultCanvasObject.SetActive(false);
        //signalCenter.Emit(SignalType.ChangeAngularBall, gameObject, AngularBallState.Active);
        signalCenter.Emit(SignalType.BootCompletion, gameObject);
    }

    void Update()
    {
        //float lastTime = Time.time;
        if (startTime != 0f)
        {
            if (musicLength + startTime <= Time.time)
            {
                PlayEnd();
            }
            elapsedMs = (int)((Time.time - startTime) * 1000);
            while (judgmentIndex < musicData.pavement.judgment.Count)
            {
                JudgmentData jd = musicData.pavement.judgment[judgmentIndex];
                if (elapsedMs >= jd.start)
                {
                    jd.judgmentObject = new GameObject("Judgment_" + judgmentIndex);
                    MeshFilter jdmf = jd.judgmentObject.AddComponent<MeshFilter>();
                    MeshRenderer jdmr = jd.judgmentObject.AddComponent<MeshRenderer>();
                    jdmr.SetMaterials(new List<Material>() { JudgeMaterial });
                    jdmf.mesh = sharedCylinderMesh;
                    activeJudgments.Enqueue(jd);
                    judgmentIndex++;
                }
                else
                {
                    break;
                }
            }
            for (int i = 0; i < musicData.pavement.judgment.Count; i++)
            {
                JudgmentData jd = musicData.pavement.judgment[i];
                if (jd.judgmentObject == null)
                {
                    continue;
                }
                NoteData noteData;
                for (; noteIndex < jd.notes.Count; noteIndex++)
                {
                    noteData = jd.notes[noteIndex];
                    if (noteData.time - elapsedMs > 5000)
                    {
                        break;
                    }
                    var note = notesPool.Get();
                    var noteController = note.GetComponent<NoteController>();
                    noteController.time = noteData.time;
                    noteController.distance = noteData.distance;
                    noteController.rotation = noteData.rotation;
                    noteController.x = noteData.x;
                    noteController.type = noteData.type;
                    noteController.length = noteData.length;
                    noteController.judgmentObject = jd.judgmentObject;
                    notesQueue.Enqueue(note);
                }
            }
            while (activeJudgments.Count != 0)
            {
                JudgmentData jd = activeJudgments.Peek();
                if (jd.end != -1 && jd.end <= elapsedMs)
                {
                    Destroy(jd.judgmentObject);
                    activeJudgments.Dequeue();
                }
                else
                {
                    break;
                }
            }
            var enumerator = activeJudgments.GetEnumerator();
            while (enumerator.MoveNext())
            {
                // 注意！函数名大小写敏感，比如 Abs() 而不是 abs()
                JudgmentData jd = enumerator.Current;
                jd.judgmentObject.transform.SetPositionAndRotation(
                    new Vector3(
                        FormulaCompiler.Evaluate(jd.position.x, elapsedMs),
                        FormulaCompiler.Evaluate(jd.position.y, elapsedMs),
                        FormulaCompiler.Evaluate(jd.position.z, elapsedMs)
                    ),
                    Quaternion.Euler(
                        FormulaCompiler.Evaluate(jd.rotation.x, elapsedMs),
                        FormulaCompiler.Evaluate(jd.rotation.y, elapsedMs),
                        FormulaCompiler.Evaluate(jd.rotation.z, elapsedMs)
                    )
                );
            }
            maxComboNumber = Math.Max(maxComboNumber, nowComboNumber);
            decimal accuracy = ((decimal)perfectNumber + (decimal)goodNumber * (decimal)NoteConstant.goodScoreWeight)
                                / (decimal)noteCountNumber * 1000000m;
            int scoreIntRounded = (int)Math.Round(accuracy);
            ScoreObjectTMP.SetText(scoreIntRounded.ToString("D7"));
            ComboObjectTMP.GetComponent<TextMeshProUGUI>().SetText(nowComboNumber.ToString());
            InfoObjectTMP.GetComponent<TextMeshProUGUI>().SetText($"完美 Perfect: {perfectNumber}\n很好 Good: {goodNumber}\n过早 Bad: {badNumber}\n漏键 Miss: {missNumber}\n最大连击 Max Combo: {maxComboNumber}\n物量 Note Count: {noteCountNumber}");
        }
        //float d = Time.time - lastTime;
        //if (d > 0.1f)
        //{
        //    Debug.LogWarning("[Music.cs 卡顿] " + d);
        //}
    }

    private List<string> CollectAllFormulas(MusicData data)
    {
        var formulas = new HashSet<string>(); // 用HashSet自动去重

        foreach (var judgment in data.pavement.judgment)
        {
            // 收集position的xyz公式
            formulas.Add(judgment.position.x);
            formulas.Add(judgment.position.y);
            Debug.Log(judgment.position.y);
            formulas.Add(judgment.position.z);

            // 收集rotation的xyz公式
            formulas.Add(judgment.rotation.x);
            formulas.Add(judgment.rotation.y);
            formulas.Add(judgment.rotation.z);

            // 收集每个note的公式
            foreach (var note in judgment.notes)
            {
                formulas.Add(note.x);
                formulas.Add(note.distance);
            }
        }

        // 移除空字符串
        formulas.Remove("");
        formulas.Remove(null);

        return formulas.ToList();
    }

    Mesh GenerateCylinder()
    {
        // 1. 准备存放顶点和三角形的容器
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float halfHeight = height / 2f;
        float angleStep = 360f / segments;

        // 2. 生成顶点 (从上底面和下底面的圆周开始)
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * i * angleStep;
            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);

            // 下底面圆周顶点 (y = -halfHeight)
            vertices.Add(new Vector3(x, -halfHeight, z));
            // 上底面圆周顶点 (y = +halfHeight)
            vertices.Add(new Vector3(x, halfHeight, z));
        }

        // 3. 构建三角形 (连接上下底面的顶点，形成侧面)
        for (int i = 0; i < segments; i++)
        {
            int bottomLeft = i * 2;
            int topLeft = bottomLeft + 1;
            int bottomRight = bottomLeft + 2;
            int topRight = bottomLeft + 3;

            // 第一个三角形 (左下-右下-左上)
            triangles.Add(bottomLeft);
            triangles.Add(bottomRight);
            triangles.Add(topLeft);

            // 第二个三角形 (右上-左上-右下)
            triangles.Add(topRight);
            triangles.Add(topLeft);
            triangles.Add(bottomRight);
        }

        // 4. 将数据赋值给Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        // 5. 自动计算法线，以便光照生效
        mesh.RecalculateNormals();
        // 可选：自动计算包围盒
        mesh.RecalculateBounds();

        return mesh;
    }
}

[RequireComponent(typeof(Renderer))]
public class NoteController : MonoBehaviour
{
    public int time; // 同 Note Data
    public string distance;
    public string rotation;
    public string x;
    public string type;
    public int length;
    public GameObject judgmentObject;
    public Renderer rendererComponent;

    private Material cachedMaterial;      // 缓存 Material
    private Color originalColor;          // 缓存原始颜色

    void Start()
    {
        rendererComponent = GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            cachedMaterial = rendererComponent.sharedMaterial;
        }
        if (rendererComponent.sharedMaterial != null)
        {
            originalColor = rendererComponent.sharedMaterial.color;
        }
    }

    void Update()
    {
        if (x == null || Music.Instance.elapsedMs == 0) return;
        int delay = time - Music.Instance.elapsedMs;
        transform.position = judgmentObject.transform.TransformPoint(new Vector3(FormulaCompiler.Evaluate(distance, Music.Instance.elapsedMs),
            FormulaCompiler.Evaluate(x, Music.Instance.elapsedMs), 0));
        transform.rotation = judgmentObject.transform.rotation;
        if (judgmentObject == null)
        {
            Music.Instance.notesPool.Release(gameObject);
            return;
        }
        if (delay < 0)
        {
            if (delay <= -NoteConstant.hideValue)
            {
                Music.Instance.missNumber++;
                Music.Instance.nowComboNumber = 0;
                if (Music.Instance.notesQueue.Count > 0 && Music.Instance.notesQueue.Peek() == gameObject)
                {
                    Music.Instance.notesQueue.Dequeue();
                }
                Music.Instance.notesPool.Release(gameObject);
            }
            else if (cachedMaterial != null)
            {
                originalColor.a = (delay + NoteConstant.hideValue) / NoteConstant.hideValue;
                cachedMaterial.color = originalColor;
            }
        }
    }
}