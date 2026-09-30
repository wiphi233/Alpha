using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System;

// 通用数据管理器
public class DataManager : MonoBehaviour, IDataManager
{
    // 当前使用的数据格式（可通过配置修改）
    public static DataFormat CurrentFormat = DataFormat.JSON;

    // 获取可写目录的完整路径
    private static string GetPersistentPath(string fileName, DataFormat format)
    {
        string fullFileName = GetFullFileName(fileName, format);
        return Path.Combine(Application.persistentDataPath, fullFileName);
    }

    // 获取只读目录的完整路径
    private static string GetStreamingPath(string fileName, DataFormat format)
    {
        string fullFileName = GetFullFileName(fileName, format);
        return Application.streamingAssetsPath + "/" + fullFileName;
    }

    // 根据格式获取完整文件名
    private static string GetFullFileName(string fileName, DataFormat format)
    {
        string extension;
        if (format == DataFormat.JSON)
        {
            extension = ".json";
        }
        else if (format == DataFormat.YAML)
        {
            extension = ".yaml";
        }
        else // TEXT
        {
            extension = ".txt";
        }

        // 如果文件名已经包含扩展名，不再添加
        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }
        if (fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }
        if (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }
        return fileName + extension;
    }

    // 序列化数据
    private static string Serialize<T>(T data, DataFormat format)
    {
        if (format == DataFormat.JSON)
        {
            return JsonUtility.ToJson(data, true);
        }
        else if (format == DataFormat.YAML)
        {
            return YamlUtility.StaticToYaml(data);
        }
        else // TEXT
        {
            // 对于TEXT格式，如果T是string直接返回，否则调用ToString()
            if (data is string textData)
            {
                return textData;
            }
            return data?.ToString() ?? string.Empty;
        }
    }

    // 反序列化数据
    private static T Deserialize<T>(string content, DataFormat format)
    {
        if (format == DataFormat.JSON)
        {
            return JsonUtility.FromJson<T>(content);
        }
        else if (format == DataFormat.YAML)
        {
            return YamlUtility.StaticFromYaml<T>(content);
        }
        else // TEXT
        {
            // 对于TEXT格式，尝试将内容转换为T类型
            Type targetType = typeof(T);

            if (targetType == typeof(string))
            {
                return (T)(object)content;
            }
            else if (targetType == typeof(int) && int.TryParse(content, out int intResult))
            {
                return (T)(object)intResult;
            }
            else if (targetType == typeof(float) && float.TryParse(content, out float floatResult))
            {
                return (T)(object)floatResult;
            }
            else if (targetType == typeof(double) && double.TryParse(content, out double doubleResult))
            {
                return (T)(object)doubleResult;
            }
            else if (targetType == typeof(bool) && bool.TryParse(content, out bool boolResult))
            {
                return (T)(object)boolResult;
            }
            else
            {
                // 尝试直接转换（适用于支持类型转换的场景）
                try
                {
                    return (T)Convert.ChangeType(content, targetType);
                }
                catch
                {
                    Debug.LogError($"无法将文本内容转换为类型 {targetType}: {content}");
                    return default(T);
                }
            }
        }
    }

    // 专门用于保存纯文本的便捷方法
    //public static async Task<bool> SaveTextAsync(string text, string fileName)
    //{
    //    return await SaveDataAsync<string>(text, fileName, DataFormat.TEXT);
    //}

    //// 保存纯文本（同步）
    //public static bool SaveText(string text, string fileName)
    //{
    //    return SaveData<string>(text, fileName, DataFormat.TEXT);
    //}

    //// 专门用于加载纯文本的便捷方法
    //public static void LoadText(string fileName, Action<string> onComplete)
    //{
    //    LoadData<string>(fileName, onComplete, DataFormat.TEXT);
    //}

    //// 加载纯文本（异步Task）
    //public static async Task<string> LoadTextAsync(string fileName)
    //{
    //    return await LoadDataAsync<string>(fileName, DataFormat.TEXT);
    //}

    // 1. 保存数据（异步）
    public async Task<bool> SaveDataAsync<T>(T data, string fileName, DataFormat? format = null)
    {
        DataFormat targetFormat = format ?? CurrentFormat;
        return await Task.Run(() =>
        {
            try
            {
                string serializedData = Serialize(data, targetFormat);
                string path = GetPersistentPath(fileName, targetFormat);

                // 确保目录存在
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, serializedData);
                Debug.Log($"数据已保存至: {path} (格式: {targetFormat})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存数据失败: {e.Message}");
                return false;
            }
        });
    }

    // 1. 保存数据（同步）
    public bool SaveData<T>(T data, string fileName, DataFormat? format = null)
    {
        DataFormat targetFormat = format ?? CurrentFormat;
        try
        {
            string serializedData = Serialize(data, targetFormat);
            string path = GetPersistentPath(fileName, targetFormat);

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, serializedData);
            Debug.Log($"数据已保存至: {path} (格式: {targetFormat})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"保存数据失败: {e.Message}");
            return false;
        }
    }

    // 2. 加载数据（异步回调）
    public void LoadData<T>(string fileName, Action<T> onComplete, DataFormat? format = null)
    {
        MonoBehaviour behaviour = GetCoroutineRunner();
        behaviour.StartCoroutine(LoadDataCoroutine(fileName, onComplete, format));
    }

    // 2. 加载数据（异步Task）
    public async Task<T> LoadDataAsync<T>(string fileName, DataFormat? format = null)
    {
        DataFormat targetFormat = format ?? CurrentFormat;
        string persistentPath = GetPersistentPath(fileName, targetFormat);

        // 优先检查用户存档
        if (File.Exists(persistentPath))
        {
            try
            {
                string content = await ReadFileTextAsync(persistentPath);
                return Deserialize<T>(content, targetFormat);
            }
            catch (Exception e)
            {
                Debug.LogError($"读取用户数据失败: {e.Message}");
            }
        }

        // 读取默认数据
        string streamingPath = GetStreamingPath(fileName, targetFormat);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android真机：使用 UnityWebRequest
        using (UnityWebRequest request = UnityWebRequest.Get(streamingPath))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();
                
            if (request.result == UnityWebRequest.Result.Success)
            {
                return Deserialize<T>(request.downloadHandler.text, targetFormat);
            }
            else
            {
                Debug.LogError($"加载默认数据失败: {request.error}");
                return default(T);
            }
        }
#else
        // 编辑器、PC、Mac、iOS：直接读取
        try
        {
            string content = File.ReadAllText(streamingPath);
            return Deserialize<T>(content, targetFormat);
        }
        catch (Exception e)
        {
            Debug.LogError($"读取默认数据失败: {e.Message}");
            return default(T);
        }
#endif
    }

    // 异步读取文件内容的辅助方法
    private static async Task<string> ReadFileTextAsync(string path)
    {
        return await Task.Run(() => File.ReadAllText(path));
    }

    // 异步写入文件内容的辅助方法
    private static async Task WriteFileTextAsync(string path, string content)
    {
        await Task.Run(() => File.WriteAllText(path, content));
    }

    // 协程加载
    private static System.Collections.IEnumerator LoadDataCoroutine<T>(string fileName, Action<T> onComplete, DataFormat? format = null)
    {
        DataFormat targetFormat = format ?? CurrentFormat;
        T result = default(T);
        string persistentPath = GetPersistentPath(fileName, targetFormat);

        // 优先读取用户存档
        if (File.Exists(persistentPath))
        {
            string _content = File.ReadAllText(persistentPath);
            result = Deserialize<T>(_content, targetFormat);
            onComplete?.Invoke(result);
            yield break;
        }

        // 读取默认数据
        string streamingPath = GetStreamingPath(fileName, targetFormat);

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest request = UnityWebRequest.Get(streamingPath))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                result = Deserialize<T>(request.downloadHandler.text, targetFormat);
            }
            else
            {
                Debug.LogError($"加载失败: {request.error}");
            }
        }
#else
        string content = File.ReadAllText(streamingPath);
        result = Deserialize<T>(content, targetFormat);
#endif
        onComplete?.Invoke(result);
    }

    // 3. 检查文件是否存在
    public bool FileExists(string fileName, DataFormat? format = null)
    {
        DataFormat targetFormat = format ?? CurrentFormat;
        string path = GetPersistentPath(fileName, targetFormat);
        return File.Exists(path);
    }

    // 4. 删除文件
    public bool DeleteFile(string fileName, DataFormat? format = null)
    {
        DataFormat targetFormat = format ?? CurrentFormat;
        string path = GetPersistentPath(fileName, targetFormat);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"文件已删除: {path}");
            return true;
        }
        return false;
    }

    // 协程执行器
    private static MonoBehaviour _coroutineRunner;
    private static MonoBehaviour GetCoroutineRunner()
    {
        if (_coroutineRunner == null)
        {
            GameObject go = new GameObject("DataManagerCoroutineRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _coroutineRunner = go.AddComponent<DummyMonoBehaviour>();
        }
        return _coroutineRunner;
    }
}

// YAML 工具类
public class YamlUtility : IYamlUtility
{
    public string ToYaml<T>(T data)
    {
        return StaticToYaml(data);
    }

    public static string StaticToYaml<T>(T data)
    {
        var type = typeof(T);
        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("---");

        foreach (var field in fields)
        {
            object value = field.GetValue(data);
            sb.AppendLine($"{field.Name}: {value}");
        }

        return sb.ToString();
    }

    public T FromYaml<T>(string yamlContent)
    {
        return StaticFromYaml<T>(yamlContent);
    }

    public static T StaticFromYaml<T>(string yamlContent)
    {
        T result = Activator.CreateInstance<T>();
        var lines = yamlContent.Split('\n');
        var type = typeof(T);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("---"))
                continue;

            var parts = line.Split(':');
            if (parts.Length >= 2)
            {
                string fieldName = parts[0].Trim();
                string fieldValue = parts[1].Trim();

                var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    try
                    {
                        object convertedValue = Convert.ChangeType(fieldValue, field.FieldType);
                        field.SetValue(result, convertedValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"YAML转换失败: {fieldName} = {fieldValue}, 错误: {e.Message}");
                    }
                }
            }
        }

        return result;
    }
}

public class DummyMonoBehaviour : MonoBehaviour { }