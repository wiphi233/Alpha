/* 
 * Alpha OS
 * The Earliest Version: v.0.1.0
 * WiPhi233_awa's Works
 * Birthday: 2026/2/22
 * 
 * 
 * 
 * 
 * 本文件主要包含定义
 * 
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.Json;
using System.Text.Json.Serialization;

/*
粗体	<b> </b>	"普通 <b>加粗文字</b> 普通"
斜体	<i> </i>	"普通 <i>斜体文字</i> 普通"
<u>下划线</u>	<u> </u>	"普通 <u>下划线文字</u> 普通"
~~删除线~~	<s> </s> 或 <strikethrough>	"普通 <s>删除线文字</s> 普通"
彩色文字	<color=#RRGGBB> 或 <color=颜色名>	"普通 <color=#FF0000>红色文字</color> 普通"
多种组合	嵌套使用	"<b><i><color=yellow>粗斜黄字</color></i></b>"
*/


public class AlphaFragment
{
    public readonly String Collector;
    public readonly DateTime CollectionTime;
    public readonly String Title;
    public readonly String Content;
    public AlphaFragment(String _Collector, String _Content, String _Title, DateTime _CollectionTime)
    {
        Collector = _Collector;
        CollectionTime = _CollectionTime;
        Title = _Title;
        Content = _Content;
    }
}


[Serializable]
public class StringTriad
{
    // 三元组，string 类型
    public string x;
    public string y;
    public string z;
}

[Serializable]
public class NoteData
{
    public int time;
    public string distance;
    public string rotation;
    public string x;
    public string type;
    public int length;
}

[Serializable]
public class JudgmentData
{
    public string type;
    public int start;
    public int end;
    public string alpha;
    public StringTriad position;
    public StringTriad rotation;
    public List<NoteData> notes;
    [NonSerialized] public GameObject judgmentObject;
}

[Serializable]
public class DecorationData
{

}

[Serializable]
public class PavementData
{
    public List<JudgmentData> judgment;
    public List<DecorationData> decoration;
    public PavementData()
    {

    }
}

[Serializable]
public class MusicData
{
    public string name;
    public string musicAuthor;
    public string pavementAuthor;
    public string illustration;
    public string illustrationAuthor;
    public string audio;
    public PavementData pavement;

    public MusicData()
    {
        name = "Song";
        musicAuthor = "Unknown";
        pavementAuthor = "Unknown";
        illustration = "";
        illustrationAuthor = "Unknown";
        audio = "";
    }
}

public static class Tags
{
    public const string Note = "Note";
}

public class NoteConstant
{
    public const float hideValue = 130f;
    public const int perfectRange = 60;
    public const int goodRange = 130;
    public const int badRange = 200;
    public const double goodScoreWeight = 0.6;
}

public enum GameState
{
    LoadNotStarted,        // 加载未开始
    LoadBeginning,         // 加载开始
    LoadGeneratingTerrain, // 正在生成地形
    LoadingCompleted,      // 加载完成
    //PlayingCutscene,       // 正在播放 CG （过场动画）
    GameCrashed,           // 游戏已崩溃
}

public class GameStateManager
{
    private static GameState GameState = GameState.LoadNotStarted;

    public static void Set(GameState newState)
    {
        if (newState == GameState.GameCrashed) { Debug.LogWarning($"[Game State Changed] {GameState} => {newState}"); }
        else { Debug.Log($"[Game State Changed] {GameState} => {newState}"); }
        GameState = newState;
    }

    public static GameState Get()
    {
        return GameState;
    }
}

//public enum AngularBallState
//{
//    Inactive = 0,
//    Active = 1
//}


public enum PlayerType
{
    Banned,
    Normal,
    Administrator,
    Sudo
}

[Serializable]
public class PlayerData
{
    public string name;
    public PlayerType playerType;
    public string description;
    public string avatar;
    public string passwordHash;
    public int level;
    public int experience;
    public int age;
    public int crystalFragments;
    public TimeSpan totalPlayTime;

    public void Sudo()
    {
        const int Infinity = 1000000000; // 十亿
        name = "Sudo";
        description = "Sudo User";
        avatar = "";
        passwordHash = null;
        level = Infinity;
        experience = Infinity;
        age = 30;
        crystalFragments = Infinity;
        totalPlayTime = TimeSpan.MaxValue;
    }

    public PlayerData()
    {
        name = "Player";
        description = "";
        avatar = "";
        passwordHash = null;
        level = 0;
        experience = 0;
        age = 30;
        crystalFragments = 0;
        totalPlayTime = TimeSpan.FromSeconds(0);
    }
}

/// <summary>
/// 自定义 TimeSpan 转换器，将 TimeSpan 序列化为 "d.hh:mm:ss" 格式的字符串
/// </summary>
//public class TimeSpanConverter : JsonConverter<TimeSpan>
//{
//    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//    {
//        string value = reader.GetString();
//        return TimeSpan.Parse(value);
//    }

//    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
//    {
//        writer.WriteStringValue(value.ToString()); // 格式如 "1.02:03:04" 表示 1天2小时3分钟4秒
//    }
//}

//public static class PlayerDataJsonSerializer
//{
//    private static readonly JsonSerializerOptions _options;

//    static PlayerDataJsonSerializer()
//    {
//        _options = new JsonSerializerOptions
//        {
//            WriteIndented = true,           // 格式化输出，方便阅读
//            IncludeFields = true            // 序列化公共字段（因为 PlayerData 使用字段而非属性）
//        };
//        // 添加 TimeSpan 转换器
//        _options.Converters.Add(new TimeSpanConverter());
//    }

//    /// <summary>
//    /// 将 PlayerData 对象序列化为 JSON 字符串
//    /// </summary>
//    public static string Serialize(PlayerData data)
//    {
//        return JsonSerializer.Serialize(data, _options);
//    }

//    /// <summary>
//    /// 从 JSON 字符串反序列化为 PlayerData 对象
//    /// </summary>
//    public static PlayerData Deserialize(string json)
//    {
//        return JsonSerializer.Deserialize<PlayerData>(json, _options);
//    }
//}

public class WorldConfig
{
    public static readonly float waterLevelHeight = 0.4f;
    public static readonly float heightMultiplier = 300f;
    public static readonly int chunkSize = 128;
    public static readonly float xSpacing = 1f;
    public static readonly float zSpacing = 1f;
    public static int loadFPS = 120;
    public static float loadSPF = 1f / loadFPS;
    //public static int viewDistance = 16;
    //public static int viewDistance = 26;
    //public static int viewDistance = 48;
    public static readonly Color color0 = new Color(1f, 0.980392f, 0.980392f);  // 雪
    public static readonly Color color1 = new Color(0.6f, 0.8f, 0.4f);  // 浅绿 (高处)
    public static readonly Color color2 = new Color(0.3f, 0.5f, 0.2f);  // 深绿
    public static readonly Color color3 = new Color(0.5f, 0.4f, 0.2f);  // 土色
    public static readonly Color color4 = new Color(0.7f, 0.7f, 0.7f);  // 浅层石头色
    public static readonly Color color5 = new Color(0.2f, 0.2f, 0.2f);  // 深层石头色 (最低处)
}