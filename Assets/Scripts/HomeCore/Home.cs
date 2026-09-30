using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Home : MonoBehaviour
{
    public GameObject Buttons;

    public GameObject RhythmGameButtonObject;
    public GameObject WorldButtonObject;
    public Button RhythmGameButton;
    public Button WorldButton;

    public GameObject AboutUsButton;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    private ISignalCenter signalCenter;

    private RectTransform buttonRectTransform;
    private CanvasGroup buttonCanvasGroup;

    void Awake()
    {
        RhythmGameButton = RhythmGameButtonObject.GetComponent<Button>();
        WorldButton = WorldButtonObject.GetComponent<Button>();
    }

    void Start()
    {
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        signalCenter.Subscribe(SignalType.BootCompletion, OnBootCompletion);
        signalCenter.Subscribe(SignalType.ExitHomepage, OnExitHomepage);
        buttonRectTransform = Buttons.GetComponent<RectTransform>();
        buttonCanvasGroup = Buttons.GetComponent<CanvasGroup>();
        if (buttonRectTransform == null)
        {
            Debug.LogError("RhythmGameButton does not have a RectTransform component.");
        }
        if (buttonCanvasGroup == null)
        {
            Debug.LogError("RhythmGameButton does not have a CanvasGroup component. Adding one.");
        }
        if (AboutUsButton == null)
        {
            Debug.LogError("AboutUsButton reference is not set in the Inspector.");
        }
    }

    void OnBootCompletion(GameObject sender, object data)
    {
        LightingManager.SetSkyboxCubemap(LightingManager.Instance.belfast_sunset_puresky_4k, 1f, 60f);
        if (!AudioManager.bgmSource.isPlaying)
        {
            AudioManager.bgmSource.Play();
        }
        Buttons.SetActive(true);
        AboutUsButton.SetActive(true);
        buttonCanvasGroup.alpha = 0f;
        RhythmGameButton.enabled = false;
        WorldButton.enabled = false;


        SetPositionOffScreenRight();
        Vector3 targetPosition = new(-100f, buttonRectTransform.anchoredPosition.y);
        buttonRectTransform.DOAnchorPosX(targetPosition.x, 0.8f)
            .SetEase(Ease.OutCubic).onComplete = () =>
            {
                RhythmGameButton.enabled = true;
                WorldButton.enabled = true;
            };
        Sequence alphaSequence = DOTween.Sequence();
        alphaSequence.Append(buttonCanvasGroup.DOFade(0.8f, 0.3f).SetEase(Ease.OutQuad))
                        .Append(buttonCanvasGroup.DOFade(1f, 0.5f).SetEase(Ease.InOutSine));
    }

    // 设置按钮到屏幕右侧以外
    private void SetPositionOffScreenRight()
    {
        Vector2 currentPos = buttonRectTransform.anchoredPosition;
        buttonRectTransform.anchoredPosition = new Vector2(2000, currentPos.y);
    }

    public void OnExitHomepage(GameObject sender = null, object data = null)
    {
        LightingManager.SetSkyboxCubemap(LightingManager.Instance.belfast_sunset_puresky_4k, 1f, 240f);
        RhythmGameButton.enabled = false;
        WorldButton.enabled = false;
        AboutUsButton.SetActive(false);
        Sequence combinedSequence = DOTween.Sequence();
        combinedSequence.Join(buttonCanvasGroup.DOFade(1.0f, 0.3f).SetEase(Ease.OutQuad))
                        //.Join(buttonCanvasGroup.DOFade(0.5f, 0.5f).SetEase(Ease.InOutSine))
                        .Join(buttonRectTransform.DOAnchorPosX(2000f, 0.8f).SetEase(Ease.OutCubic));

    }

    private void OnButtonClicked(SignalType sendSignal)
    {
        OnExitHomepage();
        Buttons.SetActive(false);
        //signalCenter.Emit(SignalType.ChangeAngularBall, gameObject, AngularBallState.Inactive);
        signalCenter.Emit(sendSignal, gameObject);
    }
    public void OnRhythmGameButtonClicked()
    {
        MapList.Instance.OpenCloseWall(() =>
        {
            OnButtonClicked(SignalType.EnterRhythmGame);
        });
    }

    public void OnWorldButtonClicked()
    {
        OnButtonClicked(SignalType.EnterWorld);
    }
}
