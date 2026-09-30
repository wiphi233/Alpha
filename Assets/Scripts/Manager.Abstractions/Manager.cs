using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


// 为规范信号的使用，以下列出所有信号类型，而不使用string
public enum SignalType
{
    BootCompletion,
    ChangeAngularBall,
    ExitHomepage,
    LoadMusic,
    EnterWorld,
    WorldLoaded,
    ChangeUIState,
    ChangeGameSettings,
    EnterRhythmGame
}

// 数据格式枚举
public enum DataFormat
{
    JSON,
    YAML,
    TEXT
}

public interface IDataManager
{
    Task<bool> SaveDataAsync<T>(T data, string fileName, DataFormat? format = null);
    bool SaveData<T>(T data, string fileName, DataFormat? format = null);
    void LoadData<T>(string fileName, Action<T> onComplete, DataFormat? format = null);
    Task<T> LoadDataAsync<T>(string fileName, DataFormat? format = null);
    bool FileExists(string fileName, DataFormat? format = null);
    bool DeleteFile(string fileName, DataFormat? format = null);
}

public interface IYamlUtility
{
    string ToYaml<T>(T data);
    T FromYaml<T>(string yamlContent);
}

public interface ISignalCenter
{
    void Subscribe(SignalType signalName, System.Action<GameObject, object> callback);
    void Unsubscribe(SignalType signalName, System.Action<GameObject, object> callback);
    void Emit(SignalType signalType, GameObject sender, object data = null);
}