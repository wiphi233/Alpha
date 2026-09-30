using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;


[Serializable]
public class GameSettings
{
    public string FormatVersion;
    public Dictionary<string, Dictionary<string, Setting>> Categories;

    // 加载JSON
    public static GameSettings LoadFromJson(string json)
    {
        return JsonConvert.DeserializeObject<GameSettings>(json);
    }

    // 保存JSON
    public static string ToJson(GameSettings settings)
    {
        return JsonConvert.SerializeObject(settings, Formatting.Indented);
    }
}

[Serializable]
public class Setting
{
    public string Type;
    //public LocalizedString description;
    public object MinValue;
    public object MaxValue;
    public object DefaultValue;
    public object CurrentValue;
    public bool Lock;
}

[Serializable]
public class LocalizedString
{
    public string en_gb;
    public string zh_cn;
    public string zh_tw;
}

[Serializable]
public class Option
{
    public int value;
    public LocalizedString name;
}


/*
        playerData = new PlayerData();
        playerData.Sudo();
        string fileName = $"Players/PlayerData";
        dataManager = dataManagerObject.GetComponent<IDataManager>();
        dataManager.SaveData(playerData, fileName, DataFormat.JSON);
*/

public static class Alpha
{
    public static GameSettings gameSettings = new GameSettings();
}