using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : MonoBehaviour, ITutorial
{
    public static Tutorial Instance;

    [Header("【一】新手教程 BeginTutorial")]

    public GameObject TutorialCanvas;
    public GameObject Welcome;
    public GameObject Begin;
    public GameObject TutorialTitle;
    public GameObject Move1;
    public GameObject Move2;
    public GameObject Mouse1;
    public GameObject Mouse2;
    public GameObject FullScreen;
    public GameObject End;

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Tutorial: 重复实例！");
            Destroy(this);
            return;
        }
    }

    public void EnterTutorial(TutorialSet TutorialID)
    {
        //LightingManager.SetSkybox(LightingManager.Instance.dayAndNightSkyboxMaterial);
        if (TutorialID == TutorialSet.BeginTutorial)
        {
            //AudioManager.bgmSource.Stop();
            //AudioManager.bgmSource.clip = AudioManager.Instance.Relax;
            //AudioManager.bgmSource.Play();
            TutorialCanvas.SetActive(true);
            const float AnimationSpeed = 0.5f;
            List<GameObject> gameObjects = new() { Welcome, Begin, Move1, Move2, Mouse1, Mouse2, FullScreen, End };
            if (!TutorialTitle.TryGetComponent<CanvasGroup>(out var TutorialTitleGroup))
            {
                TutorialTitleGroup = TutorialTitle.AddComponent<CanvasGroup>();
            }
            TutorialTitleGroup.alpha = 0;
            TutorialTitle.SetActive(true);
            DOVirtual.Float(0, 1, AnimationSpeed, (float val) =>
            {
                TutorialTitleGroup.alpha = val;
            }).SetEase(Ease.OutQuad);
            int i = 0;
            foreach (GameObject obj in gameObjects)
            {
                if (!obj.TryGetComponent<CanvasGroup>(out var objGroup))
                {
                    objGroup = obj.AddComponent<CanvasGroup>();
                }
                objGroup.alpha = 0;
                DOVirtual.DelayedCall(AnimationSpeed * i * 8, () =>
                {
                    obj.SetActive(true);
                    DOVirtual.Float(0, 1, AnimationSpeed, (float val) =>
                    {
                        objGroup.alpha = val;
                    }).SetEase(Ease.OutQuad);
                    DOVirtual.DelayedCall(AnimationSpeed * 7, () =>
                    {
                        DOVirtual.Float(1, 0, AnimationSpeed, (float y) =>
                        {
                            objGroup.alpha = y;
                        }).SetEase(Ease.OutQuad);
                        DOVirtual.DelayedCall(AnimationSpeed, () =>
                        {
                            obj.SetActive(false);
                        });
                    });
                });
                i++;
            }
            DOVirtual.DelayedCall(AnimationSpeed * (i * 8 + 2), () =>
            {
                DOVirtual.Float(1, 0, AnimationSpeed, (float ay) =>
                {
                    TutorialTitleGroup.alpha = ay;
                }).SetEase(Ease.OutQuad);
                DOVirtual.DelayedCall(AnimationSpeed, () =>
                {
                    TutorialTitle.SetActive(false);
                    TutorialCanvas.SetActive(false);
                    //LightingManager.SetSkybox(LightingManager.Instance.defaultSkyboxMaterial);

                    //AudioManager.bgmSource.Stop();
                    //AudioManager.bgmSource.clip = AudioManager.Instance.wind;
                    //AudioManager.bgmSource.Play();

                    //AudioSource treeWindSource = AudioManager.Instance.PlayAudio(AudioManager.Instance.treeWind);
                    //treeWindSource.loop = true;
                    //AudioSource windSource = AudioManager.Instance.PlayAudio(AudioManager.Instance.wind);
                    //windSource.loop = true;
                });
            });
        }
    }

    //void Update()
    //{
        
    //}
}
