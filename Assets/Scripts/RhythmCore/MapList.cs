using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
//using UnityEngine.WSA;

[System.Serializable]
public struct FolderInfo
{
    public string fullPath;
    public string folderName;

    public FolderInfo(string fullPath, string folderName)
    {
        this.fullPath = fullPath;
        this.folderName = folderName;
    }
}

public class MapList : MonoBehaviour
{
    public static MapList Instance;

    [Header("Map List 界面")]
    public GameObject MapListCanvasObject;
    public GameObject CurrentSongName;
    public GameObject CurrentMusicAuthor;
    public GameObject CurrentPavementAuthor;
    public GameObject CurrentIllustrationAuthor;
    public GameObject CurrentTime;
    public GameObject CurrentSongState;
    public GameObject CurrentSongIllustration;
    public GameObject WallLeft;
    public GameObject WallRight;
    public GameObject Content;
    public GameObject Line;
    public GameObject LeftSong;
    public GameObject MiddleSong;
    public GameObject RightSong;
    // Illustration SongInfo DifficultyList Difficulties 根据名称获取
    public GameObject DownDifficultyList;
    public GameObject DownDifficultyTextObject;

    private Canvas MapListCanvas;

    private TextMeshProUGUI CurrentSongNameTMP;
    private TextMeshProUGUI CurrentMusicAuthorTMP;
    private TextMeshProUGUI CurrentPavementAuthorTMP;
    private TextMeshProUGUI CurrentIllustrationAuthorTMP;
    private TextMeshProUGUI CurrentTimeTMP;
    private Image CurrentSongStateImage;
    private TextMeshProUGUI CurrentSongStateTMP;
    private Image CurrentSongIllustrationImage;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    public GameObject dataManagerObject;
    private ISignalCenter signalCenter;
    private IDataManager dataManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("重复的 Map List 实例");
            return;
        }
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        dataManager = dataManagerObject.GetComponent<IDataManager>();
        if (MapListCanvasObject == null || CurrentSongName == null || CurrentMusicAuthor == null || CurrentPavementAuthor == null ||
            CurrentIllustrationAuthor == null || CurrentTime == null || CurrentSongState == null || CurrentSongIllustration == null ||
            WallLeft == null || WallRight == null || Content == null || Line == null || LeftSong == null || MiddleSong == null || RightSong == null ||
            signalCenterObject == null || dataManagerObject == null)
        {
            Debug.LogError("Map List: 参数未设置完全！");
        }
        else
        {
            MapListCanvas = MapListCanvasObject.GetComponent<Canvas>();
            CurrentSongNameTMP = CurrentSongName.GetComponent<TextMeshProUGUI>();
            CurrentMusicAuthorTMP = CurrentMusicAuthor.GetComponent<TextMeshProUGUI>();
            CurrentPavementAuthorTMP = CurrentPavementAuthor.GetComponent<TextMeshProUGUI>();
            CurrentIllustrationAuthorTMP = CurrentIllustrationAuthor.GetComponent<TextMeshProUGUI>();
            CurrentTimeTMP = CurrentTime.GetComponent<TextMeshProUGUI>();
            CurrentSongStateImage = CurrentSongState.GetComponent<Image>();
            CurrentSongStateTMP = CurrentSongState.GetComponentInChildren<TextMeshProUGUI>();
            CurrentSongIllustrationImage = CurrentSongIllustration.GetComponent<Image>();
        }
    }

    void Start()
    {
        signalCenter.Subscribe(SignalType.EnterRhythmGame, OnEnterRhythmGame);
    }

    public void OpenCloseWall(Action func)
    {
        RectTransform WallLeftRT = WallLeft.GetComponent<RectTransform>();
        RectTransform WallRightRT = WallRight.GetComponent<RectTransform>();
        WallLeftRT.anchoredPosition = new Vector2(-MapListCanvas.pixelRect.width / 2, 0);
        WallRightRT.anchoredPosition = new Vector2(MapListCanvas.pixelRect.width / 2, 0);
        WallLeft.SetActive(true);
        WallRight.SetActive(true);
        DOVirtual.Float(MapListCanvas.pixelRect.width / 2f, 0f, 1f, (float value) =>
        {
            WallLeftRT.anchoredPosition = new Vector2(-value, 0);
            WallRightRT.anchoredPosition = new Vector2(value, 0);
        }).OnComplete(() =>
        {
            func();
            DOVirtual.Float(0f, MapListCanvas.pixelRect.width / 2f, 1f, (float value) =>
            {
                WallLeftRT.anchoredPosition = new Vector2(-value, 0);
                WallRightRT.anchoredPosition = new Vector2(value, 0);
            }).OnComplete(() =>
            {
                WallLeft.SetActive(false);
                WallRight.SetActive(false);
            });
        });
    }

    public void OnEnterRhythmGame(GameObject sender, object data)
    {
        MapListCanvasObject.SetActive(false);
        LoadCanvas();
        MapListCanvasObject.SetActive(true);
    }

    public static List<FolderInfo> GetTopLevelFolders()
    {
        string rootPath = Path.Combine(Application.streamingAssetsPath, "Music");
        List<FolderInfo> folderList = new List<FolderInfo>();

        if (Directory.Exists(rootPath))
        {
            // 只获取直接子目录（不递归）
            string[] subDirs = Directory.GetDirectories(rootPath, "*", SearchOption.TopDirectoryOnly);
            foreach (string dir in subDirs)
            {
                string name = Path.GetFileName(dir);
                folderList.Add(new FolderInfo(dir, name));
            }
        }
        else
        {
            Debug.LogWarning($"Music folder not found: {rootPath}");
        }

        return folderList;
    }

    /// <summary>
    /// 使用 UnityWebRequest 同时加载音频和曲绘（异步）
    /// </summary>
    /// <param name="folderPath">文件夹完整物理路径（如 Application.streamingAssetsPath + "/Songs"）</param>
    /// <param name="musicData">音乐数据，包含 audio 和 illustration 文件名</param>
    /// <returns>协程迭代器</returns>
    IEnumerator LoadAudioAndIllustration(string folderPath, MusicData musicData)
    {
        // 1. 拼接完整路径，并统一为 Unix 风格（Unity 更兼容）
        string audioFullPath = Path.Combine(folderPath, musicData.audio).Replace('\\', '/');
        string illustFullPath = Path.Combine(folderPath, musicData.illustration).Replace('\\', '/');

        // 2. 本地文件需加 "file://" 前缀
        string audioUrl = "file://" + audioFullPath;
        string illustUrl = "file://" + illustFullPath;

        // 3. 获取音频格式（根据扩展名）
        AudioType audioType = GetAudioTypeFromExtension(audioFullPath);

        // 4. 创建两个请求（音频 + 纹理）
        using (UnityWebRequest audioUwr = UnityWebRequestMultimedia.GetAudioClip(audioUrl, audioType))
        using (UnityWebRequest textureUwr = UnityWebRequestTexture.GetTexture(illustUrl))
        {
            // 5. 同时发送请求
            audioUwr.SendWebRequest();
            textureUwr.SendWebRequest();

            // 6. 等待两者全部完成（不阻塞主线程）
            while (!audioUwr.isDone || !textureUwr.isDone)
                yield return null;

            // 7. 处理音频加载结果
            if (audioUwr.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(audioUwr);
                if (clip != null)
                {
                    // 更新显示歌曲时长
                    CurrentTimeTMP.SetText($"时间：{TimeSpan.FromSeconds(clip.length).ToString(@"mm\:ss")}");
                    // 如果需要播放，可以保存 clip 到 AudioSource
                    // audioSource.clip = clip; 
                    // 注意：此处 clip 已解码完成，可直接播放，无需再 LoadAudioData()
                }
                else
                {
                    Debug.LogError($"音频解析失败（clip 为空）: {audioUrl}");
                }
            }
            else
            {
                Debug.LogError($"音频加载失败: {audioUwr.error}\n路径: {audioUrl}");
            }

            // 8. 处理曲绘加载结果
            if (textureUwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(textureUwr);
                if (texture != null)
                {
                    // 将 Texture2D 转换为 Sprite（注意：需要设置像素坐标）
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f) // 锚点居中
                    );
                    CurrentSongIllustrationImage.sprite = sprite;
                }
                else
                {
                    Debug.LogError($"曲绘解析失败（纹理为空）: {illustUrl}");
                }
            }
            else
            {
                Debug.LogError($"曲绘加载失败: {textureUwr.error}\n路径: {illustUrl}");
            }

            // 9. 其他 UI 更新（不依赖加载的部分可以提前设置，但为了统一，在此也可补充）
            CurrentSongStateImage = CurrentSongState.GetComponent<Image>();
            CurrentSongStateTMP.SetText("内置");
            // 注意：歌曲名、作者等已在外部设置，此处无需重复
        }
    }

    /// <summary>
    /// 辅助：根据文件扩展名返回 Unity 音频类型
    /// </summary>
    private AudioType GetAudioTypeFromExtension(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        switch (ext)
        {
            case ".wav": return AudioType.WAV;
            case ".mp3": return AudioType.MPEG;
            case ".ogg": return AudioType.OGGVORBIS;
            case ".aiff": return AudioType.AIFF;
            case ".aif": return AudioType.AIFF;
            default: return AudioType.UNKNOWN; // 让 Unity 尝试自动识别（可能失败）
        }
    }

    private void RefreshSongList()
    {
        Transform parentTransform = Content.transform;
        int childCount = parentTransform.childCount;
        // 必须使用 for 倒序遍历，因为销毁子物体后 childCount 会变化，索引会失效
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = parentTransform.GetChild(i);
            if (child.name != Line.name) // 它们的 Instance ID 不同，但 name 相同，非常奇怪，能用就行。
            {
                Debug.Log($"Destory: {child.gameObject.name}");
                Destroy(child.gameObject);
            }
        }
        var songFolders = GetTopLevelFolders();
        bool theFirstSong = true;
        int rows = 0;
        int column = 3; // [1, 3]
        GameObject lineObj = null;
        Line.SetActive(true);
        foreach (FolderInfo folder in songFolders)
        {
            if (File.Exists(Path.Combine(folder.fullPath, $"{folder.folderName}.json")))
            {
                // 读取 JSON 文件并解析为 SongInfo 对象
                string fileName = Path.Combine(folder.fullPath, $"{folder.folderName}.json");
                MusicData musicData = null;
                dataManager.LoadData<MusicData>(fileName, (loadedData) =>
                {
                    if (loadedData != null)
                    {
                        musicData = loadedData;
                        Debug.Log($"曲目数据加载成功：{fileName}\nname: {musicData.name}\nmusicAuthor: {musicData.musicAuthor}\npavementAuthor: {musicData.pavementAuthor}\nillustration: {musicData.illustration}\nillustrationAuthor: {musicData.illustrationAuthor}\naudio: {musicData.audio}");
                        if (theFirstSong)
                        {
                            theFirstSong = false;
                            CurrentSongNameTMP.SetText(musicData.name);
                            CurrentMusicAuthorTMP.SetText("曲师：" + musicData.musicAuthor);
                            CurrentPavementAuthorTMP.SetText("谱师：" + musicData.pavementAuthor);
                            CurrentIllustrationAuthorTMP.SetText("曲绘：" + musicData.illustrationAuthor);

                            // ----- 异步加载音频和曲绘 -----
                            StartCoroutine(LoadAudioAndIllustration(folder.fullPath, musicData));
                        }
                        if (column == 3)
                        {
                            column = 1;
                            rows++;
                            lineObj = Instantiate(Line, Content.transform);
                        }
                        else
                        {
                            column++;
                        }
                        GameObject songObj = null;
                        Transform songTransform = null;
                        if (column == 1)
                        {
                            songTransform = lineObj.transform.Find("LeftSong");
                        }
                        else if (column == 2)
                        {
                            songTransform = lineObj.transform.Find("MiddleSong");
                        }
                        else if (column == 3)
                        {
                            songTransform = lineObj.transform.Find("RightSong");
                        }
                        if (songTransform != null)
                        {
                            songObj = songTransform.gameObject;
                            songObj.GetComponent<Image>().enabled = true;
                            Transform illustrationTransform = songTransform.Find("Illustration");
                            Transform songInfoTransform = songTransform.Find("SongInfo");
                            if (illustrationTransform != null)
                            {
                                illustrationTransform.gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(Path.Combine(folder.fullPath, musicData.illustration));
                            }
                            if (songInfoTransform != null)
                            {
                                songInfoTransform.gameObject.GetComponent<TextMeshProUGUI>().SetText(
                                    $"{musicData.name}\nby {musicData.musicAuthor}\nmapped by {musicData.pavementAuthor}");
                            }
                        }
                        else
                        {
                            Debug.LogError("Map List: 找不到对应的 Song 物体！");
                        }
                    }
                    else
                    {
                        Debug.LogError($"曲目数据加载失败：{fileName}");
                    }
                }, DataFormat.JSON);
            }
            else
            {
                Debug.LogWarning($"JSON file not found for folder: {folder.fullPath}");
            }
        }
        Line.SetActive(false); // 恢复预制体状态
        if (lineObj == null)
        {
            return;
        }
        if (column <= 2)
        {
            Transform songTransform = lineObj.transform.Find("RightSong");
            if (songTransform != null)
            {
                Destroy(songTransform.gameObject);
            }
        }
        if (column <= 1)
        {
            Transform songTransform = lineObj.transform.Find("MiddleSong");
            if (songTransform != null)
            {
                Destroy(songTransform.gameObject);
            }
        }
    }

    private void LoadCanvas()
    {
        RefreshSongList();
    }

    public void OnSongSelected()
    {

    }

    private void ExitMapList(SignalType signalType)
    {
        OpenCloseWall(() =>
        {
            MapListCanvasObject.SetActive(false);
            signalCenter.Emit(signalType, gameObject);
        });
    }

    public void OnExitButtonClicked()
    {
        ExitMapList(SignalType.BootCompletion);
    }

    public void OnStartButtonClicked()
    {
        ExitMapList(SignalType.LoadMusic);
    }
}