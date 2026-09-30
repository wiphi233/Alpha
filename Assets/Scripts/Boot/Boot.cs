using DG.Tweening;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.Rendering.HighDefinition;

public class Boot : MonoBehaviour
{
    [HideInInspector] public static Boot Instance;

    [Header("PSE Warning 光敏性癫痫警告")]
    public GameObject PSEWarningTitle;
    private Material PSEWarningTitleMaterial;

    public GameObject BackgroundCube;
    private Material BackgroundCubeMaterial;
    
    public GameObject PSEWarningMessage;
    private CanvasGroup PSEWarningMessageCanvasGroup;

    [SerializeField] private GameObject targetObject;

    private string versionText;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    public GameObject dataManagerObject;
    private ISignalCenter signalCenter;
    private IDataManager dataManager;

    private PlayerData playerData;

    [Header("UI")]
    public GameObject canvas;
    public GameObject iconObject;
    public GameObject authorObject;
    public GameObject lineObject;
    public GameObject clickTipObject;
    public GameObject maskObject;
    public GameObject versionObject;
    public GameObject GlobalVolumeObject;
    public GameObject titleObject;
    [ShowAssetPreview] public Sprite triangle;
    [ShowAssetPreview] public Sprite rect;
    [ShowAssetPreview] public Sprite circle;

    private List<Graph> graphs = new List<Graph>();
    private List<GameObject> graphObjects = new List<GameObject>();
    private List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
    private RectTransform canvasRect;
    private int state = 0;

    void Awake()
    {
        state = 0;
        if (Instance == null)
        {
            Instance = this;
            GameStateManager.Set(GameState.LoadNotStarted);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        Application.runInBackground = true;
        versionText = $"v.{Application.version}";
        versionObject.GetComponent<TextMeshProUGUI>().text = $"<gradient=\"Version\">{versionText}</gradient> <gradient=\"Update\">Update</gradient>";
    }

    void Start()
    {
        iconObject.SetActive(false);
        authorObject.SetActive(false);
        lineObject.SetActive(false);
        clickTipObject.SetActive(false);
        maskObject.SetActive(false);
#if false// UNITY_EDITOR
        EnterBoot();
#else
        PSEWarningTitle.SetActive(true);
        PSEWarningMessage.SetActive(true);
        BackgroundCube.SetActive(true);
        PSEWarningMessageCanvasGroup = PSEWarningMessage.GetComponent<CanvasGroup>();
        BackgroundCubeMaterial = BackgroundCube.GetComponent<MeshRenderer>().material;
        PSEWarningTitleMaterial = PSEWarningTitle.GetComponent<MeshRenderer>().material;
        DOVirtual.DelayedCall(3f, WaitAndEnterBoot);
#endif
    }

    void WaitAndEnterBoot()
    {
        PSEWarningTitle.SetActive(false);
        PSEWarningMessage.SetActive(false);
        BackgroundCube.SetActive(false);
        EnterBoot();
    }

    private void EnterBoot()
    {
        Debug.Log("Enter Boot.");
        AudioManager.bgmSource.Play();
        if (GlobalVolumeObject.GetComponent<Volume>().profile.TryGet<Vignette>(out Vignette vignette))
        {
            vignette.intensity.value = 0.3f;
        }

        canvasRect = canvas.GetComponent<Canvas>().GetComponent<RectTransform>();

        playerData = new PlayerData();
        playerData.Sudo();
        string fileName = $"Players/PlayerData";
        dataManager = dataManagerObject.GetComponent<IDataManager>();
        dataManager.SaveData(playerData, fileName, DataFormat.JSON);
        LightingManager.SetSkyboxGradient(Color.white, Color.white, Color.white, 16f);
        RectTransform lineObjectRT = lineObject.GetComponent<RectTransform>();
        RectTransform authorObjectRT = authorObject.GetComponent<RectTransform>();
        RectTransform clickTipObjectRT = clickTipObject.GetComponent<RectTransform>();
        RectTransform iconObjectRT = iconObject.GetComponent<RectTransform>();
        CanvasGroup lineObjectCG = lineObject.GetComponent<CanvasGroup>();
        CanvasGroup authorObjectCG = authorObject.GetComponent<CanvasGroup>();
        CanvasGroup iconObjectCG = iconObject.GetComponent<CanvasGroup>();
        CanvasGroup clickTipObjectCG = clickTipObject.GetComponent<CanvasGroup>();
        CanvasGroup titleObjectCG = titleObject.GetComponent<CanvasGroup>();

        DOVirtual.DelayedCall(.8f, () =>
        {
            lineObject.SetActive(true);
            DOVirtual.Float(canvasRect.rect.width, 0f, .5f, (float x) =>
            {
                lineObjectRT.anchoredPosition = new Vector2(x, lineObjectRT.anchoredPosition.y);
            }).SetEase(Ease.OutSine).OnComplete(() =>
            {
                authorObject.SetActive(true);
                maskObject.SetActive(true);
                // 设置宽
                maskObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, canvasRect.rect.width / 2);
                authorObjectRT.anchoredPosition = new Vector2(-400, authorObjectRT.anchoredPosition.y);
                DOVirtual.Float(-400f, 400f, .5f, (float x) =>
                {
                    authorObjectRT.anchoredPosition = new Vector2(x, authorObjectRT.anchoredPosition.y);
                }).SetEase(Ease.OutSine).OnComplete(() =>
                {
                    maskObject.SetActive(false);
                    iconObject.SetActive(true);
                    iconObjectRT.anchoredPosition = new Vector2(-canvasRect.rect.width, iconObjectRT.anchoredPosition.y);
                    DOVirtual.Float(-canvasRect.rect.width, -400f, .5f, (float x) =>
                    {
                        iconObjectRT.anchoredPosition = new Vector2(x, iconObjectRT.anchoredPosition.y);
                    }).SetEase(Ease.OutSine).OnComplete(() =>
                    {
                        DOVirtual.DelayedCall(2f, () =>
                        {
                            if (GlobalVolumeObject.GetComponent<Volume>().profile.TryGet<Vignette>(out Vignette vignette))
                            {
                                DOVirtual.Float(0.3f, 0f, 1f, (float x) =>
                                {
                                    vignette.intensity.value = x;
                                }).SetEase(Ease.OutSine);
                            }
                            DOVirtual.Float(1f, 0f, 1f, (float x) =>
                            {
                                lineObjectCG.alpha = x;
                                authorObjectCG.alpha = x;
                                iconObjectCG.alpha = x;
                            }).SetEase(Ease.OutSine).OnComplete(() =>
                            {
                                lineObject.SetActive(false);
                                authorObject.SetActive(false);
                                iconObject.SetActive(false);
                                clickTipObjectCG.alpha = 0f;
                                clickTipObject.SetActive(true);
                                titleObject.SetActive(true);
                                DOVirtual.Float(100f, 200f, .7f, (float x) =>
                                {
                                    clickTipObjectRT.anchoredPosition = new Vector2(clickTipObjectRT.anchoredPosition.x, x);
                                    clickTipObjectCG.alpha = 1f - (200f - x) / 100f;
                                }).SetEase(Ease.OutSine);
                                System.Random rng = new System.Random();
                                GameObject graphGroup = new GameObject("GraphGroup", typeof(RectTransform));
                                RectTransform graphGroupRectTransform = graphGroup.GetComponent<RectTransform>();
                                graphGroupRectTransform.SetParent(canvas.transform, false);
                                for (int i = 0; i < 70; i++)
                                {
                                    GameObject obj = new GameObject($"Graph_{i}", typeof(RectTransform));
                                    RectTransform rt = obj.GetComponent<RectTransform>();
                                    rt.SetParent(graphGroupRectTransform);
                                    rt.localScale = new Vector3((float)rng.NextDouble() * 1.4f, (float)rng.NextDouble() * 1.3f, 0);
                                    rt.eulerAngles = new Vector3(0, 0, rng.Next(0, 360));
                                    rt.anchoredPosition = new Vector2(rng.Next((int)-canvasRect.rect.width / 2, (int)canvasRect.rect.width / 2),
                                        rng.Next((int)-canvasRect.rect.height / 2, (int)canvasRect.rect.height / 2));
                                    Image objImg = obj.AddComponent<Image>();
                                    objImg.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.2f, 0.4f, 0.8f, 1.0f);
                                    objImg.color = new Color(objImg.color.r, objImg.color.g, objImg.color.b, 0.4f);
                                    int val = rng.Next(1, 4); // 3 + 1
                                    if (val == 1)
                                    {
                                        objImg.sprite = triangle;
                                    }
                                    else if (val == 2)
                                    {
                                        objImg.sprite = rect;
                                    }
                                    else
                                    {
                                        objImg.sprite = circle;
                                    }
                                    canvasGroups.Add(obj.AddComponent<CanvasGroup>());
                                    Graph graph = new Graph(rt, 30f, rng);
                                    graphs.Add(graph);
                                    graphObjects.Add(obj);
                                }
                                DOVirtual.DelayedCall(0.3f, () =>
                                {
                                    state = 1;
                                }).SetEase(Ease.OutSine);
                                DOVirtual.Float(0f, 1f, .8f, (float x) =>
                                {
                                    foreach (var cg in canvasGroups)
                                    {
                                        cg.alpha = x;
                                    }
                                }).SetEase(Ease.OutSine);
                                DOVirtual.Float(0f, 1f, .8f, (float x) =>
                                {
                                    titleObjectCG.alpha = x;
                                }).SetEase(Ease.OutSine);
                            });
                        });
                    });
                });
            });
        });
    }

    void Update()
    {
        if (state == 1 || state == 2)
        {
            foreach (var graph in graphs)
            {
                graph.UpdatePosition(canvasRect.rect);
            }
        }
        if (state == 1 && (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
        {
            state = 2;
            const float speed = 1f;
            CanvasGroup titleObjectCG = titleObject.GetComponent<CanvasGroup>();
            DOVirtual.Float(1f, 0f, speed, (float x) =>
            {
                titleObjectCG.alpha = x;
            }).SetEase(Ease.OutSine).OnComplete(() => {
                //DOVirtual.Float(1000f, 100000f, speed, (float x) =>
                //{
                //    foreach (var graph in graphs)
                //    {
                //        graph.ApplyRippleForce(x);
                //    }
                //}).SetEase(Ease.OutSine).OnComplete(() =>
                //{
                titleObject.SetActive(false);
                foreach (var obj in graphObjects)
                {
                    Destroy(obj);
                }
                graphObjects.Clear();
                graphs.Clear();
                canvasGroups.Clear();
                clickTipObject.SetActive(false);
                signalCenter.Emit(SignalType.BootCompletion, gameObject);
                //signalCenter.Emit(SignalType.ChangeAngularBall, gameObject, AngularBallState.Active);
                state = 3;
                //});
            });
        }
        if (Keyboard.current.ctrlKey.IsPressed() && Keyboard.current.altKey.isPressed && Keyboard.current.enterKey.isPressed)
        {
            ToggleFullScreen();
        }
    }

    void ToggleFullScreen()
    {
#if UNITY_EDITOR
        var assembly = typeof(UnityEditor.EditorWindow).Assembly;
        var type = assembly.GetType("UnityEditor.GameView");
        var gameView = UnityEditor.EditorWindow.GetWindow(type);
        gameView.maximized = !gameView.maximized;
#else
        Screen.fullScreen = !Screen.fullScreen;
#endif
    }
}