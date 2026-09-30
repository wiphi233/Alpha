using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingsUI : MonoBehaviour
{
    private static GameSettingsUI Instance; // 不应该允许外部访问

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    public GameObject dataManagerObject;
    private ISignalCenter signalCenter;
    private IDataManager dataManager;

    [Header("UI 界面")]
    public GameObject SettingsCanvas;
    private RectTransform SettingsCanvasRectTransform;

    public GameObject BoolSettingItemPerfab;
    public GameObject InputSettingItemPerfab;
    public GameObject ContentObject;

    public GameObject GroupObject;
    private RectTransform GroupRectTransform;

    private bool isDataLoaded = false;
    private List<GameObject> SettingItemsObjects = new List<GameObject>();
    private readonly string GameSettingsFileName = "GameSettings.json";

    public struct SettingItem
    {
        public string type;
        public string categoryKey;
        public string settingKey;
        public TMP_InputField inputField;
        public Setting settingData;
        public TMP_InputField.ContentType contentType;
        public Toggle toggle;
    };

    void Awake()
    {
        GroupRectTransform = GroupObject.GetComponent<RectTransform>();
        SettingsCanvasRectTransform = SettingsCanvas.GetComponent<RectTransform>();
        if (Instance != null)
        {
            Destroy(gameObject);
            Debug.LogWarning("GameSettingsUI instance already exists. Destroying duplicate.");
            return;
        }
        Instance = this;
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        dataManager = dataManagerObject.GetComponent<IDataManager>();
        //dataManager.SaveData(playerData, fileName, DataFormat.JSON);
        dataManager.LoadData<string>(GameSettingsFileName, (loadedData) =>
        {
            if (loadedData != null)
            {
                isDataLoaded = true;
                Alpha.gameSettings = GameSettings.LoadFromJson(loadedData);
                Changed();
            }
            else
            {
                Debug.LogError($"数据加载失败：{GameSettingsFileName}");
            }
        }, DataFormat.TEXT); // 不需要解析，使用 TEXT
    }

    public void OnOpenGameSettings()
    {
        foreach (KeyValuePair<string, Dictionary<string, Setting>> categorykvp in Alpha.gameSettings.Categories)
        {
            foreach (KeyValuePair<string, Setting> settingkvp in categorykvp.Value)
            {
                //settingkvp.Key
                if (settingkvp.Value.Type == "Bool")
                {
                    GameObject item = Instantiate(BoolSettingItemPerfab);
                    SettingItemsObjects.Add(item);
                    item.SetActive(true);
                    item.transform.SetParent(ContentObject.transform, false);
                    item.GetComponent<TextMeshProUGUI>().SetText(settingkvp.Key);
                    Toggle toggle = item.transform.Find("Toggle").GetComponent<Toggle>();
                    toggle.isOn = (bool)settingkvp.Value.CurrentValue;
                    SettingItem settingItem = new SettingItem
                    {
                        categoryKey = categorykvp.Key,
                        settingKey = settingkvp.Key,
                        settingData = settingkvp.Value,
                        type = "Bool",
                        toggle = toggle
                    };
                    toggle.onValueChanged.AddListener((bool value) => OnSettingChanged(settingItem, value));
                }
                else if (settingkvp.Value.Type == "Int" || settingkvp.Value.Type == "Float" ||
                    settingkvp.Value.Type == "Password" || settingkvp.Value.Type == "Name" ||
                    settingkvp.Value.Type == "Email" || settingkvp.Value.Type == "String")
                {
                    GameObject item = Instantiate(InputSettingItemPerfab);
                    SettingItemsObjects.Add(item);
                    item.SetActive(true);
                    item.transform.SetParent(ContentObject.transform, false);
                    item.GetComponent<TextMeshProUGUI>().SetText(settingkvp.Key);
                    Transform inputFieldTransform = item.transform.Find("InputField (TMP)");
                    if (inputFieldTransform != null)
                    {
                        if (inputFieldTransform.TryGetComponent<TMP_InputField>(out var inputField))
                        {
                            inputField.text = settingkvp.Value.CurrentValue.ToString();
                            if (settingkvp.Value.Type == "Int")
                            {
                                inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                            }
                            else if (settingkvp.Value.Type == "Float")
                            {
                                inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                            }
                            else if (settingkvp.Value.Type == "Password")
                            {
                                inputField.contentType = TMP_InputField.ContentType.Password;
                            }
                            else if (settingkvp.Value.Type == "Name")
                            {
                                inputField.contentType = TMP_InputField.ContentType.Name;
                            }
                            else if (settingkvp.Value.Type == "Email")
                            {
                                inputField.contentType = TMP_InputField.ContentType.EmailAddress;
                            }
                            else if (settingkvp.Value.Type == "String")
                            {
                                inputField.contentType = TMP_InputField.ContentType.Standard;
                            }
                            SettingItem settingItem = new SettingItem
                            {
                                categoryKey = categorykvp.Key,
                                settingKey = settingkvp.Key,
                                inputField = inputField,
                                settingData = settingkvp.Value,
                                contentType = inputField.contentType,
                                type = "Input"
                            };

                            inputField.onValueChanged.AddListener((string value) => OnSettingChanged(settingItem, value));
                        }
                        else
                        {
                            Debug.LogError("InputField (TMP) 上没有 InputField 组件");
                        }
                    }
                    else
                    {
                        Debug.LogError("找不到名为 InputField (TMP) 的子物体");
                    }
                }
                else
                {
                    GameObject item = Instantiate(BoolSettingItemPerfab);
                    item.SetActive(true);
                    item.transform.SetParent(ContentObject.transform, false);
                    item.GetComponent<TextMeshProUGUI>().SetText(settingkvp.Key);
                    Destroy(item.transform.Find("Toggle").gameObject);
                    SettingItemsObjects.Add(item);
                }
            }
        }
        GroupRectTransform.anchoredPosition = new Vector2(GroupRectTransform.anchoredPosition.x, SettingsCanvasRectTransform.rect.height);
        SettingsCanvas.SetActive(true);
        signalCenter.Emit(SignalType.ChangeUIState, gameObject, 0);
        DOVirtual.Float(SettingsCanvasRectTransform.rect.height, 0f, 1f, (float k) =>
        {
            GroupRectTransform.anchoredPosition = new Vector2(GroupRectTransform.anchoredPosition.x, k);
        });
    }

    void OnSettingChanged(SettingItem item, object value)
    {
        if (item.type == "Bool")
        {
            Alpha.gameSettings.Categories[item.categoryKey][item.settingKey].CurrentValue = (object)Convert.ToBoolean(item.toggle.isOn);
        }
        else // "Input"
        {
            if (string.IsNullOrEmpty(Convert.ToString(value)))
            {
                return;
            }
            if (item.contentType == TMP_InputField.ContentType.IntegerNumber)
            {
                if (int.TryParse(Convert.ToString(value), out int result))
                {
                    result = Mathf.Clamp(result, Convert.ToInt32(item.settingData.MinValue), Convert.ToInt32(item.settingData.MaxValue));
                    item.inputField.text = result.ToString();
                    Alpha.gameSettings.Categories[item.categoryKey][item.settingKey].CurrentValue = (object)Convert.ToInt32(item.inputField.text);
                }
            }
            else if (item.contentType == TMP_InputField.ContentType.DecimalNumber)
            {
                if (float.TryParse(Convert.ToString(value), out float result))
                {
                    result = Mathf.Clamp(result, (float)Convert.ToDouble(item.settingData.MinValue), (float)Convert.ToDouble(item.settingData.MaxValue));
                    item.inputField.text = result.ToString();
                    Alpha.gameSettings.Categories[item.categoryKey][item.settingKey].CurrentValue = (object)Convert.ToDouble(item.inputField.text);
                }
            }
            else
            {
                Alpha.gameSettings.Categories[item.categoryKey][item.settingKey].CurrentValue = (object)item.inputField.text;
            }
        }
    }

    public void Changed()
    {
        AudioListener.volume = Convert.ToInt32(Alpha.gameSettings.Categories["General"]["Volume"].CurrentValue);
        signalCenter.Emit(SignalType.ChangeGameSettings, gameObject);
    }

    public void OnSaveAndExitGameSettings()
    {
        dataManager.SaveData<string>(GameSettings.ToJson(Alpha.gameSettings), GameSettingsFileName, DataFormat.TEXT);
        Changed();
        DOVirtual.Float(0f, SettingsCanvasRectTransform.rect.height, 1f, (float k) =>
        {
            GroupRectTransform.anchoredPosition = new Vector2(GroupRectTransform.anchoredPosition.x, k);
        }).OnComplete(() =>
        {
            foreach (var item in SettingItemsObjects)
            {
                Destroy(item);
            }
            SettingsCanvas.SetActive(false);
            signalCenter.Emit(SignalType.ChangeUIState, gameObject, 1);
        });
    }

    void Start()
    {
        StartCoroutine(WaitForDataLoad());
    }

    IEnumerator WaitForDataLoad()
    {
        // 等待数据加载完成，最多等待5秒
        float timeout = 5f;
        while (!isDataLoaded && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (isDataLoaded)
        {
            OnSettingsLoaded();
        }
        else
        {
            Debug.LogError("GameSettings 加载超时！");
        }
    }

    void OnSettingsLoaded()
    {
        if (Alpha.gameSettings != null && Alpha.gameSettings.Categories != null)
        {
            Debug.Log($"Language: {Alpha.gameSettings.Categories["General"]["Language"].CurrentValue}\n{Alpha.gameSettings.Categories["Graphics"]["ViewDistance"].CurrentValue}");
        }
    }
}
